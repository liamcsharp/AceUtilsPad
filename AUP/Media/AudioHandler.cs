using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AUP;

public sealed class AudioHandler : MonoBehaviour
{
	private const byte VolumeDownKey = 174;
	private const byte VolumeUpKey = 175;
	private const byte NextTrackKey = 176;
	private const byte PreviousTrackKey = 177;
	private const byte PlayPauseKey = 179;
	private const uint KeyUpFlag = 2u;
	private const string NoAudioTitle = "Nothing playing";
	private const float RestartDelay = 8f;
	private const float StaleSeconds = 6f;
	private const int MaxRestarts = 6;

	private const string Script = @"
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false
$parentId = __PARENT__
$commandFile = '__COMMANDS__'
function Emit($value) { [Console]::Out.WriteLine(($value | ConvertTo-Json -Compress)); [Console]::Out.Flush() }
try {
	Add-Type -AssemblyName System.Runtime.WindowsRuntime
	$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
	function Await($operation, [Type]$type) {
		$task = $asTaskGeneric.MakeGenericMethod($type).Invoke($null, @($operation))
		if (-not $task.Wait(2500)) { return $null }
		return $task.Result
	}
	$null = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager,Windows.Media.Control,ContentType=WindowsRuntime]
	$managerType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]
	$propertiesType = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties]
	$manager = Await ($managerType::RequestAsync()) ($managerType)
	if ($manager -eq $null) { throw 'The media session manager did not answer.' }
} catch {
	Emit ([ordered]@{ ok = $false; error = [string]$_.Exception.Message })
	exit 2
}
$ErrorActionPreference = 'Continue'
while ($true) {
	if (-not (Get-Process -Id $parentId -ErrorAction SilentlyContinue)) { exit 0 }
	$session = $null
	try {
		foreach ($candidate in $manager.GetSessions()) {
			if ([string]$candidate.GetPlaybackInfo().PlaybackStatus -eq 'Playing') { $session = $candidate; break }
		}
		if ($session -eq $null) { $session = $manager.GetCurrentSession() }
	} catch { $session = $null }
	if (Test-Path -LiteralPath $commandFile) {
		$commands = @()
		try { $commands = @(Get-Content -LiteralPath $commandFile); Remove-Item -LiteralPath $commandFile -Force } catch { }
		foreach ($command in $commands) {
			if ($session -eq $null) { continue }
			try {
				if ($command -eq 'toggle') { $null = Await ($session.TryTogglePlayPauseAsync()) ([bool]) }
				elseif ($command -eq 'next') { $null = Await ($session.TrySkipNextAsync()) ([bool]) }
				elseif ($command -eq 'previous') { $null = Await ($session.TrySkipPreviousAsync()) ([bool]) }
				elseif ($command -match '^seek (\d+)$') { $null = Await ($session.TryChangePlaybackPositionAsync([long]$matches[1] * 10000000)) ([bool]) }
			} catch { }
		}
	}
	$out = $null
	if ($session -ne $null) {
		try {
			$props = Await ($session.TryGetMediaPropertiesAsync()) ($propertiesType)
			$timeline = $session.GetTimelineProperties()
			$playback = $session.GetPlaybackInfo()
			$status = [string]$playback.PlaybackStatus
			$position = $timeline.Position.TotalSeconds
			if ($status -eq 'Playing') {
				$age = ([DateTimeOffset]::Now - $timeline.LastUpdatedTime).TotalSeconds
				if ($age -gt 0 -and $age -lt 21600) { $position = $position + $age }
			}
			$out = [ordered]@{
				ok = $true
				app = [string]$session.SourceAppUserModelId
				title = [string]$props.Title
				artist = [string]$props.Artist
				album = [string]$props.AlbumTitle
				status = $status
				position = [double]$position
				start = [double]$timeline.StartTime.TotalSeconds
				end = [double]$timeline.EndTime.TotalSeconds
			}
		} catch { $out = $null }
	}
	if ($out -eq $null) { $out = [ordered]@{ ok = $true; idle = $true } }
	Emit $out
	Start-Sleep -Milliseconds 400
}
";

	private readonly object gate = new object();

	private Process process;
	private MediaSnapshot pending;
	private string commandFile;
	private float positionSeconds;
	private float positionStamp;
	private float startSeconds;
	private float endSeconds;
	private float lastHeard;
	private float nextStartTime;
	private int restarts;
	private bool unsupported;

	public string CurrentTitle { get; private set; } = NoAudioTitle;
	public string CurrentArtist { get; private set; } = string.Empty;
	public string SourceName { get; private set; } = string.Empty;
	public string Status { get; private set; } = "Looking for music on this PC...";
	public bool IsPlaying { get; private set; }
	public bool HasMedia { get; private set; }
	public bool Connecting { get; private set; }

	public bool HasTimeline => HasMedia && endSeconds - startSeconds > 1f;

	public float Duration => Mathf.Max(0f, endSeconds - startSeconds);

	public float Position
	{
		get
		{
			if (!HasMedia)
			{
				return 0f;
			}

			float position = positionSeconds;
			if (IsPlaying)
			{
				position += Time.unscaledTime - positionStamp;
			}

			position -= startSeconds;
			return HasTimeline ? Mathf.Clamp(position, 0f, Duration) : Mathf.Max(0f, position);
		}
	}

	public float Progress => HasTimeline ? Mathf.Clamp01(Position / Duration) : 0f;

	public string PositionText => HasMedia ? FormatTime(Position) : "--:--";

	public string DurationText => HasTimeline ? FormatTime(Duration) : HasMedia ? "LIVE" : "--:--";

	public string TimelineText => PositionText + " / " + DurationText;

	private void Start()
	{
		nextStartTime = Time.unscaledTime + 1f;
	}

	private void Update()
	{
		ApplyPending();
		if (unsupported)
		{
			return;
		}

		bool running = process != null && !SafeHasExited(process);
		if (!running && Time.unscaledTime >= nextStartTime)
		{
			StartReader();
		}

		if (running && Time.unscaledTime - lastHeard > (Connecting ? StaleSeconds * 3f : StaleSeconds))
		{
			Status = "Media reader stopped answering. Restarting...";
			StopReader();
			nextStartTime = Time.unscaledTime + 1f;
		}
	}

	private void OnDestroy()
	{
		StopReader();
	}

	private void OnApplicationQuit()
	{
		StopReader();
	}

	public void RefreshNowPlaying()
	{
		if (!unsupported && (process == null || SafeHasExited(process)) && Time.unscaledTime >= nextStartTime)
		{
			StartReader();
		}
	}

	public void PreviousTrack()
	{
		if (!SendCommand("previous"))
		{
			SendMediaKey(PreviousTrackKey, "previous track");
		}
	}

	public void TogglePlayPause()
	{
		if (!SendCommand("toggle"))
		{
			SendMediaKey(PlayPauseKey, "play/pause");
		}
		else
		{
			IsPlaying = !IsPlaying;
			positionSeconds += IsPlaying ? 0f : Time.unscaledTime - positionStamp;
			positionStamp = Time.unscaledTime;
		}
	}

	public void NextTrack()
	{
		if (!SendCommand("next"))
		{
			SendMediaKey(NextTrackKey, "next track");
		}
	}

	public void VolumeUp()
	{
		SendMediaKey(VolumeUpKey, "volume up");
	}

	public void VolumeDown()
	{
		SendMediaKey(VolumeDownKey, "volume down");
	}

	public void SeekBy(float seconds)
	{
		if (!HasTimeline)
		{
			Status = "This player does not share a timeline to seek in.";
			return;
		}

		float target = Mathf.Clamp(Position + seconds, 0f, Mathf.Max(0f, Duration - 1f));
		if (SendCommand("seek " + Mathf.RoundToInt(target + startSeconds)))
		{
			positionSeconds = target + startSeconds;
			positionStamp = Time.unscaledTime;
			Status = "Jumped to " + FormatTime(target) + ".";
		}
	}

	public static string FormatTime(float seconds)
	{
		int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
		int hours = total / 3600;
		int minutes = total / 60 % 60;
		int rest = total % 60;
		return hours > 0 ? hours + ":" + minutes.ToString("00") + ":" + rest.ToString("00") : minutes + ":" + rest.ToString("00");
	}

	private void StartReader()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			unsupported = true;
			Status = "Now playing needs Windows. Media keys still work.";
			return;
		}

		if (restarts >= MaxRestarts)
		{
			unsupported = true;
			Status = "Could not read Windows media info. Media keys still work.";
			return;
		}

		restarts++;
		nextStartTime = Time.unscaledTime + RestartDelay;
		StopReader();
		try
		{
			string folder = Path.Combine(Path.GetTempPath(), "AceUtilsPad");
			Directory.CreateDirectory(folder);
			int parentId = Process.GetCurrentProcess().Id;
			commandFile = Path.Combine(folder, "media-" + parentId + ".cmd");
			if (File.Exists(commandFile))
			{
				File.Delete(commandFile);
			}

			string script = Script.Replace("__PARENT__", parentId.ToString()).Replace("__COMMANDS__", commandFile.Replace("'", "''"));
			string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = PowerShellPath(),
				Arguments = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " + encoded,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = new UTF8Encoding(false),
				WindowStyle = ProcessWindowStyle.Hidden
			};

			Process started = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
			started.OutputDataReceived += OnOutput;
			started.ErrorDataReceived += OnError;
			if (!started.Start())
			{
				Status = "Could not start the media reader.";
				return;
			}

			started.BeginOutputReadLine();
			started.BeginErrorReadLine();
			process = started;
			Connecting = true;
			lastHeard = Time.unscaledTime;
			Status = "Looking for music on this PC...";
		}
		catch (Exception exception)
		{
			Connecting = false;
			Status = "Media reader failed to start: " + exception.Message;
			Debug.LogWarning("[AceUtilsPad] Media reader failed to start: " + exception);
		}
	}

	private static string PowerShellPath()
	{
		string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
		string path = Path.Combine(system, "WindowsPowerShell", "v1.0", "powershell.exe");
		return File.Exists(path) ? path : "powershell.exe";
	}

	private void StopReader()
	{
		Process running = process;
		process = null;
		Connecting = false;
		if (running == null)
		{
			return;
		}

		try
		{
			running.OutputDataReceived -= OnOutput;
			running.ErrorDataReceived -= OnError;
			if (!running.HasExited)
			{
				running.Kill();
			}
		}
		catch (Exception)
		{
		}

		try
		{
			running.Dispose();
		}
		catch (Exception)
		{
		}
	}

	private static bool SafeHasExited(Process target)
	{
		try
		{
			return target.HasExited;
		}
		catch (Exception)
		{
			return true;
		}
	}

	private bool SendCommand(string command)
	{
		if (process == null || SafeHasExited(process) || string.IsNullOrEmpty(commandFile) || !HasMedia)
		{
			return false;
		}

		try
		{
			File.AppendAllText(commandFile, command + Environment.NewLine);
			Status = "Sent " + command + " to " + (string.IsNullOrEmpty(SourceName) ? "the player" : SourceName) + ".";
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private void OnError(object sender, DataReceivedEventArgs args)
	{
		if (!string.IsNullOrWhiteSpace(args.Data))
		{
			Debug.LogWarning("[AceUtilsPad] Media reader: " + args.Data);
		}
	}

	private void OnOutput(object sender, DataReceivedEventArgs args)
	{
		string line = args.Data;
		if (string.IsNullOrWhiteSpace(line))
		{
			return;
		}

		try
		{
			JObject json = JObject.Parse(line.Trim().TrimStart('﻿'));
			MediaSnapshot snapshot = new MediaSnapshot
			{
				Ok = json.Value<bool?>("ok") ?? false,
				Idle = json.Value<bool?>("idle") ?? false,
				Error = json.Value<string>("error"),
				App = json.Value<string>("app") ?? string.Empty,
				Title = (json.Value<string>("title") ?? string.Empty).Trim(),
				Artist = (json.Value<string>("artist") ?? string.Empty).Trim(),
				Album = (json.Value<string>("album") ?? string.Empty).Trim(),
				Playback = json.Value<string>("status") ?? string.Empty,
				Position = json.Value<double?>("position") ?? 0.0,
				Start = json.Value<double?>("start") ?? 0.0,
				End = json.Value<double?>("end") ?? 0.0
			};

			lock (gate)
			{
				pending = snapshot;
			}
		}
		catch (Exception)
		{
		}
	}

	private void ApplyPending()
	{
		MediaSnapshot snapshot;
		lock (gate)
		{
			snapshot = pending;
			pending = null;
		}

		if (snapshot == null)
		{
			return;
		}

		lastHeard = Time.unscaledTime;
		Connecting = false;
		if (!snapshot.Ok)
		{
			unsupported = true;
			HasMedia = false;
			IsPlaying = false;
			CurrentTitle = NoAudioTitle;
			CurrentArtist = string.Empty;
			Status = "Windows media info is not available: " + (snapshot.Error ?? "unknown error") + ". Media keys still work.";
			StopReader();
			return;
		}

		restarts = 0;
		if (snapshot.Idle || snapshot.Title.Length == 0)
		{
			HasMedia = false;
			IsPlaying = false;
			CurrentTitle = NoAudioTitle;
			CurrentArtist = string.Empty;
			SourceName = string.Empty;
			Status = "Play something in Spotify, a browser or any media app.";
			return;
		}

		HasMedia = true;
		CurrentTitle = snapshot.Title;
		CurrentArtist = snapshot.Artist.Length > 0 ? snapshot.Artist : snapshot.Album;
		SourceName = FriendlyApp(snapshot.App);
		IsPlaying = string.Equals(snapshot.Playback, "Playing", StringComparison.OrdinalIgnoreCase);
		positionSeconds = (float)snapshot.Position;
		positionStamp = Time.unscaledTime;
		startSeconds = (float)snapshot.Start;
		endSeconds = (float)snapshot.End;
		Status = (IsPlaying ? "Playing" : FriendlyState(snapshot.Playback)) + (SourceName.Length > 0 ? " in " + SourceName : string.Empty) + ".";
	}

	private static string FriendlyState(string playback)
	{
		switch ((playback ?? string.Empty).ToLowerInvariant())
		{
			case "paused":
				return "Paused";
			case "stopped":
				return "Stopped";
			case "changing":
				return "Loading";
			default:
				return "Ready";
		}
	}

	private static string FriendlyApp(string app)
	{
		if (string.IsNullOrEmpty(app))
		{
			return string.Empty;
		}

		string lower = app.ToLowerInvariant();
		if (lower.Contains("spotify"))
		{
			return "Spotify";
		}

		if (lower.Contains("msedge"))
		{
			return "Edge";
		}

		if (lower.Contains("chrome"))
		{
			return "Chrome";
		}

		if (lower.Contains("firefox"))
		{
			return "Firefox";
		}

		if (lower.Contains("opera"))
		{
			return "Opera";
		}

		if (lower.Contains("brave"))
		{
			return "Brave";
		}

		if (lower.Contains("applemusic") || lower.Contains("itunes"))
		{
			return "Apple Music";
		}

		if (lower.Contains("zunemusic") || lower.Contains("media.player"))
		{
			return "Media Player";
		}

		if (lower.Contains("soundcloud"))
		{
			return "SoundCloud";
		}

		if (lower.Contains("vlc"))
		{
			return "VLC";
		}

		if (lower.Contains("discord"))
		{
			return "Discord";
		}

		string name = app;
		int bang = name.IndexOf('!');
		if (bang > 0)
		{
			name = name.Substring(0, bang);
		}

		int underscore = name.IndexOf('_');
		if (underscore > 0)
		{
			name = name.Substring(0, underscore);
		}

		if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			name = name.Substring(0, name.Length - 4);
		}

		int dot = name.LastIndexOf('.');
		if (dot >= 0 && dot < name.Length - 1)
		{
			name = name.Substring(dot + 1);
		}

		return name.Length > 18 ? name.Substring(0, 18) : name;
	}

	private void SendMediaKey(byte virtualKey, string commandName)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Status = "Media controls are supported on Windows only.";
			return;
		}

		try
		{
			keybd_event(virtualKey, 0, 0u, UIntPtr.Zero);
			keybd_event(virtualKey, 0, KeyUpFlag, UIntPtr.Zero);
			Status = "Sent " + commandName + ".";
		}
		catch (Exception exception)
		{
			Status = "Could not send " + commandName + ".";
			Debug.LogWarning("[AceUtilsPad] Failed to send media key: " + exception.Message);
		}
	}

	[DllImport("user32.dll")]
	private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);

	private sealed class MediaSnapshot
	{
		public bool Ok;
		public bool Idle;
		public string Error;
		public string App;
		public string Title;
		public string Artist;
		public string Album;
		public string Playback;
		public double Position;
		public double Start;
		public double End;
	}
}

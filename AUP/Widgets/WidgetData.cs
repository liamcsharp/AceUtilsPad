using System;
using System.Collections.Generic;
using AUP.UI;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

namespace AUP.Widgets;

internal enum RowType
{
	Text,
	Bar,
	Buttons,
	Slider,
	Separator,
	Panel,
	Icon
}

internal sealed class WidgetRow
{
	public RowType Type;
	public Func<string> Text;
	public float Size = 10f;
	public bool Bold;
	public bool Muted;
	public int Lines = 1;
	public Func<float> Value;
	public Action<float> Set;
	public ButtonSpec[] Buttons;
	public string Icon;
}

internal sealed class WidgetData
{
	public static readonly string[] InfoNames =
	{
		"FPS", "Ping", "Time", "Date", "Room", "Players", "Song", "Artist", "Session", "Battery", "Heading", "Camera"
	};

	public static readonly string[] ActionNames =
	{
		"Open menu", "Play / pause", "Next song", "Previous song", "Take photo", "Toggle AceCam", "Toggle name tags", "Call the orb", "Record video"
	};

	public static readonly string[] ToggleNames = { "Name tags", "AceCam", "Particles", "Cinematic camera", "Hide widgets with menu" };
	public static readonly string[] SliderNames = { "Menu opacity", "Camera FOV", "Alert volume", "Widget transparency" };
	public static readonly string[] ProgressNames = { "Song progress", "FPS", "Battery", "Lobby fill" };
	public static readonly string[] IconNames = { "star", "heart", "bolt", "bell", "play", "camera", "crown", "group", "sun", "tag" };

	private static readonly string[] Cardinals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

	private readonly MenuContext context;
	private readonly float startTime;
	private float roomStart;
	private string lastRoom;
	private float batteryStamp = -10f;
	private float batteryLeft = -1f;
	private float batteryRight = -1f;
	private float batteryHead = -1f;

	public WidgetData(MenuContext context)
	{
		this.context = context;
		startTime = Time.unscaledTime;
	}

	public Func<WidgetProfile> Profile;
	public Func<List<WidgetConfig>> Favorites;
	public Action<WidgetConfig> ToggleWidget;
	public Action Changed;

	public void Tick()
	{
		string room = context.Lobby.RoomName;
		if (room != lastRoom)
		{
			lastRoom = room;
			roomStart = Time.unscaledTime;
		}
	}

	public int Ping => PhotonNetwork.IsConnected ? PhotonNetwork.GetPing() : -1;

	public int PlayerCount => context.Lobby.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0;

	public string Room => context.Lobby.RoomName;

	public string Song => context.Audio.HasMedia ? context.Audio.CurrentTitle : null;

	public float BatteryLowest
	{
		get
		{
			ReadBattery();
			float lowest = 2f;
			lowest = batteryLeft >= 0f ? Mathf.Min(lowest, batteryLeft) : lowest;
			lowest = batteryRight >= 0f ? Mathf.Min(lowest, batteryRight) : lowest;
			lowest = batteryHead >= 0f ? Mathf.Min(lowest, batteryHead) : lowest;
			return lowest > 1.5f ? -1f : lowest;
		}
	}

	public string Info(int index)
	{
		switch (WidgetOptions.Wrap(index, InfoNames.Length))
		{
			case 0:
				return Mathf.RoundToInt(context.Fps) + " fps";
			case 1:
				return Ping >= 0 ? Ping + " ms" : "offline";
			case 2:
				return DateTime.Now.ToString("h:mm tt");
			case 3:
				return DateTime.Now.ToString("ddd, MMM d");
			case 4:
				return context.Lobby.InRoom ? Rich.Escape(context.Lobby.RoomName) : "No room";
			case 5:
				return context.Lobby.InRoom ? context.Lobby.PlayerCountText : "0";
			case 6:
				return Rich.Escape(context.Audio.CurrentTitle);
			case 7:
				return context.Audio.CurrentArtist.Length > 0 ? Rich.Escape(context.Audio.CurrentArtist) : "-";
			case 8:
				return AudioHandler.FormatTime(Time.unscaledTime - startTime);
			case 9:
				return BatteryText();
			case 10:
				return Heading();
			default:
				return context.Cam.StatusLine;
		}
	}

	public void Run(int index)
	{
		switch (WidgetOptions.Wrap(index, ActionNames.Length))
		{
			case 0:
				context.ToggleMenu?.Invoke();
				break;
			case 1:
				context.Audio.TogglePlayPause();
				break;
			case 2:
				context.Audio.NextTrack();
				break;
			case 3:
				context.Audio.PreviousTrack();
				break;
			case 4:
				context.Cam.TakePhoto();
				break;
			case 5:
				context.Cam.Toggle();
				break;
			case 6:
				context.Settings.Tags.Enabled.Value = !context.Settings.Tags.Enabled.Value;
				break;
			case 7:
				context.SummonOrb?.Invoke();
				break;
			default:
				context.Cam.ToggleRecording();
				break;
		}
	}

	public bool GetToggle(int index)
	{
		switch (WidgetOptions.Wrap(index, ToggleNames.Length))
		{
			case 0:
				return context.Settings.Tags.Enabled.Value;
			case 1:
				return context.Cam.Settings.Enabled.Value;
			case 2:
				return context.Settings.ThemeParticles.Value;
			case 3:
				return context.Cam.Settings.Cinematic.Value;
			default:
				return Profile().HideWithMenu;
		}
	}

	public void FlipToggle(int index)
	{
		switch (WidgetOptions.Wrap(index, ToggleNames.Length))
		{
			case 0:
				context.Settings.Tags.Enabled.Value = !context.Settings.Tags.Enabled.Value;
				break;
			case 1:
				context.Cam.Toggle();
				break;
			case 2:
				context.Settings.ThemeParticles.Value = !context.Settings.ThemeParticles.Value;
				break;
			case 3:
				context.Cam.Settings.Cinematic.Value = !context.Cam.Settings.Cinematic.Value;
				break;
			default:
				Profile().HideWithMenu = !Profile().HideWithMenu;
				Changed?.Invoke();
				break;
		}
	}

	public float GetSlider(int index)
	{
		switch (WidgetOptions.Wrap(index, SliderNames.Length))
		{
			case 0:
				return Mathf.InverseLerp(MenuSettings.MinOpacity, MenuSettings.MaxOpacity, context.Settings.Opacity.Value);
			case 1:
				return Mathf.InverseLerp(CamSettings.MinFov, CamSettings.MaxFov, context.Cam.Settings.Fov.Value);
			case 2:
				return context.Settings.SoundVolume.Value;
			default:
				return Mathf.InverseLerp(0.2f, 1f, Profile().GlobalAlpha);
		}
	}

	public void SetSlider(int index, float value)
	{
		value = Mathf.Clamp01(value);
		switch (WidgetOptions.Wrap(index, SliderNames.Length))
		{
			case 0:
				context.Settings.Opacity.Value = Mathf.Round(Mathf.Lerp(MenuSettings.MinOpacity, MenuSettings.MaxOpacity, value) * 20f) / 20f;
				break;
			case 1:
				context.Cam.Settings.Fov.Value = Mathf.Round(Mathf.Lerp(CamSettings.MinFov, CamSettings.MaxFov, value));
				break;
			case 2:
				context.Settings.SoundVolume.Value = Mathf.Round(value * 10f) / 10f;
				break;
			default:
				Profile().GlobalAlpha = Mathf.Round(Mathf.Lerp(0.2f, 1f, value) * 20f) / 20f;
				Changed?.Invoke();
				break;
		}
	}

	public float Progress(int index)
	{
		switch (WidgetOptions.Wrap(index, ProgressNames.Length))
		{
			case 0:
				return context.Audio.Progress;
			case 1:
				return Mathf.Clamp01(context.Fps / 120f);
			case 2:
				return Mathf.Max(0f, BatteryLowest);
			default:
				return context.Lobby.InRoom && PhotonNetwork.CurrentRoom.MaxPlayers > 0
					? Mathf.Clamp01(PhotonNetwork.CurrentRoom.PlayerCount / (float)PhotonNetwork.CurrentRoom.MaxPlayers)
					: 0f;
		}
	}

	public List<WidgetRow> Rows(WidgetConfig config)
	{
		List<WidgetRow> rows = new List<WidgetRow>();
		switch (config.KindValue)
		{
			case WidgetKind.Fps:
				rows.Add(Big(() => Mathf.RoundToInt(context.Fps).ToString()));
				rows.Add(Small(() => "fps   " + (context.Fps > 1f ? (1000f / context.Fps).ToString("0.0") : "-") + " ms"));
				rows.Add(Bar(() => Mathf.Clamp01(context.Fps / 120f)));
				break;
			case WidgetKind.Ping:
				rows.Add(Big(() => Ping >= 0 ? Ping.ToString() : "--"));
				rows.Add(Small(() => Ping < 0 ? "not connected" : Ping < 80 ? "ms   great" : Ping < 150 ? "ms   okay" : "ms   laggy"));
				break;
			case WidgetKind.Clock:
				rows.Add(Big(() => DateTime.Now.ToString("h:mm tt")));
				rows.Add(Small(() => DateTime.Now.ToString("dddd, MMM d")));
				break;
			case WidgetKind.Session:
				rows.Add(Big(() => AudioHandler.FormatTime(Time.unscaledTime - startTime)));
				rows.Add(Small(() => context.Lobby.InRoom ? "this lobby " + AudioHandler.FormatTime(Time.unscaledTime - roomStart) : "not in a lobby"));
				break;
			case WidgetKind.Players:
				rows.Add(Big(() => context.Lobby.InRoom ? context.Lobby.PlayerCountText : "0"));
				rows.Add(Small(() => context.Lobby.InRoom ? "players here" : "not in a lobby"));
				rows.Add(Bar(() => Progress(3)));
				break;
			case WidgetKind.Room:
				rows.Add(Big(() => context.Lobby.InRoom ? Rich.Escape(context.Lobby.RoomName) : "No room"));
				rows.Add(Small(() => context.Lobby.InRoom ? Rich.Escape(context.Lobby.GameMode) + "   " + context.Lobby.PlayerCountText : "join a lobby to see it here"));
				break;
			case WidgetKind.Battery:
				rows.Add(Big(() => BatteryLowest >= 0f ? Mathf.RoundToInt(BatteryLowest * 100f) + "%" : "--"));
				rows.Add(Small(BatteryText));
				rows.Add(Bar(() => Mathf.Max(0f, BatteryLowest)));
				break;
			case WidgetKind.Feed:
				for (int i = 0; i < 4; i++)
				{
					int line = i;
					rows.Add(new WidgetRow
					{
						Type = RowType.Text,
						Size = 8f,
						Muted = line > 0,
						Text = () => context.Alerts != null && line < context.Alerts.History.Count ? context.Alerts.History[line] : line == 0 ? "No alerts yet" : string.Empty
					});
				}

				break;
			case WidgetKind.Camera:
				rows.Add(Big(() => context.Cam.StatusLine, 11f));
				rows.Add(Buttons(
					new ButtonSpec("Photo", context.Cam.TakePhoto),
					new ButtonSpec(() => context.Cam.Recording ? "Stop" : "Rec", context.Cam.ToggleRecording, () => context.Cam.Recording)));
				break;
			case WidgetKind.Performance:
				rows.Add(Big(() => (context.Fps > 1f ? (1000f / context.Fps).ToString("0.0") : "-") + " ms", 13f));
				rows.Add(Small(() => "memory " + (GC.GetTotalMemory(false) / 1048576L) + " MB"));
				rows.Add(Small(() => PerformanceGuard.Level == 0 ? "full quality" : "anti-lag level " + PerformanceGuard.Level));
				break;
			case WidgetKind.Launcher:
				rows.Add(Buttons(new ButtonSpec("Menu", () => Run(0)), new ButtonSpec("Photo", () => Run(4))));
				rows.Add(Buttons(new ButtonSpec("Orb", () => Run(7)), new ButtonSpec("Tags", () => Run(6), () => GetToggle(0))));
				break;
			case WidgetKind.Music:
				rows.Add(new WidgetRow { Type = RowType.Text, Size = 9.5f, Bold = true, Text = () => Rich.Escape(context.Audio.CurrentTitle) });
				rows.Add(Small(() => context.Audio.CurrentArtist.Length > 0 ? Rich.Escape(context.Audio.CurrentArtist) : context.Audio.TimelineText));
				rows.Add(Bar(() => context.Audio.Progress));
				rows.Add(Buttons(
					new ButtonSpec("<<", context.Audio.PreviousTrack),
					new ButtonSpec(() => context.Audio.IsPlaying ? "Pause" : "Play", context.Audio.TogglePlayPause, () => context.Audio.IsPlaying),
					new ButtonSpec(">>", context.Audio.NextTrack)));
				break;
			case WidgetKind.Compass:
				rows.Add(Big(Heading));
				rows.Add(Small(() => "facing"));
				break;
			case WidgetKind.Text:
				rows.Add(Big(() => Rich.Escape(config.Text), 14f));
				break;
			case WidgetKind.Dock:
				AddDock(rows);
				break;
			default:
				AddCustom(rows, config);
				break;
		}

		return rows;
	}

	private void AddDock(List<WidgetRow> rows)
	{
		List<WidgetConfig> favorites = Favorites != null ? Favorites() : new List<WidgetConfig>();
		if (favorites.Count == 0)
		{
			rows.Add(Small(() => "Mark widgets as favorites to dock them here"));
			return;
		}

		for (int start = 0; start < favorites.Count && start < 6; start += 2)
		{
			List<ButtonSpec> specs = new List<ButtonSpec>();
			for (int i = start; i < start + 2 && i < favorites.Count; i++)
			{
				WidgetConfig target = favorites[i];
				specs.Add(new ButtonSpec(ShortName(target), () => ToggleWidget?.Invoke(target), () => target.Enabled));
			}

			rows.Add(Buttons(specs.ToArray()));
		}
	}

	private static string ShortName(WidgetConfig config)
	{
		string name = string.IsNullOrEmpty(config.Title) ? WidgetOptions.KindNames[config.Kind] : config.Title;
		int space = name.IndexOf(' ');
		string first = space > 0 ? name.Substring(0, space) : name;
		return first.Length > 9 ? first.Substring(0, 9) : first;
	}

	private void AddCustom(List<WidgetRow> rows, WidgetConfig config)
	{
		if (config.Elements.Count == 0)
		{
			rows.Add(Small(() => "Empty widget. Add elements in Widget Hub."));
			return;
		}

		foreach (WidgetElement source in config.Elements)
		{
			WidgetElement element = source;
			switch (element.Type)
			{
				case 0:
					rows.Add(new WidgetRow { Type = RowType.Text, Size = 10f, Bold = true, Text = () => Rich.Escape(element.Text) });
					break;
				case 1:
					rows.Add(new WidgetRow
					{
						Type = RowType.Text,
						Size = 10f,
						Text = () => "<b>" + InfoNames[WidgetOptions.Wrap(element.Bind, InfoNames.Length)] + "</b>  " + Info(element.Bind)
					});
					break;
				case 2:
					rows.Add(new WidgetRow { Type = RowType.Icon, Icon = IconNames[WidgetOptions.Wrap(element.Bind, IconNames.Length)] });
					break;
				case 3:
					rows.Add(Buttons(new ButtonSpec(
						() => element.Text.Length > 0 ? Rich.Escape(element.Text) : ActionNames[WidgetOptions.Wrap(element.Bind, ActionNames.Length)],
						() => Run(element.Bind))));
					break;
				case 4:
					rows.Add(Small(() => SliderNames[WidgetOptions.Wrap(element.Bind, SliderNames.Length)] + "  " + Mathf.RoundToInt(GetSlider(element.Bind) * 100f) + "%"));
					rows.Add(new WidgetRow { Type = RowType.Slider, Value = () => GetSlider(element.Bind), Set = value => SetSlider(element.Bind, value) });
					break;
				case 5:
					rows.Add(Buttons(new ButtonSpec(
						() => ToggleNames[WidgetOptions.Wrap(element.Bind, ToggleNames.Length)] + "  " + (GetToggle(element.Bind) ? "ON" : "OFF"),
						() => FlipToggle(element.Bind), () => GetToggle(element.Bind))));
					break;
				case 6:
					rows.Add(Small(() => ProgressNames[WidgetOptions.Wrap(element.Bind, ProgressNames.Length)]));
					rows.Add(Bar(() => Progress(element.Bind)));
					break;
				case 7:
					rows.Add(new WidgetRow { Type = RowType.Panel });
					break;
				default:
					rows.Add(new WidgetRow { Type = RowType.Separator });
					break;
			}
		}
	}

	public static string BindName(WidgetElement element)
	{
		switch (element.Type)
		{
			case 1:
				return InfoNames[WidgetOptions.Wrap(element.Bind, InfoNames.Length)];
			case 2:
				return IconNames[WidgetOptions.Wrap(element.Bind, IconNames.Length)];
			case 3:
				return ActionNames[WidgetOptions.Wrap(element.Bind, ActionNames.Length)];
			case 4:
				return SliderNames[WidgetOptions.Wrap(element.Bind, SliderNames.Length)];
			case 5:
				return ToggleNames[WidgetOptions.Wrap(element.Bind, ToggleNames.Length)];
			case 6:
				return ProgressNames[WidgetOptions.Wrap(element.Bind, ProgressNames.Length)];
			default:
				return "Nothing to pick";
		}
	}

	public static int BindCount(WidgetElement element)
	{
		switch (element.Type)
		{
			case 1:
				return InfoNames.Length;
			case 2:
				return IconNames.Length;
			case 3:
				return ActionNames.Length;
			case 4:
				return SliderNames.Length;
			case 5:
				return ToggleNames.Length;
			case 6:
				return ProgressNames.Length;
			default:
				return 1;
		}
	}

	private string Heading()
	{
		Transform head = PhysicsLayers.Head();
		if (head == null)
		{
			return "--";
		}

		Vector3 forward = head.forward;
		float degrees = Mathf.Repeat(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 360f);
		return Cardinals[Mathf.RoundToInt(degrees / 45f) % 8] + " " + Mathf.RoundToInt(degrees);
	}

	private string BatteryText()
	{
		ReadBattery();
		if (batteryLeft < 0f && batteryRight < 0f && batteryHead < 0f)
		{
			return "no battery info";
		}

		return "L " + Percent(batteryLeft) + "  R " + Percent(batteryRight) + "  H " + Percent(batteryHead);
	}

	private static string Percent(float level)
	{
		return level < 0f ? "--" : Mathf.RoundToInt(level * 100f) + "%";
	}

	private void ReadBattery()
	{
		if (Time.unscaledTime - batteryStamp < 5f)
		{
			return;
		}

		batteryStamp = Time.unscaledTime;
		batteryLeft = Level(XRNode.LeftHand);
		batteryRight = Level(XRNode.RightHand);
		batteryHead = Level(XRNode.Head);
	}

	private static float Level(XRNode node)
	{
		try
		{
			InputDevice device = InputDevices.GetDeviceAtXRNode(node);
			if (device.isValid && device.TryGetFeatureValue(CommonUsages.batteryLevel, out float level))
			{
				return Mathf.Clamp01(level);
			}
		}
		catch (Exception)
		{
		}

		return -1f;
	}

	private static WidgetRow Big(Func<string> text, float size = 17f)
	{
		return new WidgetRow { Type = RowType.Text, Size = size, Bold = true, Text = text };
	}

	private static WidgetRow Small(Func<string> text)
	{
		return new WidgetRow { Type = RowType.Text, Size = 8f, Muted = true, Text = text };
	}

	private static WidgetRow Bar(Func<float> value)
	{
		return new WidgetRow { Type = RowType.Bar, Value = value };
	}

	private static WidgetRow Buttons(params ButtonSpec[] specs)
	{
		return new WidgetRow { Type = RowType.Buttons, Buttons = specs };
	}
}

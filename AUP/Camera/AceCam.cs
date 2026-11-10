using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AUP.UI;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace AUP;

public enum CamMode
{
	FirstPerson,
	ThirdPerson,
	Follow,
	Tablet,
	Selfie,
	Orbit,
	Free
}

public sealed class CamSettings
{
	public const float MinFov = 30f;
	public const float MaxFov = 120f;
	public const float MinNear = 0.01f;
	public const float MaxNear = 1f;
	public const float MinDistance = 0.8f;
	public const float MaxDistance = 8f;
	public const float MinSpeed = 0.2f;
	public const float MaxSpeed = 4f;
	public const float MinSize = 0.5f;
	public const float MaxSize = 1.6f;

	public static readonly string[] ModeNames = { "First Person", "Third Person", "Follow", "Tablet", "Selfie", "Orbit", "Free" };

	public readonly ConfigEntry<bool> Enabled;
	public readonly ConfigEntry<int> Mode;
	public readonly ConfigEntry<float> Fov;
	public readonly ConfigEntry<float> Smooth;
	public readonly ConfigEntry<float> Near;
	public readonly ConfigEntry<float> Distance;
	public readonly ConfigEntry<float> Speed;
	public readonly ConfigEntry<float> Size;
	public readonly ConfigEntry<bool> Cinematic;

	public CamSettings(ConfigFile file)
	{
		Enabled = file.Bind("AceCam", "Enabled", false, "Turn the AceCam camera and tablet on.");
		Mode = file.Bind("AceCam", "Mode", (int)CamMode.ThirdPerson, "Camera mode index.");
		Fov = file.Bind("AceCam", "Fov", 80f, "Field of view in degrees.");
		Smooth = file.Bind("AceCam", "Smoothness", 0.5f, "Camera smoothing from 0 to 1.");
		Near = file.Bind("AceCam", "Clipping", 0.1f, "Near clipping distance in meters.");
		Distance = file.Bind("AceCam", "Distance", 2.5f, "Camera distance for third person, follow and orbit.");
		Speed = file.Bind("AceCam", "Speed", 1f, "Camera movement speed.");
		Size = file.Bind("AceCam", "TabletSize", 1f, "Size of the camera tablet.");
		Cinematic = file.Bind("AceCam", "Cinematic", false, "Cinematic look with letterbox bars and extra smoothing.");
	}

	public CamMode CurrentMode => (CamMode)Mathf.Clamp(Mode.Value, 0, ModeNames.Length - 1);

	public void Reset()
	{
		Mode.Value = (int)CamMode.ThirdPerson;
		Fov.Value = 80f;
		Smooth.Value = 0.5f;
		Near.Value = 0.1f;
		Distance.Value = 2.5f;
		Speed.Value = 1f;
		Size.Value = 1f;
		Cinematic.Value = false;
	}
}

public sealed class AceCam : MonoBehaviour
{
	public static readonly string[] PresetNames = { "Default", "Wide", "Close-up", "Action", "Cinematic", "Selfie", "Security" };

	private const int PreviewWidth = 480;
	private const int PreviewHeight = 270;
	private const int PhotoWidth = 1920;
	private const int PhotoHeight = 1080;
	private const int VideoWidth = 1280;
	private const int VideoHeight = 720;
	private const int VideoFps = 24;
	private const int VideoQuality = 80;
	private const int MaxQueuedFrames = 6;
	private const float LetterboxBand = 0.12f;
	private const float MoveStep = 0.25f;
	private const float TurnStep = 10f;

	private readonly List<string> shots = new List<string>();

	private MenuContext context;
	private CamSettings settings;
	private CamTablet tablet;
	private Camera lens;
	private RenderTexture previewTexture;
	private RenderTexture photoTexture;
	private RenderTexture videoTexture;
	private Camera shoulder;
	private Behaviour shoulderBrain;
	private float shoulderFov;
	private float shoulderNear;
	private Rect shoulderRect;
	private bool shoulderHooked;
	private bool shoulderSearched;
	private bool running;
	private Vector3 position;
	private Quaternion rotation = Quaternion.identity;
	private bool poseReady;
	private Vector3 followPoint;
	private Vector3 freePosition;
	private Quaternion freeRotation = Quaternion.identity;
	private Vector3 offsetAngles;
	private float orbitAngle;
	private CamMode lastMode = (CamMode)(-1);
	private int frame;
	private bool shooting;
	private float flash;
	private int presetIndex;
	private int galleryIndex;
	private Texture2D galleryTexture;
	private string galleryLoaded;
	private bool shotsDirty = true;
	private volatile string pendingStatus;
	private volatile bool pendingShots;
	private BlockingCollection<byte[]> videoQueue;
	private Thread videoThread;
	private float recordClock;
	private float recordStart;
	private bool recording;

	public CamSettings Settings => settings;

	public string Status { get; private set; } = "Ready";

	public bool Recording => recording;

	public bool Active => running;

	public float Flash => flash;

	public Texture PreviewTexture => recording && videoTexture != null ? (Texture)videoTexture : previewTexture;

	public bool Mirror => settings.CurrentMode == CamMode.Selfie;

	public int PresetIndex => presetIndex;

	public string ShotsFolder => Path.Combine(Paths.BepInExRootPath, "AceCam");

	public string StatusLine
	{
		get
		{
			if (!running)
			{
				return "Off";
			}

			if (recording)
			{
				return "REC " + AudioHandler.FormatTime(Time.unscaledTime - recordStart);
			}

			return CamSettings.ModeNames[(int)settings.CurrentMode] + "  " + Mathf.RoundToInt(settings.Fov.Value) + " fov";
		}
	}

	public void Initialize(MenuContext menuContext, CamSettings camSettings)
	{
		context = menuContext;
		settings = camSettings;
	}

	public void Toggle()
	{
		settings.Enabled.Value = !settings.Enabled.Value;
	}

	public void SetMode(CamMode mode)
	{
		settings.Mode.Value = (int)mode;
		if (!settings.Enabled.Value)
		{
			settings.Enabled.Value = true;
		}
	}

	public void SummonTablet()
	{
		if (!settings.Enabled.Value)
		{
			settings.Enabled.Value = true;
		}

		EnsureRunning();
		if (tablet != null)
		{
			tablet.PlaceInFront();
		}
	}

	public void StepPreset(int direction)
	{
		presetIndex = (presetIndex + direction + PresetNames.Length) % PresetNames.Length;
	}

	public void ApplyPreset()
	{
		switch (presetIndex)
		{
			case 1:
				Apply(CamMode.ThirdPerson, 105f, 0.4f, 3.5f, false);
				break;
			case 2:
				Apply(CamMode.ThirdPerson, 45f, 0.6f, 1.4f, false);
				break;
			case 3:
				Apply(CamMode.FirstPerson, 110f, 0.15f, 2.5f, false);
				break;
			case 4:
				Apply(CamMode.Orbit, 50f, 0.85f, 3f, true);
				break;
			case 5:
				Apply(CamMode.Selfie, 75f, 0.3f, 2.5f, false);
				break;
			case 6:
				Apply(CamMode.Follow, 70f, 0.5f, 4f, false);
				break;
			default:
				Apply(CamMode.ThirdPerson, 80f, 0.5f, 2.5f, false);
				break;
		}

		Status = "Preset " + PresetNames[presetIndex] + " applied";
	}

	private void Apply(CamMode mode, float fov, float smooth, float distance, bool cinematic)
	{
		settings.Fov.Value = fov;
		settings.Smooth.Value = smooth;
		settings.Distance.Value = distance;
		settings.Cinematic.Value = cinematic;
		offsetAngles = Vector3.zero;
		SetMode(mode);
	}

	public void ResetCamera()
	{
		settings.Reset();
		offsetAngles = Vector3.zero;
		poseReady = false;
		lastMode = (CamMode)(-1);
		Status = "Camera reset";
	}

	public void Move(Vector3 localDirection)
	{
		float step = MoveStep * settings.Speed.Value;
		if (settings.CurrentMode != CamMode.Free)
		{
			freePosition = position;
			freeRotation = rotation;
			settings.Mode.Value = (int)CamMode.Free;
			lastMode = CamMode.Free;
		}

		freePosition += freeRotation * localDirection * step;
	}

	public void Turn(Vector3 angles)
	{
		if (settings.CurrentMode == CamMode.Free)
		{
			freeRotation *= Quaternion.Euler(angles * TurnStep);
			return;
		}

		offsetAngles += angles * TurnStep;
	}

	public void CenterView()
	{
		offsetAngles = Vector3.zero;
		Transform head = PhysicsLayers.Head();
		if (head != null && settings.CurrentMode == CamMode.Free)
		{
			freePosition = head.position - PhysicsLayers.FlatForward(head) * settings.Distance.Value + Vector3.up * 0.4f;
			freeRotation = Quaternion.LookRotation(head.position - freePosition, Vector3.up);
		}
	}

	public void TakePhoto()
	{
		if (!settings.Enabled.Value)
		{
			settings.Enabled.Value = true;
		}

		EnsureRunning();
		if (!running || shooting || recording)
		{
			return;
		}

		StartCoroutine(Shoot());
	}

	public void ToggleRecording()
	{
		if (recording)
		{
			StopRecording();
			return;
		}

		if (!settings.Enabled.Value)
		{
			settings.Enabled.Value = true;
		}

		EnsureRunning();
		if (running && !shooting)
		{
			StartRecording();
		}
	}

	public int ShotCount
	{
		get
		{
			RefreshShots();
			return shots.Count;
		}
	}

	public string GalleryLabel
	{
		get
		{
			RefreshShots();
			if (shots.Count == 0)
			{
				return "No photos yet";
			}

			galleryIndex = Mathf.Clamp(galleryIndex, 0, shots.Count - 1);
			return (galleryIndex + 1) + " / " + shots.Count + "   " + Path.GetFileNameWithoutExtension(shots[galleryIndex]);
		}
	}

	public void StepGallery(int direction)
	{
		RefreshShots();
		if (shots.Count > 0)
		{
			galleryIndex = (galleryIndex + direction + shots.Count) % shots.Count;
		}
	}

	public Texture GalleryTexture()
	{
		RefreshShots();
		if (shots.Count == 0)
		{
			return null;
		}

		galleryIndex = Mathf.Clamp(galleryIndex, 0, shots.Count - 1);
		string thumb = ThumbPath(shots[galleryIndex]);
		if (galleryLoaded == thumb)
		{
			return galleryTexture;
		}

		galleryLoaded = thumb;
		try
		{
			if (!File.Exists(thumb))
			{
				return galleryTexture;
			}

			if (galleryTexture == null)
			{
				galleryTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "AceCamGallery" };
			}

			ImageConversion.LoadImage(galleryTexture, File.ReadAllBytes(thumb), false);
		}
		catch (Exception exception)
		{
			Status = "Could not open photo: " + exception.Message;
		}

		return galleryTexture;
	}

	public void DeleteShown()
	{
		RefreshShots();
		if (shots.Count == 0)
		{
			return;
		}

		galleryIndex = Mathf.Clamp(galleryIndex, 0, shots.Count - 1);
		string path = shots[galleryIndex];
		try
		{
			File.Delete(path);
			string thumb = ThumbPath(path);
			if (File.Exists(thumb))
			{
				File.Delete(thumb);
			}

			Status = "Deleted " + Path.GetFileName(path);
		}
		catch (Exception exception)
		{
			Status = "Could not delete: " + exception.Message;
		}

		shotsDirty = true;
		galleryLoaded = null;
	}

	public void OpenFolder()
	{
		try
		{
			Directory.CreateDirectory(ShotsFolder);
			Application.OpenURL("file:///" + ShotsFolder.Replace('\\', '/'));
			Status = "Opened the AceCam folder";
		}
		catch (Exception exception)
		{
			Status = "Could not open folder: " + exception.Message;
		}
	}

	private string ThumbPath(string shot)
	{
		return Path.Combine(ShotsFolder, "thumbs", Path.GetFileNameWithoutExtension(shot) + ".jpg");
	}

	private void RefreshShots()
	{
		if (!shotsDirty)
		{
			return;
		}

		shotsDirty = false;
		shots.Clear();
		try
		{
			if (Directory.Exists(ShotsFolder))
			{
				shots.AddRange(Directory.GetFiles(ShotsFolder, "*.png"));
				shots.Sort((a, b) => string.CompareOrdinal(b, a));
			}
		}
		catch (Exception exception)
		{
			Status = "Could not read photos: " + exception.Message;
		}
	}

	private void Update()
	{
		if (settings == null)
		{
			return;
		}

		if (pendingStatus != null)
		{
			Status = pendingStatus;
			pendingStatus = null;
			context.RequestRefresh();
		}

		if (pendingShots)
		{
			pendingShots = false;
			shotsDirty = true;
			galleryIndex = 0;
			galleryLoaded = null;
		}

		flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 3f);
		if (settings.Enabled.Value)
		{
			EnsureRunning();
		}
		else if (running)
		{
			Stop();
		}
	}

	private void LateUpdate()
	{
		if (!running || lens == null)
		{
			return;
		}

		Transform head = PhysicsLayers.Head();
		if (head == null)
		{
			return;
		}

		float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
		ComputeTarget(head, deltaTime, out Vector3 targetPosition, out Quaternion targetRotation);
		bool cinematic = settings.Cinematic.Value;
		float smooth = cinematic ? Mathf.Max(settings.Smooth.Value, 0.8f) : settings.Smooth.Value;
		if (!poseReady || smooth <= 0.01f)
		{
			position = targetPosition;
			rotation = targetRotation;
			poseReady = true;
		}
		else
		{
			float blend = 1f - Mathf.Exp(-Mathf.Lerp(40f, 1.4f, Mathf.Clamp01(smooth)) * deltaTime);
			position = Vector3.Lerp(position, targetPosition, blend);
			rotation = Quaternion.Slerp(rotation, targetRotation, blend);
		}

		float fov = Mathf.Clamp(settings.Fov.Value, CamSettings.MinFov, CamSettings.MaxFov);
		float near = Mathf.Clamp(settings.Near.Value, CamSettings.MinNear, CamSettings.MaxNear);
		lens.transform.SetPositionAndRotation(position, rotation);
		lens.fieldOfView = fov;
		lens.nearClipPlane = near;
		DriveShoulder(fov, near, cinematic);
		ScheduleRender(deltaTime);
		if (tablet != null)
		{
			tablet.Tick(deltaTime);
		}
	}

	private void ComputeTarget(Transform head, float deltaTime, out Vector3 targetPosition, out Quaternion targetRotation)
	{
		CamMode mode = settings.CurrentMode;
		float distance = Mathf.Clamp(settings.Distance.Value, CamSettings.MinDistance, CamSettings.MaxDistance);
		Vector3 flat = PhysicsLayers.FlatForward(head);
		if (mode != lastMode)
		{
			lastMode = mode;
			followPoint = poseReady ? position : head.position - flat * distance + Vector3.up * 0.5f;
			if (mode == CamMode.Free)
			{
				freePosition = poseReady ? position : followPoint;
				freeRotation = poseReady ? rotation : Quaternion.LookRotation(flat, Vector3.up);
			}
		}

		switch (mode)
		{
			case CamMode.FirstPerson:
				targetPosition = head.position;
				targetRotation = head.rotation;
				break;
			case CamMode.Follow:
				Vector3 away = followPoint - head.position;
				float limit = distance * 2f;
				if (away.magnitude > limit)
				{
					Vector3 wanted = head.position + away.normalized * limit;
					followPoint = Vector3.MoveTowards(followPoint, wanted, settings.Speed.Value * 4f * deltaTime);
				}

				targetPosition = followPoint;
				targetRotation = LookAt(followPoint, head.position, head.rotation);
				break;
			case CamMode.Tablet:
			case CamMode.Selfie:
				if (tablet != null)
				{
					tablet.LensPose(mode == CamMode.Selfie, out targetPosition, out targetRotation);
				}
				else
				{
					targetPosition = head.position;
					targetRotation = head.rotation;
				}

				break;
			case CamMode.Orbit:
				orbitAngle = Mathf.Repeat(orbitAngle + settings.Speed.Value * 18f * deltaTime, 360f);
				Vector3 ring = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * distance;
				targetPosition = head.position + ring + Vector3.up * 0.45f;
				targetRotation = LookAt(targetPosition, head.position, head.rotation);
				break;
			case CamMode.Free:
				targetPosition = freePosition;
				targetRotation = freeRotation;
				break;
			default:
				targetPosition = head.position - flat * distance + Vector3.up * (0.2f + distance * 0.12f);
				targetRotation = LookAt(targetPosition, head.position + Vector3.up * 0.05f, head.rotation);
				break;
		}

		if (mode != CamMode.Free)
		{
			targetRotation *= Quaternion.Euler(offsetAngles);
		}
	}

	private static Quaternion LookAt(Vector3 from, Vector3 to, Quaternion fallback)
	{
		Vector3 direction = to - from;
		return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction, Vector3.up) : fallback;
	}

	private void ScheduleRender(float deltaTime)
	{
		if (shooting)
		{
			return;
		}

		frame++;
		if (recording)
		{
			recordClock += deltaTime;
			bool capture = recordClock >= 1f / VideoFps;
			lens.enabled = capture;
			if (capture)
			{
				recordClock = Mathf.Min(recordClock - 1f / VideoFps, 1f / VideoFps);
				StartCoroutine(GrabVideoFrame());
			}

			return;
		}

		int every = PerformanceGuard.Level == 0 ? 3 : PerformanceGuard.Level == 1 ? 5 : 8;
		lens.enabled = tablet != null && tablet.PreviewVisible && frame % every == 0;
	}

	private void EnsureRunning()
	{
		if (running || context == null)
		{
			return;
		}

		Camera view = PhysicsLayers.ViewCamera();
		if (view == null || GorillaTagger.Instance == null)
		{
			return;
		}

		try
		{
			HookShoulder();
			GameObject lensObject = new GameObject("AceCamLens");
			DontDestroyOnLoad(lensObject);
			lens = lensObject.AddComponent<Camera>();
			lens.CopyFrom(shoulder != null ? shoulder : view);
			lens.stereoTargetEye = StereoTargetEyeMask.None;
			lens.rect = new Rect(0f, 0f, 1f, 1f);
			lens.depth = -50f;
			previewTexture = NewTexture(PreviewWidth, PreviewHeight, "AceCamPreview");
			lens.targetTexture = previewTexture;
			lens.ResetAspect();
			lens.enabled = false;
			tablet = CamTablet.Create(context, this);
			tablet.PlaceInFront();
			running = true;
			poseReady = false;
			lastMode = (CamMode)(-1);
			Status = "Camera on";
		}
		catch (Exception exception)
		{
			Debug.LogError("[AceUtilsPad] AceCam could not start: " + exception);
			Status = "AceCam could not start";
			settings.Enabled.Value = false;
			Stop();
		}
	}

	private static RenderTexture NewTexture(int width, int height, string name)
	{
		RenderTexture texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = name };
		texture.Create();
		return texture;
	}

	private void HookShoulder()
	{
		if (!shoulderSearched)
		{
			shoulderSearched = true;
			try
			{
				GorillaTagger tagger = GorillaTagger.Instance;
				FieldInfo field = typeof(GorillaTagger).GetField("thirdPersonCamera", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				GameObject holder = field != null && tagger != null ? field.GetValue(tagger) as GameObject : null;
				if (holder != null)
				{
					shoulder = holder.GetComponentInChildren<Camera>(true);
				}

				if (shoulder == null)
				{
					GameObject found = GameObject.Find("Shoulder Camera");
					shoulder = found != null ? found.GetComponent<Camera>() : null;
				}
			}
			catch (Exception exception)
			{
				Debug.LogWarning("[AceUtilsPad] AceCam could not find the spectator camera: " + exception.Message);
				shoulder = null;
			}
		}

		if (shoulder == null || shoulderHooked)
		{
			return;
		}

		shoulderBrain = shoulder.GetComponent("CinemachineBrain") as Behaviour;
		shoulderFov = shoulder.fieldOfView;
		shoulderNear = shoulder.nearClipPlane;
		shoulderRect = shoulder.rect;
		if (shoulderBrain != null)
		{
			shoulderBrain.enabled = false;
		}

		shoulderHooked = true;
	}

	private void DriveShoulder(float fov, float near, bool cinematic)
	{
		if (!shoulderHooked || shoulder == null)
		{
			return;
		}

		shoulder.transform.SetPositionAndRotation(position, rotation);
		shoulder.fieldOfView = fov;
		shoulder.nearClipPlane = near;
		Rect wanted = cinematic ? new Rect(0f, LetterboxBand, 1f, 1f - 2f * LetterboxBand) : new Rect(0f, 0f, 1f, 1f);
		if (shoulder.rect != wanted)
		{
			shoulder.rect = wanted;
		}
	}

	private void ReleaseShoulder()
	{
		if (!shoulderHooked)
		{
			return;
		}

		shoulderHooked = false;
		if (shoulder == null)
		{
			return;
		}

		shoulder.fieldOfView = shoulderFov;
		shoulder.nearClipPlane = shoulderNear;
		shoulder.rect = shoulderRect;
		if (shoulderBrain != null)
		{
			shoulderBrain.enabled = true;
		}
	}

	private void Stop()
	{
		if (recording)
		{
			StopRecording();
		}

		StopAllCoroutines();
		shooting = false;
		ReleaseShoulder();
		if (tablet != null)
		{
			tablet.Dispose();
			tablet = null;
		}

		if (lens != null)
		{
			lens.targetTexture = null;
			Destroy(lens.gameObject);
			lens = null;
		}

		Release(ref previewTexture);
		Release(ref photoTexture);
		Release(ref videoTexture);
		running = false;
		Status = "Camera off";
	}

	private static void Release(ref RenderTexture texture)
	{
		if (texture != null)
		{
			texture.Release();
			Destroy(texture);
			texture = null;
		}
	}

	private void OnDestroy()
	{
		Stop();
		if (galleryTexture != null)
		{
			Destroy(galleryTexture);
		}
	}

	private IEnumerator Shoot()
	{
		shooting = true;
		if (photoTexture == null)
		{
			photoTexture = NewTexture(PhotoWidth, PhotoHeight, "AceCamPhoto");
		}

		lens.targetTexture = photoTexture;
		lens.enabled = true;
		yield return new WaitForEndOfFrame();
		string name = "AceCam_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
		string folder = ShotsFolder;
		AsyncGPUReadback.Request(photoTexture, 0, TextureFormat.RGBA32, request => SavePhoto(request, folder, name));
		lens.targetTexture = previewTexture;
		yield return new WaitForEndOfFrame();
		AsyncGPUReadback.Request(previewTexture, 0, TextureFormat.RGBA32, request => SaveThumb(request, folder, name));
		flash = 1f;
		NotifySound.Play(3, context.Settings.SoundVolume.Value);
		shooting = false;
	}

	private void SavePhoto(AsyncGPUReadbackRequest request, string folder, string name)
	{
		if (request.hasError)
		{
			pendingStatus = "Photo failed on this graphics card";
			return;
		}

		byte[] raw = request.GetData<byte>().ToArray();
		Task.Run(() =>
		{
			try
			{
				Directory.CreateDirectory(folder);
				byte[] png = ImageConversion.EncodeArrayToPNG(raw, GraphicsFormat.R8G8B8A8_UNorm, PhotoWidth, PhotoHeight);
				File.WriteAllBytes(Path.Combine(folder, name + ".png"), png);
				pendingStatus = "Saved " + name + ".png";
				pendingShots = true;
			}
			catch (Exception exception)
			{
				pendingStatus = "Photo could not be saved: " + exception.Message;
			}
		});
	}

	private void SaveThumb(AsyncGPUReadbackRequest request, string folder, string name)
	{
		if (request.hasError)
		{
			return;
		}

		byte[] raw = request.GetData<byte>().ToArray();
		Task.Run(() =>
		{
			try
			{
				string thumbs = Path.Combine(folder, "thumbs");
				Directory.CreateDirectory(thumbs);
				byte[] jpg = ImageConversion.EncodeArrayToJPG(raw, GraphicsFormat.R8G8B8A8_UNorm, PreviewWidth, PreviewHeight, 0, 85);
				File.WriteAllBytes(Path.Combine(thumbs, name + ".jpg"), jpg);
				pendingShots = true;
			}
			catch (Exception)
			{
			}
		});
	}

	private void StartRecording()
	{
		try
		{
			string folder = Path.Combine(ShotsFolder, "videos");
			Directory.CreateDirectory(folder);
			string path = Path.Combine(folder, "AceCam_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".avi");
			if (videoTexture == null)
			{
				videoTexture = NewTexture(VideoWidth, VideoHeight, "AceCamVideo");
			}

			BlockingCollection<byte[]> queue = new BlockingCollection<byte[]>(MaxQueuedFrames);
			AviWriter writer = new AviWriter(path, VideoWidth, VideoHeight, VideoFps);
			videoQueue = queue;
			videoThread = new Thread(() => WriteVideo(queue, writer, path)) { IsBackground = true, Name = "AceCamVideo" };
			videoThread.Start();
			lens.targetTexture = videoTexture;
			recordClock = 0f;
			recordStart = Time.unscaledTime;
			recording = true;
			Status = "Recording...";
			NotifySound.Play(5, context.Settings.SoundVolume.Value);
		}
		catch (Exception exception)
		{
			recording = false;
			Status = "Recording could not start: " + exception.Message;
		}
	}

	private void StopRecording()
	{
		recording = false;
		BlockingCollection<byte[]> queue = videoQueue;
		videoQueue = null;
		videoThread = null;
		if (queue != null)
		{
			queue.CompleteAdding();
		}

		if (lens != null)
		{
			lens.targetTexture = previewTexture;
		}

		Status = "Saving video...";
	}

	private IEnumerator GrabVideoFrame()
	{
		yield return new WaitForEndOfFrame();
		BlockingCollection<byte[]> queue = videoQueue;
		if (!recording || queue == null || videoTexture == null)
		{
			yield break;
		}

		AsyncGPUReadback.Request(videoTexture, 0, TextureFormat.RGBA32, request =>
		{
			if (request.hasError)
			{
				return;
			}

			try
			{
				if (!queue.IsAddingCompleted)
				{
					queue.TryAdd(request.GetData<byte>().ToArray());
				}
			}
			catch (Exception)
			{
			}
		});
	}

	private void WriteVideo(BlockingCollection<byte[]> queue, AviWriter writer, string path)
	{
		try
		{
			foreach (byte[] raw in queue.GetConsumingEnumerable())
			{
				byte[] jpg = ImageConversion.EncodeArrayToJPG(raw, GraphicsFormat.R8G8B8A8_UNorm, VideoWidth, VideoHeight, 0, VideoQuality);
				writer.AddFrame(jpg);
			}

			int frames = writer.FrameCount;
			writer.Dispose();
			pendingStatus = "Saved video " + Path.GetFileName(path) + " (" + frames + " frames)";
		}
		catch (Exception exception)
		{
			pendingStatus = "Video failed: " + exception.Message;
			try
			{
				writer.Dispose();
			}
			catch (Exception)
			{
			}
		}
		finally
		{
			queue.Dispose();
		}
	}
}

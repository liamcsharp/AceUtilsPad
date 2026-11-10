using System.Collections.Generic;
using AUP.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace AUP;

public sealed class MenuController : MonoBehaviour
{
	private const float HandForwardOffset = 0.03f;
	private const float GripUnits = 23f;
	private const float PhoneGripMeters = 0.02f;
	private const float PhoneLift = 0.2f;
	private const float BannerGap = 8f;
	private const float MirrorInterval = 0.5f;
	private const float LaserRange = 8f;
	private const float LaserMinDistance = 0.12f;
	private const float LaserWidth = 0.006f;
	private const float MouseWorldRange = 40f;
	private const float WheelCooldown = 0.09f;

	private static readonly Quaternion HandTilt = Quaternion.Euler(125f, 0f, 10f);

	private static readonly Key[] PageKeys =
	{
		Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
	};

	private readonly MenuTouchInput touchInput = new MenuTouchInput();
	private readonly List<UiButton> touchTargets = new List<UiButton>();
	private readonly List<UiButton> screenTargets = new List<UiButton>();
	private readonly List<UiButton> worldTargets = new List<UiButton>();

	private MenuContext context;
	private IReadOnlyList<MenuPage> pages;
	private AlertHub alerts;
	private MenuNetworkDisplay networkDisplay;
	private OrbMenu orb;
	private MenuSurface worldSurface;
	private MenuSurface screenSurface;
	private AlertBanner worldBanner;
	private Placement placement = Placement.Wrist;
	private Vector3 anchor;
	private LineRenderer laser;
	private Transform laserDot;
	private Material laserMaterial;
	private Material laserDotMaterial;
	private UiButton hoveredScreen;
	private UiButton hoveredWorld;
	private UiButton hoveredLaser;
	private Keyboard hookedKeyboard;
	private bool toggleHeld;
	private bool triggerHeld;
	private bool orbModeSeen;
	private bool phoneModeSeen;
	private float nextMirrorTime;
	private float nextWheelTime;

	private enum Placement
	{
		Wrist,
		Deployed
	}

	public bool IsWorldMenuOpen => worldSurface != null && worldSurface.IsOpen;

	private bool ScreenOpen => screenSurface != null && screenSurface.IsOpen;

	public void Initialize(MenuContext menuContext, IReadOnlyList<MenuPage> menuPages, AlertHub alertHub, MenuNetworkDisplay display, OrbMenu orbMenu)
	{
		context = menuContext;
		pages = menuPages;
		alerts = alertHub;
		networkDisplay = display;
		orb = orbMenu;
		context.HideMenu = HideWorldMenu;
		context.SummonOrb = SummonOrb;
		context.OrbStatus = () => orb.StatusText;
		context.MenuOpen = () => IsWorldMenuOpen || ScreenOpen;
		context.ToggleMenu = ToggleAnyMenu;
		context.Text.Changed += context.RequestRefresh;
		orb.Deployed += OnOrbDeployed;
		orb.Recalled += OnOrbRecalled;
		worldBanner = alerts.Find(SurfaceMode.World);
		orbModeSeen = context.Settings.OrbMode.Value;
		phoneModeSeen = context.Settings.PhoneMode.Value;
		Application.onBeforeRender += PlaceWorldMenu;
	}

	public void ToggleWorldMenu()
	{
		EnsureWorldSurface();
		if (IsWorldMenuOpen)
		{
			if (placement == Placement.Wrist)
			{
				anchor = WristAnchor();
			}

			worldSurface.Close();
			touchInput.Reset();
			return;
		}

		if (placement == Placement.Deployed && orb.IsDeployed)
		{
			OpenDeployed();
			return;
		}

		OpenWrist();
	}

	private void OpenWrist()
	{
		EnsureWorldSurface();
		bool phone = context.Settings.PhoneMode.Value;
		placement = Placement.Wrist;
		worldSurface.PlacementScale = 1f;
		worldSurface.SetCurved(false);
		worldSurface.SetPhone(phone);
		worldSurface.SetHandle(!phone);
		if (phone)
		{
			worldSurface.ShowHome();
		}

		anchor = WristAnchor();
		worldSurface.Open();
		touchInput.Reset();
	}

	private void ToggleAnyMenu()
	{
		if (!SimpleInputs.Available || !UnityEngine.XR.XRSettings.isDeviceActive)
		{
			ToggleScreenMenu();
		}
		else if (context.Settings.OrbMode.Value)
		{
			orb.Summon();
		}
		else
		{
			ToggleWorldMenu();
		}
	}

	public void HideWorldMenu()
	{
		if (IsWorldMenuOpen)
		{
			worldSurface.Close();
			touchInput.Reset();
		}

		if (ScreenOpen)
		{
			screenSurface.Close();
		}
	}

	public void ToggleScreenMenu()
	{
		if (screenSurface == null)
		{
			screenSurface = MenuSurface.Create(SurfaceMode.Screen, context, pages);
		}

		if (ScreenOpen)
		{
			screenSurface.Close();
			return;
		}

		screenSurface.Open();
		ReleaseCursor();
	}

	public void Shutdown()
	{
		if (worldSurface != null)
		{
			Destroy(worldSurface.gameObject);
		}

		if (screenSurface != null)
		{
			Destroy(screenSurface.gameObject);
		}

		DestroyLaser();
		UnhookKeyboard();
		Application.onBeforeRender -= PlaceWorldMenu;
		alerts.DestroyAll();
		worldBanner = null;
		enabled = false;
	}

	private void OnDestroy()
	{
		Application.onBeforeRender -= PlaceWorldMenu;
		DestroyLaser();
		UnhookKeyboard();
	}

	private static void ReleaseCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	private void EnsureWorldSurface()
	{
		if (worldSurface == null)
		{
			worldSurface = MenuSurface.Create(SurfaceMode.World, context, pages);
		}
	}

	private void SummonOrb()
	{
		if (!context.Settings.OrbMode.Value)
		{
			context.Settings.OrbMode.Value = true;
		}

		orb.Summon();
	}

	private void OnOrbDeployed()
	{
		OpenDeployed();
	}

	private void OpenDeployed()
	{
		EnsureWorldSurface();
		if (IsWorldMenuOpen)
		{
			worldSurface.HideNow();
		}

		placement = Placement.Deployed;
		worldSurface.PlacementScale = OrbMenu.DeployScale;
		worldSurface.SetPhone(false);
		worldSurface.SetHandle(false);
		worldSurface.SetCurved(true);
		anchor = orb.OrbPosition;
		worldSurface.Open();
		touchInput.Reset();
	}

	private void OnOrbRecalled()
	{
		if (placement == Placement.Deployed && IsWorldMenuOpen)
		{
			anchor = orb.OrbPosition;
			worldSurface.Close();
			touchInput.Reset();
		}
	}

	private void Update()
	{
		if (context == null)
		{
			return;
		}

		UiMotion.Enabled = context.Settings.MenuAnimations.Value;
		WatchOrbMode();
		HookKeyboard();
		PollToggles();
		HandleKeyboard();
		bool mouseOnScreenMenu = HandleMouse();
		KeepNowPlayingFresh();
		PlaceWorldMenu();
		HandleTouch();
		HandleLaser();
		HandleMouseOnWorld(mouseOnScreenMenu);
		MirrorState();
	}

	private void WatchOrbMode()
	{
		MenuSettings settings = context.Settings;
		bool orbMode = settings.OrbMode.Value;
		bool phoneMode = settings.PhoneMode.Value;
		if (orbMode && !orbModeSeen && phoneMode)
		{
			settings.PhoneMode.Value = false;
			phoneMode = false;
		}
		else if (phoneMode && !phoneModeSeen && orbMode)
		{
			settings.OrbMode.Value = false;
			orbMode = false;
		}

		bool wristOpen = placement == Placement.Wrist && IsWorldMenuOpen;
		if (orbMode && !orbModeSeen && wristOpen)
		{
			worldSurface.Close();
			touchInput.Reset();
		}
		else if (phoneMode != phoneModeSeen && wristOpen && !orbMode)
		{
			worldSurface.HideNow();
			OpenWrist();
		}

		orbModeSeen = orbMode;
		phoneModeSeen = phoneMode;
	}

	private void PollToggles()
	{
		Keyboard keyboard = Keyboard.current;
		if (keyboard != null && !context.Text.Active)
		{
			if (keyboard.jKey.wasPressedThisFrame)
			{
				ToggleScreenMenu();
			}

			if (keyboard.gKey.wasPressedThisFrame && context.Settings.OrbMode.Value)
			{
				orb.Summon();
			}
		}

		if (!SimpleInputs.Available)
		{
			return;
		}

		bool pressed = SimpleInputs.LeftY;
		if (pressed && !toggleHeld)
		{
			if (context.Settings.OrbMode.Value)
			{
				orb.Summon();
			}
			else
			{
				ToggleWorldMenu();
			}
		}

		toggleHeld = pressed;
	}

	private void HookKeyboard()
	{
		Keyboard current = Keyboard.current;
		if (ReferenceEquals(current, hookedKeyboard))
		{
			return;
		}

		UnhookKeyboard();
		hookedKeyboard = current;
		if (hookedKeyboard != null)
		{
			hookedKeyboard.onTextInput += OnTextInput;
		}
	}

	private void UnhookKeyboard()
	{
		if (hookedKeyboard != null)
		{
			hookedKeyboard.onTextInput -= OnTextInput;
		}

		hookedKeyboard = null;
	}

	private void OnTextInput(char character)
	{
		if (context == null || !context.Text.Active || char.IsControl(character))
		{
			return;
		}

		context.Text.TypeRaw(character);
	}

	private void HandleKeyboard()
	{
		Keyboard keyboard = Keyboard.current;
		if (keyboard == null)
		{
			return;
		}

		if (context.Text.Active)
		{
			if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
			{
				context.Text.Commit();
			}
			else if (keyboard.escapeKey.wasPressedThisFrame)
			{
				context.Text.Cancel();
			}
			else if (keyboard.backspaceKey.wasPressedThisFrame)
			{
				context.Text.Backspace();
			}

			return;
		}

		if (!ScreenOpen)
		{
			return;
		}

		bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
		if (keyboard.escapeKey.wasPressedThisFrame)
		{
			screenSurface.Close();
		}
		else if (keyboard.tabKey.wasPressedThisFrame)
		{
			StepPage(shift ? -1 : 1);
		}
		else if (keyboard.downArrowKey.wasPressedThisFrame)
		{
			StepPage(1);
		}
		else if (keyboard.upArrowKey.wasPressedThisFrame)
		{
			StepPage(-1);
		}
		else if (keyboard.eKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
		{
			screenSurface.ScrollSections(1);
		}
		else if (keyboard.qKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
		{
			screenSurface.ScrollSections(-1);
		}
		else
		{
			for (int i = 0; i < PageKeys.Length && i < context.Nav.PageCount; i++)
			{
				KeyControl key = keyboard[PageKeys[i]];
				if (key != null && key.wasPressedThisFrame)
				{
					context.Nav.GoToPage(i);
					Feedback.Click();
					break;
				}
			}
		}
	}

	private void StepPage(int direction)
	{
		int count = context.Nav.PageCount;
		context.Nav.GoToPage((context.Nav.Page + direction + count) % count);
		Feedback.Click();
	}

	private void KeepNowPlayingFresh()
	{
		if (IsWorldMenuOpen || ScreenOpen)
		{
			context.Audio.RefreshNowPlaying();
		}
	}

	private bool HandleMouse()
	{
		AlertBanner screenBanner = alerts.Find(SurfaceMode.Screen);
		bool bannerOpen = screenBanner != null && screenBanner.IsShowing;
		Mouse mouse = Mouse.current;
		bool screenOpen = ScreenOpen && screenSurface.IsInteractive;
		if (mouse == null || !screenOpen && !bannerOpen)
		{
			SetHover(ref hoveredScreen, null);
			return false;
		}

		screenTargets.Clear();
		if (screenOpen)
		{
			screenSurface.CollectTouchTargets(screenTargets);
		}

		if (bannerOpen)
		{
			screenBanner.CollectTouchTargets(screenTargets);
		}

		Vector2 point = mouse.position.ReadValue();
		UiButton hovered = null;
		foreach (UiButton button in screenTargets)
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(button.Rect, point, null))
			{
				hovered = button;
				break;
			}
		}

		SetHover(ref hoveredScreen, hovered);
		bool overMenu = hovered != null || screenOpen && IsOverScreenPanel(point);
		if (mouse.leftButton.wasPressedThisFrame && hovered != null)
		{
			hovered.PressAt(new Vector3(point.x, point.y, 0f));
		}

		if (screenOpen && overMenu && Time.unscaledTime >= nextWheelTime)
		{
			float scroll = mouse.scroll.ReadValue().y;
			if (Mathf.Abs(scroll) > 0.01f)
			{
				nextWheelTime = Time.unscaledTime + WheelCooldown;
				int direction = scroll > 0f ? -1 : 1;
				if (RectTransformUtility.RectangleContainsScreenPoint(screenSurface.RailPanel, point, null))
				{
					screenSurface.ScrollRail(direction);
				}
				else
				{
					screenSurface.ScrollSections(direction);
				}
			}
		}

		return overMenu;
	}

	private bool IsOverScreenPanel(Vector2 point)
	{
		return RectTransformUtility.RectangleContainsScreenPoint(screenSurface.MainPanel, point, null)
			   || RectTransformUtility.RectangleContainsScreenPoint(screenSurface.RailPanel, point, null);
	}

	private void HandleMouseOnWorld(bool blocked)
	{
		Mouse mouse = Mouse.current;
		Camera camera = PhysicsLayers.ViewCamera();
		bool active = !blocked && mouse != null && camera != null && Cursor.lockState != CursorLockMode.Locked;
		if (!active)
		{
			SetHover(ref hoveredWorld, null);
			return;
		}

		Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
		RaycastWorldMenu(ray, MouseWorldRange, out UiButton button, out Vector3 point, out _);
		SetHover(ref hoveredWorld, button);
		if (button != null && mouse.leftButton.wasPressedThisFrame)
		{
			button.PressAt(point);
		}
	}

	private void HandleLaser()
	{
		Transform hand = Hands.Local(HandSide.Right);
		bool active = hand != null && SimpleInputs.Available && !SimpleInputs.RightGrab;
		if (!active)
		{
			HideLaser();
			SetHover(ref hoveredLaser, null);
			triggerHeld = SimpleInputs.RightTrigger;
			return;
		}

		Ray ray = new Ray(hand.position, hand.forward);
		bool hit = RaycastWorldMenu(ray, LaserRange, out UiButton button, out Vector3 point, out float distance);
		bool trigger = SimpleInputs.RightTrigger;
		bool clicked = trigger && !triggerHeld;
		triggerHeld = trigger;
		bool wristOnly = placement == Placement.Wrist && button != null && worldSurface != null && button.Rect.IsChildOf(worldSurface.transform);
		if (!hit || distance < LaserMinDistance || wristOnly)
		{
			HideLaser();
			SetHover(ref hoveredLaser, null);
			return;
		}

		SetHover(ref hoveredLaser, button);
		DrawLaser(ray.origin, point, button != null);
		if (clicked && button != null)
		{
			button.PressAt(point);
		}
	}

	private bool RaycastWorldMenu(Ray ray, float maxDistance, out UiButton button, out Vector3 point, out float distance)
	{
		button = null;
		point = Vector3.zero;
		distance = float.MaxValue;
		worldTargets.Clear();
		bool menuActive = IsWorldMenuOpen && worldSurface.IsInteractive;
		if (menuActive)
		{
			worldSurface.CollectTouchTargets(worldTargets);
		}

		if (worldBanner != null && worldBanner.IsShowing)
		{
			worldBanner.CollectTouchTargets(worldTargets);
		}

		WorldUi.Collect(worldTargets);
		foreach (UiButton candidate in worldTargets)
		{
			if (IntersectRect(ray, candidate.Rect, maxDistance, 0f, out Vector3 hit, out float t) && t < distance)
			{
				distance = t;
				point = hit;
				button = candidate;
			}
		}

		if (button != null)
		{
			return true;
		}

		if (!menuActive || placement != Placement.Deployed)
		{
			return false;
		}

		TryPanel(ray, worldSurface.MainPanel, maxDistance, ref point, ref distance);
		TryPanel(ray, worldSurface.RailPanel, maxDistance, ref point, ref distance);
		if (worldSurface.GlanceVisible)
		{
			TryPanel(ray, worldSurface.GlancePanel, maxDistance, ref point, ref distance);
		}

		return distance < float.MaxValue;
	}

	private static void TryPanel(Ray ray, RectTransform panel, float maxDistance, ref Vector3 point, ref float distance)
	{
		if (panel != null && IntersectRect(ray, panel, maxDistance, 0f, out Vector3 hit, out float t) && t < distance)
		{
			distance = t;
			point = hit;
		}
	}

	private static bool IntersectRect(Ray ray, RectTransform rect, float maxDistance, float margin, out Vector3 hit, out float distance)
	{
		hit = Vector3.zero;
		distance = 0f;
		if (rect == null || !rect.gameObject.activeInHierarchy)
		{
			return false;
		}

		Vector3 normal = rect.forward;
		float facing = Vector3.Dot(ray.direction, normal);
		if (Mathf.Abs(facing) < 0.0001f)
		{
			return false;
		}

		distance = Vector3.Dot(rect.position - ray.origin, normal) / facing;
		if (distance < 0f || distance > maxDistance)
		{
			return false;
		}

		hit = ray.origin + ray.direction * distance;
		Rect bounds = rect.rect;
		Vector3 local = rect.InverseTransformPoint(hit);
		return local.x >= bounds.xMin - margin && local.x <= bounds.xMax + margin && local.y >= bounds.yMin - margin && local.y <= bounds.yMax + margin;
	}

	private static void SetHover(ref UiButton current, UiButton next)
	{
		if (ReferenceEquals(current, next))
		{
			return;
		}

		if (current != null)
		{
			current.Hovered = false;
		}

		current = next;
		if (current != null)
		{
			current.Hovered = true;
		}
	}

	private Vector3 WristAnchor()
	{
		Transform hand = Hands.Local(HandSide.Left);
		return hand != null ? hand.position : anchor;
	}

	private void PlaceWorldMenu()
	{
		if (context == null || orb == null)
		{
			return;
		}

		bool worldActive = worldSurface != null && worldSurface.gameObject.activeSelf;
		float factor = context.Settings.SizeScale;
		if (placement == Placement.Deployed)
		{
			if (orb.State == OrbState.Deployed || orb.State == OrbState.Held || orb.State == OrbState.Floating)
			{
				anchor = orb.OrbPosition;
			}

			Quaternion rotation = orb.MenuRotation;
			float scale = factor * OrbMenu.DeployScale;
			if (worldActive)
			{
				Vector2 offset = worldSurface.MainOffset * (MenuSurface.WorldScale * scale);
				Vector3 target = orb.MenuCenter - rotation * new Vector3(offset.x, offset.y, 0f);
				worldSurface.transform.SetPositionAndRotation(Vector3.LerpUnclamped(anchor, target, worldSurface.Reveal), rotation);
			}

			PlaceBanner(orb.MenuCenter, rotation, scale, OrbMenu.DeployScale);
			return;
		}

		Transform hand = Hands.Local(HandSide.Left);
		if (hand == null)
		{
			return;
		}

		bool phone = worldSurface != null && worldSurface.IsPhone;
		Vector2 size = worldSurface != null ? worldSurface.CanvasSize : new Vector2(MenuSurface.TotalWidth, MenuSurface.MainHeight);
		Vector2 mainOffset = worldSurface != null ? worldSurface.MainOffset : new Vector2((MenuSurface.RailWidth + MenuSurface.RailGap) * 0.5f, 0f);
		float unit = MenuSurface.WorldScale * factor;
		Quaternion handRotation = hand.rotation * HandTilt;
		Vector3 handRight = handRotation * Vector3.right;
		Vector3 handUp = handRotation * Vector3.up;
		float slide = (phone ? PhoneGripMeters : GripUnits * unit) + size.x * 0.5f * unit + context.Settings.WristOffset.Value;
		Vector3 canvasCenter = hand.position + hand.forward * HandForwardOffset + handRight * slide;
		if (phone)
		{
			canvasCenter += handUp * (size.y * PhoneLift * unit);
		}

		Vector3 mainCenter = canvasCenter + handRight * (mainOffset.x * unit) + handUp * (mainOffset.y * unit);
		anchor = hand.position;
		if (worldActive)
		{
			worldSurface.transform.SetPositionAndRotation(Vector3.LerpUnclamped(anchor, canvasCenter, worldSurface.Reveal), handRotation);
		}

		Vector3 bannerBase = phone ? mainCenter + handUp * ((MenuSurface.PhoneStatusHeight + MenuSurface.PhoneBezel) * unit) : mainCenter;
		PlaceBanner(bannerBase, handRotation, factor, 1f);
	}

	private void PlaceBanner(Vector3 mainCenter, Quaternion rotation, float scale, float placementScale)
	{
		if (worldBanner == null || !worldBanner.IsShowing)
		{
			return;
		}

		worldBanner.PlacementScale = placementScale;
		float lift = (MenuSurface.MainHeight + AlertBanner.Height) * 0.5f + BannerGap;
		Vector3 bannerCenter = mainCenter + rotation * Vector3.up * (lift * MenuSurface.WorldScale * scale);
		worldBanner.transform.SetPositionAndRotation(bannerCenter, rotation);
	}

	private void HandleTouch()
	{
		touchTargets.Clear();
		Transform reference = null;

		if (IsWorldMenuOpen && worldSurface.IsInteractive)
		{
			worldSurface.CollectTouchTargets(touchTargets);
			reference = worldSurface.MainPanel;
		}

		if (worldBanner != null && worldBanner.IsShowing)
		{
			worldBanner.CollectTouchTargets(touchTargets);
			reference = reference != null ? reference : worldBanner.transform;
		}

		if (reference == null || !SimpleInputs.Available)
		{
			touchInput.Reset();
			return;
		}

		touchInput.Update(reference, touchTargets);
	}

	private void DrawLaser(Vector3 origin, Vector3 end, bool onButton)
	{
		EnsureLaser();
		Color accent = context.Theme.Current.Accent;
		Color color = onButton ? accent : new Color(accent.r, accent.g, accent.b, 0.55f);
		laser.gameObject.SetActive(true);
		laserDot.gameObject.SetActive(true);
		laser.SetPosition(0, origin);
		laser.SetPosition(1, end);
		laser.startColor = new Color(color.r, color.g, color.b, color.a * 0.25f);
		laser.endColor = color;
		laserDotMaterial.color = color;
		laserDot.position = end;
		laserDot.localScale = Vector3.one * (onButton ? 0.022f : 0.014f);
	}

	private void EnsureLaser()
	{
		if (laser == null)
		{
			laserMaterial = Materials.CreateUnlit(Color.white);
			GameObject lineObject = new GameObject("AUPMenuLaser");
			DontDestroyOnLoad(lineObject);
			laser = lineObject.AddComponent<LineRenderer>();
			laser.positionCount = 2;
			laser.startWidth = LaserWidth * 0.5f;
			laser.endWidth = LaserWidth;
			laser.material = laserMaterial;
			laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			laser.receiveShadows = false;
		}

		if (laserDot == null)
		{
			laserDotMaterial = Materials.CreateUnlit(Color.white);
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = "AUPMenuLaserDot";
			DontDestroyOnLoad(sphere);
			Destroy(sphere.GetComponent<Collider>());
			sphere.GetComponent<Renderer>().sharedMaterial = laserDotMaterial;
			laserDot = sphere.transform;
		}
	}

	private void HideLaser()
	{
		if (laser != null && laser.gameObject.activeSelf)
		{
			laser.gameObject.SetActive(false);
		}

		if (laserDot != null && laserDot.gameObject.activeSelf)
		{
			laserDot.gameObject.SetActive(false);
		}
	}

	private void DestroyLaser()
	{
		if (laser != null)
		{
			Destroy(laser.gameObject);
		}

		if (laserDot != null)
		{
			Destroy(laserDot.gameObject);
		}

		if (laserMaterial != null)
		{
			Destroy(laserMaterial);
		}

		if (laserDotMaterial != null)
		{
			Destroy(laserDotMaterial);
		}

		laser = null;
		laserDot = null;
		laserMaterial = null;
		laserDotMaterial = null;
	}

	private void MirrorState()
	{
		if (networkDisplay == null || Time.unscaledTime < nextMirrorTime)
		{
			return;
		}

		nextMirrorTime = Time.unscaledTime + MirrorInterval;
		bool open = IsWorldMenuOpen;
		string content = open ? worldSurface.CollectVisibleText() : string.Empty;
		networkDisplay.PublishLocalState(open, context.Nav.Page, content);
	}
}

using System;
using System.Collections.Generic;
using AUP.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

namespace AUP.Widgets;

public sealed class WidgetManager : MonoBehaviour
{
	private const float UnitScale = 0.0011f;
	private const float ScreenScale = 1.2f;
	private const float GrabRadius = 0.12f;
	private const float TouchRange = 0.4f;
	private const float RuleInterval = 2f;
	private const float RuleCooldown = 45f;
	private const float SlideUnits = 22f;
	private const int ScreenSortingOrder = 31990;

	private readonly List<Host> hosts = new List<Host>();
	private readonly List<Host> dragGroup = new List<Host>();
	private readonly Dictionary<string, float> ruleTimes = new Dictionary<string, float>();

	private MenuContext context;
	private WidgetStore store;
	private WidgetData data;
	private RectTransform screenRoot;
	private bool worldMode;
	private bool modeKnown;
	private int builtLayout = -1;
	private bool rebuildAll = true;
	private Vector3 hudPosition;
	private Quaternion hudRotation = Quaternion.identity;
	private bool hudReady;
	private Host dragged;
	private Transform dragHand;
	private bool dragRight;
	private bool leftHeld;
	private bool rightHeld;
	private Vector2 lastMouse;
	private UiButton hovered;
	private float nextRuleTime;
	private int lastPlayers = -1;
	private string lastSong;
	private string lastRoom;
	private bool rulesPrimed;
	private int selected;
	private int studioSelected;
	private int elementSelected;
	private int profileSelected;
	private int libraryPage;
	private string search = string.Empty;

	public WidgetStore Store => store;

	public WidgetProfile Profile => store.Profile;

	public string Search => search;

	public int LibraryPage => libraryPage;

	public void Initialize(MenuContext menuContext)
	{
		context = menuContext;
		store = new WidgetStore();
		data = new WidgetData(menuContext)
		{
			Profile = () => store.Profile,
			Favorites = FavoriteWidgets,
			ToggleWidget = ToggleEnabled,
			Changed = () => store.MarkDirty()
		};
		WorldUi.Register(CollectWorld);
	}

	public int MenuStyle => context.Settings.OrbMode.Value ? 2 : context.Settings.PhoneMode.Value ? 1 : 0;

	public int ActiveLayoutIndex
	{
		get
		{
			int styled = Profile.StyleLayouts[MenuStyle];
			return Mathf.Clamp(styled >= 0 ? styled : Profile.Active, 0, Profile.Layouts.Count - 1);
		}
	}

	public WidgetLayout Layout => Profile.Layouts[ActiveLayoutIndex];

	public List<WidgetConfig> Widgets => Layout.Widgets;

	public WidgetConfig Selected
	{
		get
		{
			List<WidgetConfig> widgets = Widgets;
			selected = Mathf.Clamp(selected, 0, widgets.Count - 1);
			return widgets[selected];
		}
	}

	public string SelectedName => Name(Selected);

	public static string Name(WidgetConfig config)
	{
		return string.IsNullOrEmpty(config.Title) ? WidgetOptions.KindNames[config.Kind] : config.Title;
	}

	public void StepSelected(int direction)
	{
		selected = WidgetOptions.Wrap(selected + direction, Widgets.Count);
	}

	public void Select(WidgetConfig config)
	{
		int index = Widgets.IndexOf(config);
		if (index >= 0)
		{
			selected = index;
		}
	}

	public void SetLayout(int index)
	{
		Profile.Active = Mathf.Clamp(index, 0, Profile.Layouts.Count - 1);
		Profile.StyleLayouts[MenuStyle] = -1;
		Touch(true);
	}

	public void StepStyleLayout(int style, int direction)
	{
		int count = Profile.Layouts.Count;
		int value = Profile.StyleLayouts[style] + direction;
		Profile.StyleLayouts[style] = value < -1 ? count - 1 : value >= count ? -1 : value;
		Touch(true);
	}

	public string StyleLayoutName(int style)
	{
		int value = Profile.StyleLayouts[style];
		return value < 0 || value >= Profile.Layouts.Count ? "Shared" : Profile.Layouts[value].Name;
	}

	public void ResetLayout()
	{
		store.ResetLayout(ActiveLayoutIndex);
		Touch(true);
	}

	public void ResetAll()
	{
		store.ResetAll();
		Touch(true);
	}

	public void Touch(bool everything)
	{
		store.MarkDirty();
		if (everything)
		{
			rebuildAll = true;
		}

		context.RequestRefresh();
	}

	public void Restyle(WidgetConfig config)
	{
		store.MarkDirty();
		Host host = Find(config);
		if (host != null)
		{
			BuildView(host);
		}

		if (config.KindValue != WidgetKind.Dock)
		{
			Host dock = hosts.Find(candidate => candidate.Config.KindValue == WidgetKind.Dock);
			if (dock != null)
			{
				BuildView(dock);
			}
		}

		context.RequestRefresh();
	}

	public void ToggleEnabled(WidgetConfig config)
	{
		config.Enabled = !config.Enabled;
		Restyle(config);
	}

	public void SetSearch(string value)
	{
		search = (value ?? string.Empty).Trim();
		libraryPage = 0;
		context.RequestRefresh();
	}

	public List<WidgetConfig> Library()
	{
		List<WidgetConfig> result = new List<WidgetConfig>();
		string needle = search.ToLowerInvariant();
		foreach (WidgetConfig config in Widgets)
		{
			if (needle.Length == 0 || Name(config).ToLowerInvariant().Contains(needle) || WidgetOptions.Categories[config.Kind].Contains(needle)
				|| WidgetOptions.GroupNames[config.Group].ToLowerInvariant().Contains(needle))
			{
				result.Add(config);
			}
		}

		return result;
	}

	public void StepLibraryPage(int direction, int pageSize)
	{
		int pages = Mathf.Max(1, Mathf.CeilToInt(Library().Count / (float)pageSize));
		libraryPage = WidgetOptions.Wrap(libraryPage + direction, pages);
	}

	public WidgetConfig LibraryItem(int slot, int pageSize)
	{
		List<WidgetConfig> library = Library();
		int index = libraryPage * pageSize + slot;
		return index >= 0 && index < library.Count ? library[index] : null;
	}

	public List<WidgetConfig> FavoriteWidgets()
	{
		List<WidgetConfig> favorites = new List<WidgetConfig>();
		foreach (WidgetConfig config in Widgets)
		{
			if (config.Favorite && config.KindValue != WidgetKind.Dock)
			{
				favorites.Add(config);
			}
		}

		return favorites;
	}

	public List<WidgetConfig> CustomWidgets()
	{
		return Widgets.FindAll(config => config.KindValue == WidgetKind.Custom);
	}

	public WidgetConfig StudioWidget
	{
		get
		{
			List<WidgetConfig> customs = CustomWidgets();
			if (customs.Count == 0)
			{
				return null;
			}

			studioSelected = Mathf.Clamp(studioSelected, 0, customs.Count - 1);
			return customs[studioSelected];
		}
	}

	public WidgetElement StudioElement
	{
		get
		{
			WidgetConfig widget = StudioWidget;
			if (widget == null || widget.Elements.Count == 0)
			{
				return null;
			}

			elementSelected = Mathf.Clamp(elementSelected, 0, widget.Elements.Count - 1);
			return widget.Elements[elementSelected];
		}
	}

	public int ElementIndex => elementSelected;

	public void StepStudio(int direction)
	{
		studioSelected = WidgetOptions.Wrap(studioSelected + direction, CustomWidgets().Count);
		elementSelected = 0;
	}

	public void StepElement(int direction)
	{
		WidgetConfig widget = StudioWidget;
		if (widget != null)
		{
			elementSelected = WidgetOptions.Wrap(elementSelected + direction, widget.Elements.Count);
		}
	}

	public void NewStudioWidget()
	{
		WidgetConfig created = store.AddCustom(Layout);
		if (created == null)
		{
			return;
		}

		PlaceInFront(created);
		studioSelected = CustomWidgets().Count - 1;
		elementSelected = 0;
		Touch(true);
	}

	public void RemoveStudioWidget()
	{
		WidgetConfig widget = StudioWidget;
		if (widget != null)
		{
			Widgets.Remove(widget);
			studioSelected = 0;
			elementSelected = 0;
			Touch(true);
		}
	}

	public void AddElement()
	{
		WidgetConfig widget = StudioWidget;
		if (widget == null || widget.Elements.Count >= 10)
		{
			return;
		}

		widget.Elements.Add(new WidgetElement { Type = 0, Text = "Label" });
		elementSelected = widget.Elements.Count - 1;
		Restyle(widget);
	}

	public void RemoveElement()
	{
		WidgetConfig widget = StudioWidget;
		if (widget != null && widget.Elements.Count > 0)
		{
			widget.Elements.RemoveAt(Mathf.Clamp(elementSelected, 0, widget.Elements.Count - 1));
			elementSelected = Mathf.Max(0, elementSelected - 1);
			Restyle(widget);
		}
	}

	public void MoveElementUp()
	{
		WidgetConfig widget = StudioWidget;
		if (widget != null && elementSelected > 0 && elementSelected < widget.Elements.Count)
		{
			WidgetElement moved = widget.Elements[elementSelected];
			widget.Elements.RemoveAt(elementSelected);
			widget.Elements.Insert(elementSelected - 1, moved);
			elementSelected--;
			Restyle(widget);
		}
	}

	public void StepElementType(int direction)
	{
		WidgetElement element = StudioElement;
		if (element != null)
		{
			element.Type = WidgetOptions.Wrap(element.Type + direction, WidgetOptions.ElementTypes.Length);
			element.Bind = 0;
			Restyle(StudioWidget);
		}
	}

	public void StepElementBind(int direction)
	{
		WidgetElement element = StudioElement;
		if (element != null)
		{
			element.Bind = WidgetOptions.Wrap(element.Bind + direction, WidgetData.BindCount(element));
			Restyle(StudioWidget);
		}
	}

	public string SelectedProfileName
	{
		get
		{
			List<string> names = store.SavedProfiles();
			if (names.Count == 0)
			{
				return null;
			}

			profileSelected = Mathf.Clamp(profileSelected, 0, names.Count - 1);
			return names[profileSelected];
		}
	}

	public void StepProfile(int direction)
	{
		profileSelected = WidgetOptions.Wrap(profileSelected + direction, store.SavedProfiles().Count);
	}

	public void LoadSelectedProfile()
	{
		string name = SelectedProfileName;
		if (name != null && store.LoadProfile(name))
		{
			Touch(true);
		}
	}

	public void ImportCode()
	{
		if (store.ImportCode())
		{
			Touch(true);
		}
	}

	public void SetAnchor(WidgetConfig config, int anchor)
	{
		Host host = Find(config);
		config.Anchor = WidgetOptions.Wrap(anchor, WidgetOptions.AnchorNames.Length);
		if (host != null && host.Holder != null && worldMode && host.Holder.gameObject.activeSelf)
		{
			StorePose(host);
		}
		else if (config.Anchor != 0)
		{
			PlaceInFront(config);
		}

		store.MarkDirty();
	}

	public void PlaceInFront(WidgetConfig config)
	{
		config.ScreenX = 0.5f;
		config.ScreenY = 0.5f;
		Transform head = PhysicsLayers.Head();
		if (head == null)
		{
			return;
		}

		Vector3 forward = PhysicsLayers.FlatForward(head);
		Vector3 position = head.position + forward * 0.6f + Vector3.down * 0.08f;
		Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
		WritePose(config, position, rotation);
		store.MarkDirty();
	}

	public void ResetSpot(WidgetConfig config)
	{
		store.PlaceAtSlot(config, Widgets.IndexOf(config), Profile.DefaultSpot);
		store.MarkDirty();
	}

	private Host Find(WidgetConfig config)
	{
		return hosts.Find(host => ReferenceEquals(host.Config, config));
	}

	private void OnDestroy()
	{
		WorldUi.Unregister(CollectWorld);
		if (store != null)
		{
			store.Flush();
		}

		DestroyHosts();
		if (screenRoot != null)
		{
			Destroy(screenRoot.gameObject);
		}
	}

	private void OnApplicationQuit()
	{
		if (store != null)
		{
			store.Flush();
		}
	}

	private void CollectWorld(List<UiButton> targets)
	{
		if (!worldMode)
		{
			return;
		}

		foreach (Host host in hosts)
		{
			if (host.Appear > 0.9f && !host.Behind && host.View != null)
			{
				foreach (UiButton button in host.View.Buttons)
				{
					if (button.CanPress)
					{
						targets.Add(button);
					}
				}
			}
		}
	}

	private void Update()
	{
		if (context == null)
		{
			return;
		}

		store.Tick();
		data.Tick();
		bool wantWorld = Profile.DisplayMode == 1 || Profile.DisplayMode == 0 && XRSettings.isDeviceActive;
		if (!modeKnown || wantWorld != worldMode)
		{
			modeKnown = true;
			worldMode = wantWorld;
			rebuildAll = true;
		}

		if (ActiveLayoutIndex != builtLayout)
		{
			rebuildAll = true;
		}

		if (rebuildAll)
		{
			RebuildAll();
		}

		float deltaTime = Time.unscaledDeltaTime;
		bool inLobby = context.Lobby.InRoom;
		bool menuOpen = context.MenuOpen != null && context.MenuOpen();
		bool editing = menuOpen && context.Pages != null && context.Nav != null && context.Nav.Page >= 0 && context.Nav.Page < context.Pages.Count
					   && (context.Pages[context.Nav.Page] is AUP.Pages.WidgetsPage || context.Pages[context.Nav.Page] is AUP.Pages.WidgetHubPage);
		bool allowed = Profile.Enabled && !(Profile.HideWithMenu && menuOpen && !editing) && !(Profile.LobbyOnly && !inLobby);
		float animationRate = 5f * Mathf.Clamp(Profile.AnimationSpeed, 0.25f, 3f);
		bool motion = context.Theme.MotionEnabled;
		float rate = WidgetOptions.RateFactors[Mathf.Clamp(Profile.UpdateRate, 0, WidgetOptions.RateFactors.Length - 1)];
		MenuTheme theme = context.Theme.Current;
		Vector3 fingertip = Vector3.zero;
		bool haveTip = worldMode && SimpleInputs.Available && FingerTip.TryGet(out fingertip);

		foreach (Host host in hosts)
		{
			WidgetConfig config = host.Config;
			bool want = allowed && config.Enabled && Condition(config.Visibility, inLobby, menuOpen);
			float target = want ? 1f : 0f;
			host.Appear = !motion || config.Animation == 0 ? target : Mathf.MoveTowards(host.Appear, target, deltaTime * animationRate);
			bool active = host.Appear > 0.001f;
			if (host.Holder.gameObject.activeSelf != active)
			{
				host.Holder.gameObject.SetActive(active);
				host.NextRefresh = 0f;
			}

			if (!active)
			{
				host.Touch.Reset();
				continue;
			}

			ApplyAppearance(host);
			if (Time.unscaledTime >= host.NextRefresh)
			{
				host.NextRefresh = Time.unscaledTime + PerformanceGuard.Interval(WidgetOptions.Intervals[config.Interval] * rate);
				host.View.Refresh(theme, Profile.GlobalAlpha);
			}

			host.View.Tick(deltaTime);
			if (haveTip && !host.Behind && host != dragged && (fingertip - host.Holder.position).sqrMagnitude < TouchRange * TouchRange)
			{
				host.Touch.Update(host.View.Root, host.View.Buttons);
			}
			else
			{
				host.Touch.Reset();
			}
		}

		if (worldMode)
		{
			HandleGrips();
		}
		else
		{
			HandleMouse();
		}

		if (Time.unscaledTime >= nextRuleTime)
		{
			nextRuleTime = Time.unscaledTime + RuleInterval;
			EvaluateRules();
		}
	}

	private static bool Condition(int visibility, bool inLobby, bool menuOpen)
	{
		switch (visibility)
		{
			case 1:
				return inLobby;
			case 2:
				return !inLobby;
			case 3:
				return !menuOpen;
			case 4:
				return menuOpen;
			default:
				return true;
		}
	}

	private void ApplyAppearance(Host host)
	{
		WidgetConfig config = host.Config;
		float eased = UiMotion.EaseOutCubic(host.Appear);
		float alpha = host.Behind ? 0f : eased;
		if (!Mathf.Approximately(host.View.Group.alpha, alpha))
		{
			host.View.Group.alpha = alpha;
		}

		float pop = config.Animation == 2 ? Mathf.Lerp(0.6f, 1f, UiMotion.EaseOutBack(host.Appear)) : 1f;
		float size = Mathf.Clamp(config.Scale, 0.4f, 2.5f) * Mathf.Clamp(Profile.GlobalScale, 0.5f, 2f) * pop;
		Vector2 slide = config.Animation == 3 ? new Vector2(0f, -(1f - eased) * SlideUnits) : Vector2.zero;
		if (host.View.Root.anchoredPosition != slide)
		{
			host.View.Root.anchoredPosition = slide;
		}

		if (worldMode)
		{
			float scale = UnitScale * size;
			if (!Mathf.Approximately(host.Holder.localScale.x, scale))
			{
				host.Holder.localScale = new Vector3(scale, scale, scale);
			}

			return;
		}

		RectTransform rect = (RectTransform)host.Holder;
		Vector2 position = new Vector2(Mathf.Clamp01(config.ScreenX) * Screen.width, Mathf.Clamp01(config.ScreenY) * Screen.height);
		if ((rect.anchoredPosition - position).sqrMagnitude > 0.01f)
		{
			rect.anchoredPosition = position;
		}

		float pixelScale = ScreenScale * size;
		if (!Mathf.Approximately(rect.localScale.x, pixelScale))
		{
			rect.localScale = new Vector3(pixelScale, pixelScale, 1f);
		}

		Quaternion roll = Quaternion.Euler(0f, 0f, config.Roll);
		if (rect.localRotation != roll)
		{
			rect.localRotation = roll;
		}
	}

	private void LateUpdate()
	{
		if (context == null || !worldMode)
		{
			return;
		}

		Transform head = PhysicsLayers.Head();
		if (head == null)
		{
			return;
		}

		Quaternion level = Quaternion.LookRotation(head.forward, Vector3.up);
		if (!hudReady)
		{
			hudReady = true;
			hudRotation = level;
		}
		else
		{
			float angle = Quaternion.Angle(hudRotation, level);
			float sharpness = angle > 50f ? 14f : 6f;
			hudRotation = Quaternion.Slerp(hudRotation, level, 1f - Mathf.Exp(-sharpness * Mathf.Min(Time.unscaledDeltaTime, 0.05f)));
		}

		hudPosition = head.position;
		Transform hand = Hands.Local(HandSide.Right);
		Vector3 viewer = head.position;
		foreach (Host host in hosts)
		{
			if (!host.Holder.gameObject.activeSelf)
			{
				continue;
			}

			if (host == dragged || dragGroup.Contains(host))
			{
				host.Behind = false;
				continue;
			}

			ReadPose(host.Config, hand, out Vector3 position, out Quaternion rotation);
			host.Holder.SetPositionAndRotation(position, rotation);
			host.Behind = host.Config.Anchor != 0 && Vector3.Dot(viewer - position, rotation * Vector3.forward) > 0f;
		}

		if (dragged != null && dragHand != null)
		{
			dragged.Holder.SetPositionAndRotation(dragHand.TransformPoint(dragged.HoldPosition), dragHand.rotation * dragged.HoldRotation);
			foreach (Host member in dragGroup)
			{
				member.Holder.SetPositionAndRotation(dragged.Holder.TransformPoint(member.HoldPosition), dragged.Holder.rotation * member.HoldRotation);
			}
		}
	}

	private void ReadPose(WidgetConfig config, Transform hand, out Vector3 position, out Quaternion rotation)
	{
		Vector3 local = new Vector3(config.X, config.Y, config.Z);
		switch (config.Anchor)
		{
			case 1:
				position = local;
				rotation = Quaternion.Euler(config.Pitch, config.Yaw, config.Roll);
				break;
			case 2:
				if (hand != null)
				{
					position = hand.TransformPoint(local);
					rotation = hand.rotation * Quaternion.Euler(config.Pitch, config.Yaw, config.Roll);
				}
				else
				{
					position = hudPosition + hudRotation * new Vector3(0.2f, -0.2f, 0.5f);
					rotation = hudRotation;
				}

				break;
			default:
				if (local.sqrMagnitude < 0.01f)
				{
					local = new Vector3(0f, 0f, 0.85f);
				}

				position = hudPosition + hudRotation * local;
				rotation = Quaternion.LookRotation(position - hudPosition, hudRotation * Vector3.up) * Quaternion.Euler(0f, 0f, config.Roll);
				break;
		}
	}

	private void WritePose(WidgetConfig config, Vector3 position, Quaternion rotation)
	{
		switch (config.Anchor)
		{
			case 1:
				config.X = position.x;
				config.Y = position.y;
				config.Z = position.z;
				WriteAngles(config, rotation.eulerAngles);
				break;
			case 2:
				Transform hand = Hands.Local(HandSide.Right);
				if (hand != null)
				{
					Vector3 local = hand.InverseTransformPoint(position);
					config.X = local.x;
					config.Y = local.y;
					config.Z = local.z;
					WriteAngles(config, (Quaternion.Inverse(hand.rotation) * rotation).eulerAngles);
				}

				break;
			default:
				Vector3 offset = Quaternion.Inverse(hudRotation) * (position - hudPosition);
				if (offset.magnitude < 0.25f)
				{
					offset = offset.sqrMagnitude > 0.0001f ? offset.normalized * 0.25f : new Vector3(0f, 0f, 0.25f);
				}

				config.X = offset.x;
				config.Y = offset.y;
				config.Z = offset.z;
				break;
		}
	}

	private static void WriteAngles(WidgetConfig config, Vector3 angles)
	{
		config.Pitch = angles.x;
		config.Yaw = angles.y;
		config.Roll = angles.z;
	}

	private void StorePose(Host host)
	{
		WritePose(host.Config, host.Holder.position, host.Holder.rotation);
	}

	private void HandleGrips()
	{
		bool left = SimpleInputs.LeftGrab;
		bool right = SimpleInputs.RightGrab;
		bool leftPressed = left && !leftHeld;
		bool rightPressed = right && !rightHeld;
		leftHeld = left;
		rightHeld = right;

		if (dragged != null)
		{
			if (dragRight ? !right : !left)
			{
				StorePose(dragged);
				foreach (Host member in dragGroup)
				{
					StorePose(member);
				}

				dragged = null;
				dragHand = null;
				dragGroup.Clear();
				store.MarkDirty();
			}

			return;
		}

		if (rightPressed)
		{
			TryGrab(HandSide.Right);
		}
		else if (leftPressed)
		{
			TryGrab(HandSide.Left);
		}
	}

	private void TryGrab(HandSide side)
	{
		Transform hand = Hands.Local(side);
		if (hand == null)
		{
			return;
		}

		Host best = null;
		float bestDistance = GrabRadius;
		foreach (Host host in hosts)
		{
			if (host.Config.Locked || host.Appear < 0.9f)
			{
				continue;
			}

			float distance = Vector3.Distance(hand.position, host.Holder.position);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				best = host;
			}
		}

		if (best == null)
		{
			return;
		}

		dragged = best;
		dragHand = hand;
		dragRight = side == HandSide.Right;
		best.HoldPosition = hand.InverseTransformPoint(best.Holder.position);
		best.HoldRotation = Quaternion.Inverse(hand.rotation) * best.Holder.rotation;
		dragGroup.Clear();
		if (best.Config.Group > 0)
		{
			foreach (Host host in hosts)
			{
				if (host != best && host.Config.Group == best.Config.Group && !host.Config.Locked && host.Holder.gameObject.activeSelf)
				{
					host.HoldPosition = best.Holder.InverseTransformPoint(host.Holder.position);
					host.HoldRotation = Quaternion.Inverse(best.Holder.rotation) * host.Holder.rotation;
					dragGroup.Add(host);
				}
			}
		}

		Select(best.Config);
		GorillaTagger tagger = GorillaTagger.Instance;
		if (tagger != null)
		{
			tagger.StartVibration(side == HandSide.Left, 0.25f, 0.04f);
		}
	}

	private void HandleMouse()
	{
		Mouse mouse = Mouse.current;
		if (mouse == null || Cursor.lockState == CursorLockMode.Locked)
		{
			SetHover(null);
			dragged = null;
			return;
		}

		Vector2 point = mouse.position.ReadValue();
		if (dragged != null)
		{
			if (!mouse.leftButton.isPressed)
			{
				dragged = null;
				dragGroup.Clear();
				store.MarkDirty();
				return;
			}

			Vector2 delta = point - lastMouse;
			lastMouse = point;
			MoveScreen(dragged.Config, delta);
			foreach (Host member in dragGroup)
			{
				MoveScreen(member.Config, delta);
			}

			return;
		}

		Host over = null;
		UiButton button = null;
		for (int i = hosts.Count - 1; i >= 0 && over == null; i--)
		{
			Host host = hosts[i];
			if (host.Appear < 0.9f || !RectTransformUtility.RectangleContainsScreenPoint(host.View.Root, point, null))
			{
				continue;
			}

			over = host;
			foreach (UiButton candidate in host.View.Buttons)
			{
				if (candidate.CanPress && RectTransformUtility.RectangleContainsScreenPoint(candidate.Rect, point, null))
				{
					button = candidate;
					break;
				}
			}
		}

		SetHover(button);
		if (over == null)
		{
			return;
		}

		float scroll = mouse.scroll.ReadValue().y;
		if (Mathf.Abs(scroll) > 0.01f && !over.Config.Locked)
		{
			over.Config.Scale = Mathf.Clamp(over.Config.Scale + Mathf.Sign(scroll) * 0.05f, 0.4f, 2.5f);
			store.MarkDirty();
		}

		if (!mouse.leftButton.wasPressedThisFrame)
		{
			return;
		}

		if (button != null)
		{
			button.PressAt(new Vector3(point.x, point.y, 0f));
			return;
		}

		Select(over.Config);
		if (over.Config.Locked)
		{
			return;
		}

		dragged = over;
		lastMouse = point;
		dragGroup.Clear();
		if (over.Config.Group > 0)
		{
			foreach (Host host in hosts)
			{
				if (host != over && host.Config.Group == over.Config.Group && !host.Config.Locked)
				{
					dragGroup.Add(host);
				}
			}
		}
	}

	private static void MoveScreen(WidgetConfig config, Vector2 delta)
	{
		config.ScreenX = Mathf.Clamp01(config.ScreenX + delta.x / Mathf.Max(1f, Screen.width));
		config.ScreenY = Mathf.Clamp01(config.ScreenY + delta.y / Mathf.Max(1f, Screen.height));
	}

	private void SetHover(UiButton next)
	{
		if (ReferenceEquals(hovered, next))
		{
			return;
		}

		if (hovered != null)
		{
			hovered.Hovered = false;
		}

		hovered = next;
		if (hovered != null)
		{
			hovered.Hovered = true;
		}
	}

	private void EvaluateRules()
	{
		int players = data.PlayerCount;
		string song = data.Song;
		string room = data.Room;
		if (rulesPrimed && Profile.Enabled)
		{
			foreach (WidgetConfig config in Widgets)
			{
				if (!config.Enabled || config.Rule <= 0)
				{
					continue;
				}

				string message = RuleMessage(config, players, song, room);
				if (message == null || ruleTimes.TryGetValue(config.Id, out float last) && Time.unscaledTime - last < RuleCooldown)
				{
					continue;
				}

				ruleTimes[config.Id] = Time.unscaledTime;
				if (context.Alerts != null)
				{
					context.Alerts.Post(new AlertMessage { Prefix = Name(config), IconId = WidgetOptions.KindIcons[config.Kind], IconColor = context.Theme.Current.Accent, Text = message, Seconds = 5f });
				}
			}
		}

		rulesPrimed = true;
		lastPlayers = players;
		lastSong = song;
		lastRoom = room;
	}

	private string RuleMessage(WidgetConfig config, int players, string song, string room)
	{
		int level = Mathf.Clamp(config.Rule, 1, 3) - 1;
		switch (config.KindValue)
		{
			case WidgetKind.Fps:
				float[] fpsLimits = { 30f, 45f, 60f };
				return context.Fps > 1f && context.Fps < fpsLimits[level] ? "FPS dropped to " + Mathf.RoundToInt(context.Fps) : null;
			case WidgetKind.Ping:
				int[] pingLimits = { 100, 150, 250 };
				return data.Ping > pingLimits[level] ? "Ping is up to " + data.Ping + " ms" : null;
			case WidgetKind.Battery:
				float[] batteryLimits = { 0.1f, 0.2f, 0.3f };
				float battery = data.BatteryLowest;
				return battery >= 0f && battery < batteryLimits[level] ? "Battery is down to " + Mathf.RoundToInt(battery * 100f) + "%" : null;
			case WidgetKind.Players:
				return room != null && room == lastRoom && players != lastPlayers ? "Player count is now " + players : null;
			case WidgetKind.Music:
				return song != null && song != lastSong ? "Now playing " + Rich.Escape(song) : null;
			case WidgetKind.Room:
				return room != lastRoom ? (room != null ? "Joined room " + Rich.Escape(room) : "Left the room") : null;
			default:
				return null;
		}
	}

	private void RebuildAll()
	{
		rebuildAll = false;
		builtLayout = ActiveLayoutIndex;
		dragged = null;
		dragHand = null;
		dragGroup.Clear();
		SetHover(null);
		DestroyHosts();
		if (!worldMode)
		{
			EnsureScreenRoot();
		}

		if (screenRoot != null)
		{
			screenRoot.gameObject.SetActive(!worldMode);
		}

		foreach (WidgetConfig config in Widgets)
		{
			Host host = new Host { Config = config };
			if (worldMode)
			{
				GameObject canvasObject = new GameObject("AceWidget_" + config.Id, typeof(RectTransform));
				DontDestroyOnLoad(canvasObject);
				Canvas canvas = canvasObject.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.WorldSpace;
				canvasObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1000f;
				((RectTransform)canvasObject.transform).sizeDelta = new Vector2(10f, 10f);
				canvasObject.transform.localScale = Vector3.one * UnitScale;
				host.Holder = canvasObject.transform;
			}
			else
			{
				RectTransform rect = UiKit.NewRect("AceWidget_" + config.Id, screenRoot);
				rect.anchorMin = Vector2.zero;
				rect.anchorMax = Vector2.zero;
				rect.pivot = new Vector2(0.5f, 0.5f);
				rect.sizeDelta = new Vector2(10f, 10f);
				rect.gameObject.AddComponent<Canvas>();
				host.Holder = rect;
			}

			BuildView(host);
			host.Holder.gameObject.SetActive(false);
			hosts.Add(host);
		}
	}

	private void BuildView(Host host)
	{
		if (host.View != null && host.View.Root != null)
		{
			Destroy(host.View.Root.gameObject);
		}

		WidgetConfig config = host.Config;
		host.View = new WidgetView(host.Holder, config, data.Rows(config),
			() => SetMinimized(config, true), () => SetMinimized(config, false), PlayClick);
		host.NextRefresh = 0f;
		host.Touch.Reset();
	}

	private void SetMinimized(WidgetConfig config, bool minimized)
	{
		config.Minimized = minimized;
		Restyle(config);
	}

	private void PlayClick()
	{
		NotifySound.Play(Profile.ClickSound, context.Settings.SoundVolume.Value * 0.6f);
		context.RequestRefresh();
		foreach (Host host in hosts)
		{
			host.NextRefresh = 0f;
		}
	}

	private void EnsureScreenRoot()
	{
		if (screenRoot != null)
		{
			return;
		}

		GameObject root = new GameObject("AceWidgetsScreen", typeof(RectTransform));
		DontDestroyOnLoad(root);
		Canvas canvas = root.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.overrideSorting = true;
		canvas.sortingOrder = ScreenSortingOrder;
		root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
		screenRoot = (RectTransform)root.transform;
	}

	private void DestroyHosts()
	{
		foreach (Host host in hosts)
		{
			if (host.Holder != null)
			{
				Destroy(host.Holder.gameObject);
			}
		}

		hosts.Clear();
	}

	private sealed class Host
	{
		public readonly MenuTouchInput Touch = new MenuTouchInput();
		public WidgetConfig Config;
		public Transform Holder;
		public WidgetView View;
		public float Appear;
		public float NextRefresh;
		public bool Behind;
		public Vector3 HoldPosition;
		public Quaternion HoldRotation = Quaternion.identity;
	}
}

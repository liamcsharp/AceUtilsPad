using System;
using System.Collections.Generic;
using System.Text;
using AUP.Pages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

public enum SurfaceMode
{
	World,
	Screen
}

public sealed class MenuSurface : MonoBehaviour
{
	public const float MainWidth = 360f;
	public const float MainHeight = 436f;
	public const float RailWidth = 48f;
	public const float RailGap = 6f;
	public const float TotalWidth = RailWidth + RailGap + MainWidth;
	public const float GlanceWidth = 150f;
	public const float GlanceGap = 8f;
	public const float WorldScale = 0.00125f;
	public const float ScreenScale = 1.35f;
	public const float ContentX = 14f;
	public const float ContentY = 70f;
	public const float ContentWidth = MainWidth - 2f * ContentX;
	public const float ContentHeight = MainHeight - ContentY - 44f;
	public const float PhoneBezel = 10f;
	public const float PhoneStatusHeight = 24f;
	public const float PhoneHomeBarHeight = 34f;
	public const float PhoneWidth = MainWidth + 2f * PhoneBezel;
	public const float PhoneHeight = MainHeight + PhoneStatusHeight + PhoneHomeBarHeight + 2f * PhoneBezel;
	public const float HandleReach = 34f;

	private const float RefreshInterval = 0.15f;
	private const float AnimationInterval = 0.05f;
	private const float RailIconSize = 36f;
	private const float RailPitch = 40f;
	private const float RailArrowHeight = 20f;
	private const float RailTop = 30f;
	private const float PagerHeight = 28f;
	private const float TabY = 40f;
	private const float TabHeight = 22f;
	private const float UnderlineY = 62f;
	private const float UnderlineHeight = 2.5f;
	private const float LogoSize = 34f;
	private const float OpenDuration = 0.42f;
	private const float CloseDuration = 0.17f;
	private const float SlideDuration = 0.26f;
	private const float SlideDistance = 34f;
	private const float RiseDistance = 16f;
	private const float RippleDuration = 0.5f;
	private const float RippleSize = 64f;
	private const float RailCurveAngle = 22f;
	private const float GlanceCurveAngle = 22f;
	private const int RipplePoolSize = 6;
	private const int PanelRadius = 16;
	private const int RailRadius = 14;
	private const int RailButtonRadius = 10;
	private const int ScreenSortingOrder = 32000;
	private const int MaxMirrorLength = 900;
	private const float BodyDepth = 16f;
	private const float BodyRim = 4f;
	private const float HomeDuration = 0.22f;
	private const float AppIconSize = 56f;
	private const float AppPitchX = 86f;
	private const float AppPitchY = 86f;
	private const int AppColumns = 4;

	private static readonly Vector2 ScreenOffset = new Vector2(-36f, -36f);

	private readonly List<PageView> pages = new List<PageView>();
	private readonly List<List<UiButton>> pageTabs = new List<List<UiButton>>();
	private readonly List<RectTransform> tabRoots = new List<RectTransform>();
	private readonly List<UiButton> railButtons = new List<UiButton>();
	private readonly HashSet<string> reportedFailures = new HashSet<string>();
	private readonly Ripple[] ripples = new Ripple[RipplePoolSize];

	private SurfaceMode mode;
	private MenuContext context;
	private RectTransform frame;
	private RectTransform main;
	private RectTransform rail;
	private RectTransform contentRoot;
	private CanvasGroup rootGroup;
	private CanvasGroup contentGroup;
	private Image mainBackground;
	private Image scenery;
	private GradientBackground gradient;
	private ParticleField particles;
	private Image railBackground;
	private Image railIndicator;
	private Image separator;
	private Image underline;
	private OrbEmblem emblem;
	private TextMeshProUGUI titleText;
	private TextMeshProUGUI cornerText;
	private RectTransform pagerRoot;
	private UiButton railUp;
	private UiButton railDown;
	private UiButton pagerPrevious;
	private UiButton pagerNext;
	private UiLabel pagerLabel;
	private RectTransform glance;
	private CanvasGroup glanceGroup;
	private Image glanceBackground;
	private PageView glanceView;
	private MenuTheme appliedTheme;
	private bool appliedParticles;
	private bool appliedScenery;
	private float gradientPhase;
	private float gradientClock;
	private float nextRefreshTime;
	private int railOffset;
	private bool dirty = true;
	private bool closing;
	private bool opening;
	private float animationClock;
	private float reveal = 1f;
	private float slideClock = -1f;
	private Vector2 slideDirection;
	private Vector2 contentBase;
	private int shownPage = -1;
	private int shownSection = -1;
	private float underlineX;
	private float underlineWidth;
	private bool underlineReady;
	private float indicatorY;
	private bool indicatorReady;
	private float curveTarget;
	private float curve;
	private int nextRipple;
	private readonly List<UiButton> appButtons = new List<UiButton>();
	private readonly List<TextMeshProUGUI> appLabels = new List<TextMeshProUGUI>();
	private MenuBody mainBody;
	private MenuBody railBody;
	private MenuBody glanceBody;
	private MenuBody handleBody;
	private MenuBody phoneBody;
	private RectTransform backRoot;
	private CanvasGroup backGroup;
	private TextMeshProUGUI backText;
	private RectTransform phoneShell;
	private Image phoneGlass;
	private Image phoneIsland;
	private TextMeshProUGUI phoneClock;
	private TextMeshProUGUI phoneStatus;
	private UiButton homePill;
	private RectTransform homeScreen;
	private CanvasGroup homeGroup;
	private Image homeBackground;
	private Image homeScenery;
	private TextMeshProUGUI homeClock;
	private TextMeshProUGUI homeDate;
	private bool phone;
	private bool homeShown = true;
	private float homeAmount = 1f;
	private bool handleWanted;
	private bool viewedFromBehind;
	private float animationAlpha = 1f;
	private Sprite appliedScenerySprite;

	public Transform PanelTransform => frame;

	public RectTransform MainPanel => main;

	public RectTransform RailPanel => rail;

	public RectTransform GlancePanel => glance;

	public bool GlanceVisible => glance != null && glance.gameObject.activeSelf && curve > 0.5f;

	public float PlacementScale { get; set; } = 1f;

	public bool IsPhone => phone;

	public Vector2 CanvasSize => phone ? new Vector2(PhoneWidth, PhoneHeight) : new Vector2(TotalWidth, MainHeight);

	public Vector2 MainOffset => phone
		? new Vector2(0f, (PhoneHomeBarHeight - PhoneStatusHeight) * 0.5f)
		: new Vector2((RailWidth + RailGap) * 0.5f, 0f);

	public float Reveal => reveal;

	public bool IsOpen => gameObject.activeSelf && !closing;

	public bool IsInteractive => IsOpen && !opening || IsOpen && reveal > 0.85f;

	public static MenuSurface Create(SurfaceMode surfaceMode, MenuContext menuContext, IReadOnlyList<MenuPage> menuPages)
	{
		GameObject root = new GameObject(surfaceMode == SurfaceMode.World ? "AUPMenuWorld" : "AUPMenuScreen");
		Canvas canvas = root.AddComponent<Canvas>();
		MenuSurface surface = root.AddComponent<MenuSurface>();
		surface.mode = surfaceMode;
		surface.context = menuContext;
		surface.rootGroup = root.AddComponent<CanvasGroup>();
		surface.rootGroup.interactable = false;
		surface.rootGroup.blocksRaycasts = false;
		surface.ConfigureCanvas(canvas);
		surface.Build(menuPages);
		surface.Subscribe();
		DontDestroyOnLoad(root);
		root.SetActive(false);
		return surface;
	}

	public void Open()
	{
		bool wasHidden = !gameObject.activeSelf || closing;
		if (!gameObject.activeSelf)
		{
			gameObject.SetActive(true);
		}

		closing = false;
		dirty = true;
		if (!wasHidden)
		{
			return;
		}

		if (!context.Theme.MotionEnabled)
		{
			opening = false;
			reveal = 1f;
			animationAlpha = 1f;
			ApplyScale();
			return;
		}

		opening = true;
		animationClock = 0f;
		reveal = 0.02f;
		animationAlpha = 0f;
		StartSlide(new Vector2(0f, -RiseDistance));
		emblem.Burst();
		ApplyScale();
	}

	public void Close()
	{
		if (!gameObject.activeSelf || closing)
		{
			return;
		}

		if (!context.Theme.MotionEnabled)
		{
			FinishClose();
			return;
		}

		opening = false;
		closing = true;
		animationClock = 0f;
	}

	public void HideNow()
	{
		if (gameObject.activeSelf)
		{
			FinishClose();
		}
	}

	public void SetCurved(bool curved)
	{
		curveTarget = curved ? 1f : 0f;
		if (!context.Theme.MotionEnabled)
		{
			curve = curveTarget;
			ApplyCurve();
		}
	}

	public void MarkDirty()
	{
		dirty = true;
	}

	public void SetPhone(bool enabled)
	{
		if (mode != SurfaceMode.World || phoneShell == null || phone == enabled)
		{
			return;
		}

		phone = enabled;
		((RectTransform)transform).sizeDelta = CanvasSize;
		if (phone)
		{
			UiKit.PlaceTopLeft(main, PhoneBezel, PhoneBezel + PhoneStatusHeight, MainWidth, MainHeight);
			homeShown = true;
			homeAmount = 1f;
		}
		else
		{
			UiKit.PlaceTopLeft(main, RailWidth + RailGap, 0f, MainWidth, MainHeight);
		}

		rail.gameObject.SetActive(!phone);
		phoneShell.gameObject.SetActive(phone);
		ApplyHome();
		ApplyBodies();
		dirty = true;
	}

	public void SetHandle(bool visible)
	{
		handleWanted = visible;
		ApplyBodies();
	}

	public void ShowHome()
	{
		if (phone)
		{
			homeShown = true;
			dirty = true;
		}
	}

	private void OpenApp(int index)
	{
		context.Nav.GoToPage(index);
		homeShown = false;
		dirty = true;
	}

	private void ApplyBodies()
	{
		if (mainBody == null)
		{
			return;
		}

		mainBody.SetVisible(!phone);
		railBody.SetVisible(!phone);
		handleBody.SetVisible(!phone && handleWanted);
		phoneBody.SetVisible(phone);
	}

	private void ApplyHome()
	{
		if (homeScreen == null)
		{
			return;
		}

		bool shown = phone && homeAmount > 0.001f;
		if (homeScreen.gameObject.activeSelf != shown)
		{
			homeScreen.gameObject.SetActive(shown);
		}

		if (!shown)
		{
			return;
		}

		float eased = UiMotion.EaseOutCubic(homeAmount);
		homeGroup.alpha = eased;
		float scale = Mathf.Lerp(1.12f, 1f, eased);
		homeScreen.localScale = new Vector3(scale, scale, 1f);
	}

	private void AdvanceHome(float deltaTime)
	{
		if (!phone)
		{
			return;
		}

		float target = homeShown ? 1f : 0f;
		if (Mathf.Approximately(homeAmount, target))
		{
			return;
		}

		homeAmount = context.Theme.MotionEnabled ? Mathf.MoveTowards(homeAmount, target, deltaTime / HomeDuration) : target;
		ApplyHome();
	}

	private void AdvanceFacing()
	{
		if (mode != SurfaceMode.World)
		{
			rootGroup.alpha = animationAlpha;
			return;
		}

		Camera camera = PhysicsLayers.ViewCamera();
		if (camera != null)
		{
			float side = Vector3.Dot(camera.transform.position - frame.position, frame.forward);
			viewedFromBehind = side > 0f;
		}

		if (mainBody != null)
		{
			mainBody.SetBehind(viewedFromBehind);
			railBody.SetBehind(viewedFromBehind);
			handleBody.SetBehind(viewedFromBehind);
			phoneBody.SetBehind(viewedFromBehind);
			glanceBody?.SetBehind(viewedFromBehind);
		}

		float alpha = viewedFromBehind ? 0f : animationAlpha;
		if (!Mathf.Approximately(rootGroup.alpha, alpha))
		{
			rootGroup.alpha = alpha;
		}

		float backAlpha = viewedFromBehind ? animationAlpha : 0f;
		if (backGroup != null && !Mathf.Approximately(backGroup.alpha, backAlpha))
		{
			backGroup.alpha = backAlpha;
		}
	}

	private static void Isolate(GameObject target)
	{
		if (target.GetComponent<Canvas>() == null)
		{
			target.AddComponent<Canvas>();
		}
	}

	public void CollectTouchTargets(List<UiButton> targets)
	{
		if (phone)
		{
			AddPressable(targets, homePill);
			if (homeShown)
			{
				AddPressable(targets, appButtons);
				return;
			}
		}
		else
		{
			AddPressable(targets, railButtons);
			AddPressable(targets, railUp, railDown);
		}

		AddPressable(targets, pagerPrevious, pagerNext);
		int page = CurrentPageIndex();
		if (page >= 0 && page < pageTabs.Count && pageTabs[page].Count > 1)
		{
			AddPressable(targets, pageTabs[page]);
		}

		SectionView section = CurrentSection();
		if (section != null)
		{
			AddPressable(targets, section.Buttons);
		}

		if (GlanceVisible && glanceView != null && glanceView.Sections.Count > 0)
		{
			AddPressable(targets, glanceView.Sections[0].Buttons);
		}
	}

	public string CollectVisibleText()
	{
		SectionView section = CurrentSection();
		if (section == null)
		{
			return string.Empty;
		}

		StringBuilder builder = new StringBuilder();
		foreach (UiLabel label in section.Labels)
		{
			string text = label.CurrentText;
			if (label.IsVisible && !string.IsNullOrWhiteSpace(text))
			{
				if (builder.Length > 0)
				{
					builder.Append('\n');
				}

				builder.Append(text.Trim());
			}
		}

		string result = builder.ToString();
		return result.Length > MaxMirrorLength ? result.Substring(0, MaxMirrorLength - 3) + "..." : result;
	}

	public void ScrollSections(int direction)
	{
		StepSection(direction);
	}

	public void ScrollRail(int direction)
	{
		railOffset += direction;
		LayoutRail();
		dirty = true;
	}

	private void ConfigureCanvas(Canvas canvas)
	{
		RectTransform rect = (RectTransform)transform;
		if (mode == SurfaceMode.World)
		{
			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = Camera.main;
			gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1000f;
			rect.sizeDelta = new Vector2(TotalWidth, MainHeight);
			rect.localScale = Vector3.one * WorldScale;
			return;
		}

		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.overrideSorting = true;
		canvas.sortingOrder = ScreenSortingOrder;
		gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
	}

	private void Build(IReadOnlyList<MenuPage> menuPages)
	{
		frame = UiKit.NewRect("Frame", transform);
		if (mode == SurfaceMode.World)
		{
			UiKit.Stretch(frame);
		}
		else
		{
			frame.anchorMin = Vector2.one;
			frame.anchorMax = Vector2.one;
			frame.pivot = Vector2.one;
			frame.anchoredPosition = ScreenOffset;
			frame.sizeDelta = new Vector2(TotalWidth, MainHeight);
			frame.localScale = Vector3.one * ScreenScale;
		}

		BuildMainPanel();
		BuildHeader();
		BuildPages(menuPages);
		BuildTabs();
		BuildPager();
		BuildRail(menuPages);
		if (mode == SurfaceMode.World)
		{
			BuildGlance();
		}

		if (mode == SurfaceMode.World)
		{
			BuildPhone(menuPages);
			BuildBodies();
		}

		BuildRipples();
		ApplyNavigation();
	}

	private void BuildPhone(IReadOnlyList<MenuPage> menuPages)
	{
		phoneShell = UiKit.NewRect("PhoneShell", frame);
		UiKit.PlaceTopLeft(phoneShell, 0f, 0f, PhoneWidth, PhoneHeight);
		phoneShell.SetAsFirstSibling();
		phoneGlass = phoneShell.gameObject.AddComponent<Image>();
		phoneGlass.sprite = SpriteFactory.Rounded(PanelRadius);
		phoneGlass.type = Image.Type.Sliced;
		phoneGlass.raycastTarget = false;
		phoneGlass.color = new Color(0.02f, 0.02f, 0.03f, 1f);

		phoneClock = UiKit.NewText("PhoneClock", phoneShell, 9.5f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
		UiKit.PlaceTopLeft(phoneClock.rectTransform, PhoneBezel + 12f, PhoneBezel, 110f, PhoneStatusHeight);
		phoneStatus = UiKit.NewText("PhoneStatus", phoneShell, 8.5f, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
		UiKit.PlaceTopLeft(phoneStatus.rectTransform, PhoneWidth - PhoneBezel - 142f, PhoneBezel, 130f, PhoneStatusHeight);
		phoneIsland = UiKit.NewImage("Island", phoneShell, SpriteFactory.Rounded(8), false);
		UiKit.PlaceTopLeft(phoneIsland.rectTransform, (PhoneWidth - 84f) * 0.5f, PhoneBezel + 4f, 84f, 16f);
		phoneIsland.color = new Color(0.07f, 0.07f, 0.09f, 1f);
		TextMeshProUGUI brand = UiKit.NewText("Brand", phoneIsland.rectTransform, 7f, TextAlignmentOptions.Center, FontStyles.Bold);
		UiKit.Stretch(brand.rectTransform);
		brand.text = "AceOne";
		brand.color = new Color(0.6f, 0.62f, 0.7f, 1f);

		ButtonSpec pillSpec = new ButtonSpec(string.Empty, ShowHome);
		homePill = ButtonFactory.Create(phoneShell, "HomePill", pillSpec, 132f, 12f, 6, 8f, context.RequestRefresh);
		UiKit.PlaceTopLeft(homePill.Rect, (PhoneWidth - 132f) * 0.5f, PhoneHeight - PhoneBezel - PhoneHomeBarHeight * 0.5f - 6f, 132f, 12f);

		homeScreen = UiKit.NewRect("Home", main);
		UiKit.Stretch(homeScreen);
		Isolate(homeScreen.gameObject);
		homeGroup = homeScreen.gameObject.AddComponent<CanvasGroup>();
		homeGroup.interactable = false;
		homeGroup.blocksRaycasts = false;
		homeBackground = UiKit.NewImage("HomeBackground", homeScreen, SpriteFactory.Rounded(PanelRadius), false);
		UiKit.Stretch(homeBackground.rectTransform);
		homeScenery = UiKit.NewImage("HomeScenery", homeScreen, null, false);
		homeScenery.type = Image.Type.Simple;
		UiKit.PlaceTopLeft(homeScenery.rectTransform, 0f, MainHeight - SceneryPainter.Height, SceneryPainter.Width, SceneryPainter.Height);
		homeClock = UiKit.NewText("HomeClock", homeScreen, 26f, TextAlignmentOptions.Center, FontStyles.Bold);
		UiKit.PlaceTopLeft(homeClock.rectTransform, 0f, 10f, MainWidth, 34f);
		homeDate = UiKit.NewText("HomeDate", homeScreen, 9f, TextAlignmentOptions.Center, FontStyles.Normal);
		UiKit.PlaceTopLeft(homeDate.rectTransform, 0f, 44f, MainWidth, 16f);

		float left = (MainWidth - AppColumns * AppPitchX) * 0.5f + (AppPitchX - AppIconSize) * 0.5f;
		for (int i = 0; i < menuPages.Count; i++)
		{
			int index = i;
			float x = left + i % AppColumns * AppPitchX;
			float y = 70f + i / AppColumns * AppPitchY;
			UiButton app = ButtonFactory.CreateIcon(homeScreen, "App_" + menuPages[i].Title, menuPages[i].IconId, AppIconSize, 16,
				() => OpenApp(index), null, context.RequestRefresh, ButtonStyle.Solid);
			UiKit.PlaceTopLeft(app.Rect, x, y, AppIconSize, AppIconSize);
			appButtons.Add(app);
			TextMeshProUGUI label = UiKit.NewText("AppLabel", homeScreen, 7.5f, TextAlignmentOptions.Top, FontStyles.Normal);
			label.text = menuPages[i].Title;
			label.textWrappingMode = TextWrappingModes.NoWrap;
			UiKit.PlaceTopLeft(label.rectTransform, x - 14f, y + AppIconSize + 2f, AppIconSize + 28f, 14f);
			appLabels.Add(label);
		}

		phoneShell.gameObject.SetActive(false);
		homeScreen.gameObject.SetActive(false);
	}

	private void BuildBodies()
	{
		float z = 1f + BodyDepth * 0.5f;
		Vector2 mainCenter = main.rect.center;
		mainBody = new MenuBody(main, "MainBody");
		mainBody.Add(new Vector3(mainCenter.x, mainCenter.y, z), MainWidth + 2f * BodyRim, MainHeight + 2f * BodyRim, BodyDepth, PanelRadius + BodyRim, 4f, 0);
		mainBody.Add(new Vector3(mainCenter.x, mainCenter.y - MainHeight * 0.32f, 1f + BodyDepth + 0.8f), MainWidth * 0.5f, 7f, 2f, 3f, 0.8f, 1);
		mainBody.Add(new Vector3(mainCenter.x + MainWidth * 0.5f - 22f, mainCenter.y + MainHeight * 0.5f - 22f, 1f + BodyDepth + 0.6f), 10f, 10f, 1.6f, 5f, 0.6f, 2);
		mainBody.Add(new Vector3(mainCenter.x - MainWidth * 0.5f + 22f, mainCenter.y + MainHeight * 0.5f - 22f, 1f + BodyDepth + 0.6f), 10f, 10f, 1.6f, 5f, 0.6f, 2);
		mainBody.Add(new Vector3(mainCenter.x + MainWidth * 0.5f - 22f, mainCenter.y - MainHeight * 0.5f + 22f, 1f + BodyDepth + 0.6f), 10f, 10f, 1.6f, 5f, 0.6f, 2);
		mainBody.Add(new Vector3(mainCenter.x - MainWidth * 0.5f + 22f, mainCenter.y - MainHeight * 0.5f + 22f, 1f + BodyDepth + 0.6f), 10f, 10f, 1.6f, 5f, 0.6f, 2);

		Vector2 railCenter = rail.rect.center;
		railBody = new MenuBody(rail, "RailBody");
		railBody.Add(new Vector3(railCenter.x, railCenter.y, z), RailWidth + 2f * BodyRim, MainHeight + 2f * BodyRim, BodyDepth, RailRadius + BodyRim, 4f, 0);

		handleBody = new MenuBody(rail, "HandleBody");
		float gripX = railCenter.x - RailWidth * 0.5f - HandleReach + 11f;
		handleBody.Add(new Vector3(gripX, railCenter.y, 1f + 11f), 22f, 200f, 22f, 11f, 5f, 3);
		handleBody.Add(new Vector3(gripX + 16f, railCenter.y + 72f, 1f + 9f), 30f, 18f, 12f, 6f, 3f, 1);
		handleBody.Add(new Vector3(gripX + 16f, railCenter.y - 72f, 1f + 9f), 30f, 18f, 12f, 6f, 3f, 1);

		if (glance != null)
		{
			Vector2 glanceCenter = glance.rect.center;
			glanceBody = new MenuBody(glance, "GlanceBody");
			glanceBody.Add(new Vector3(glanceCenter.x, glanceCenter.y, z), GlanceWidth + 2f * BodyRim, MainHeight + 2f * BodyRim, BodyDepth, PanelRadius + BodyRim, 4f, 0);
		}

		Vector2 shellCenter = phoneShell.rect.center;
		float phoneDepth = BodyDepth + 2f;
		float phoneBack = 1f + phoneDepth;
		phoneBody = new MenuBody(phoneShell, "PhoneBody");
		phoneBody.Add(new Vector3(shellCenter.x, shellCenter.y, 1f + phoneDepth * 0.5f), PhoneWidth + 2f * BodyRim, PhoneHeight + 2f * BodyRim, phoneDepth, PanelRadius + BodyRim + 6f, 5f, 0);
		phoneBody.Add(new Vector3(shellCenter.x + PhoneWidth * 0.5f + BodyRim + 1f, shellCenter.y + 70f, 1f + phoneDepth * 0.5f), 5f, 56f, 7f, 2.4f, 1.2f, 3);
		phoneBody.Add(new Vector3(shellCenter.x - PhoneWidth * 0.5f - BodyRim - 1f, shellCenter.y + 96f, 1f + phoneDepth * 0.5f), 5f, 34f, 7f, 2.4f, 1.2f, 3);
		phoneBody.Add(new Vector3(shellCenter.x - PhoneWidth * 0.5f - BodyRim - 1f, shellCenter.y + 52f, 1f + phoneDepth * 0.5f), 5f, 34f, 7f, 2.4f, 1.2f, 3);
		float bumpX = shellCenter.x + PhoneWidth * 0.5f - 62f;
		float bumpY = shellCenter.y + PhoneHeight * 0.5f - 62f;
		phoneBody.Add(new Vector3(bumpX, bumpY, phoneBack + 2f), 92f, 92f, 5f, 24f, 2f, 2);
		phoneBody.Add(new Vector3(bumpX - 20f, bumpY + 20f, phoneBack + 5.5f), 30f, 30f, 3f, 15f, 1.2f, 3);
		phoneBody.Add(new Vector3(bumpX + 20f, bumpY + 20f, phoneBack + 5.5f), 30f, 30f, 3f, 15f, 1.2f, 3);
		phoneBody.Add(new Vector3(bumpX - 20f, bumpY - 20f, phoneBack + 5.5f), 30f, 30f, 3f, 15f, 1.2f, 3);
		phoneBody.Add(new Vector3(bumpX + 20f, bumpY - 20f, phoneBack + 5f), 12f, 12f, 2f, 6f, 0.8f, 1);

		backRoot = UiKit.NewRect("Back", main);
		backRoot.anchorMin = new Vector2(0.5f, 0.5f);
		backRoot.anchorMax = new Vector2(0.5f, 0.5f);
		backRoot.pivot = new Vector2(0.5f, 0.5f);
		backRoot.sizeDelta = new Vector2(240f, 200f);
		backRoot.anchoredPosition3D = new Vector3(0f, 10f, phoneBack + 1.2f);
		backRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
		Isolate(backRoot.gameObject);
		backGroup = backRoot.gameObject.AddComponent<CanvasGroup>();
		backGroup.ignoreParentGroups = true;
		backGroup.interactable = false;
		backGroup.blocksRaycasts = false;
		backGroup.alpha = 0f;
		OrbEmblem backEmblem = OrbEmblem.Create(backRoot, 120f);
		backEmblem.Rect.anchorMin = new Vector2(0.5f, 0.5f);
		backEmblem.Rect.anchorMax = new Vector2(0.5f, 0.5f);
		backEmblem.Rect.pivot = new Vector2(0.5f, 0.5f);
		backEmblem.Rect.anchoredPosition = new Vector2(0f, 26f);
		backText = UiKit.NewText("BackName", backRoot, 15f, TextAlignmentOptions.Center, FontStyles.Bold);
		backText.text = PluginInfo.Name;
		backText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		backText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		backText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
		backText.rectTransform.sizeDelta = new Vector2(240f, 24f);
		backText.rectTransform.anchoredPosition = new Vector2(0f, -56f);
		ApplyBodies();
	}

	private void BuildMainPanel()
	{
		main = UiKit.NewRect("Main", frame);
		UiKit.PlaceTopLeft(main, RailWidth + RailGap, 0f, MainWidth, MainHeight);

		mainBackground = UiKit.NewImage("Backdrop", main, SpriteFactory.Rounded(PanelRadius), false);
		mainBackground.type = Image.Type.Sliced;
		UiKit.Stretch(mainBackground.rectTransform);
		Isolate(mainBackground.gameObject);
		gradient = mainBackground.gameObject.AddComponent<GradientBackground>();
		gradient.enabled = false;

		scenery = UiKit.NewImage("Scenery", main, null, false);
		scenery.type = Image.Type.Simple;
		UiKit.PlaceTopLeft(scenery.rectTransform, 0f, MainHeight - SceneryPainter.Height, SceneryPainter.Width, SceneryPainter.Height);
		particles = ParticleField.Create(main);
		Isolate(particles.gameObject);
	}

	private void BuildHeader()
	{
		emblem = OrbEmblem.Create(main, LogoSize);
		UiKit.PlaceTopLeft(emblem.Rect, ContentX - 4f, 4f, LogoSize, LogoSize);
		Isolate(emblem.gameObject);

		titleText = UiKit.NewText("Title", main, 14f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
		titleText.text = PluginInfo.Name;
		UiKit.PlaceTopLeft(titleText.rectTransform, ContentX + LogoSize, 9f, 200f, 24f);

		cornerText = UiKit.NewText("Corner", main, 8.5f, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
		UiKit.PlaceTopLeft(cornerText.rectTransform, MainWidth - ContentX - 120f, 11f, 120f, 20f);

		underline = UiKit.NewImage("Underline", main, SpriteFactory.Rounded(4), false);
		UiKit.PlaceTopLeft(underline.rectTransform, ContentX, UnderlineY, 40f, UnderlineHeight);
		Isolate(underline.gameObject);

		separator = UiKit.NewImage("Separator", main, null, false);
		UiKit.PlaceTopLeft(separator.rectTransform, ContentX, UnderlineY + UnderlineHeight + 1f, ContentWidth, 1f);
	}

	private void BuildPages(IReadOnlyList<MenuPage> menuPages)
	{
		contentRoot = UiKit.NewRect("Content", main);
		UiKit.PlaceTopLeft(contentRoot, ContentX, ContentY, ContentWidth, ContentHeight);
		contentBase = contentRoot.anchoredPosition;
		Isolate(contentRoot.gameObject);
		contentGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
		contentGroup.interactable = false;
		contentGroup.blocksRaycasts = false;

		foreach (MenuPage page in menuPages)
		{
			pages.Add(BuildView(page, contentRoot, ContentWidth, ContentHeight));
		}
	}

	private PageView BuildView(MenuPage page, RectTransform parent, float width, float height)
	{
		RectTransform pageRoot = UiKit.NewRect(page.Title, parent);
		UiKit.Stretch(pageRoot);
		PageView view = new PageView { Page = page, Root = pageRoot };
		PageBuilder builder = new PageBuilder(context, view, width, height);
		try
		{
			page.Build(builder);
		}
		catch (Exception exception)
		{
			Debug.LogError("[AceUtilsPad] Page '" + page.Title + "' failed to build: " + exception);
			builder.Label("This page failed to load. Check the BepInEx log.", 2, 9f);
		}

		builder.Finish();
		return view;
	}

	private void BuildTabs()
	{
		for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
		{
			PageView view = pages[pageIndex];
			RectTransform root = UiKit.NewRect("Tabs_" + view.Page.Title, main);
			UiKit.PlaceTopLeft(root, ContentX, TabY, ContentWidth, TabHeight);
			List<UiButton> tabs = new List<UiButton>();
			int count = view.Sections.Count;
			float maxWidth = ContentWidth / Mathf.Max(1, count);
			float x = 0f;
			for (int i = 0; i < count; i++)
			{
				int sectionIndex = i;
				string name = view.Sections[i].Name ?? view.Page.Title;
				ButtonSpec spec = new ButtonSpec(name, () => context.Nav.GoToSection(sectionIndex), () => context.Nav.Section == sectionIndex);
				UiButton tab = ButtonFactory.Create(root, "Tab", spec, 60f, TabHeight, 6, 9f, context.RequestRefresh, ButtonStyle.Tab);
				float preferred = MeasureText(tab.Label, name) + 18f;
				float width = Mathf.Clamp(preferred, 44f, Mathf.Max(44f, maxWidth));
				UiKit.PlaceTopLeft(tab.Rect, x, 0f, width, TabHeight);
				x += width + 2f;
				tabs.Add(tab);
			}

			root.gameObject.SetActive(false);
			pageTabs.Add(tabs);
			tabRoots.Add(root);
		}
	}

	private static float MeasureText(TextMeshProUGUI label, string text)
	{
		try
		{
			float width = label.GetPreferredValues(text, 400f, TabHeight).x;
			if (width > 0f && !float.IsNaN(width) && !float.IsInfinity(width))
			{
				return width;
			}
		}
		catch (Exception)
		{
		}

		return text.Length * 5.4f;
	}

	private void BuildPager()
	{
		pagerRoot = UiKit.NewRect("Pager", main);
		UiKit.PlaceTopLeft(pagerRoot, ContentX, MainHeight - 40f, ContentWidth, PagerHeight);

		pagerPrevious = AddPagerButton("<", -1, 0f);
		pagerNext = AddPagerButton(">", 1, ContentWidth - PageBuilder.StepperButtonWidth);

		TextMeshProUGUI label = UiKit.NewText("PagerLabel", pagerRoot, 9f, TextAlignmentOptions.Center, FontStyles.Normal);
		UiKit.PlaceTopLeft(label.rectTransform, PageBuilder.StepperButtonWidth + PageBuilder.RowGap, 0f,
			ContentWidth - 2f * (PageBuilder.StepperButtonWidth + PageBuilder.RowGap), PagerHeight);
		pagerLabel = new UiLabel(label, PagerText, Pick.Muted);
	}

	private UiButton AddPagerButton(string text, int direction, float x)
	{
		ButtonSpec spec = new ButtonSpec(text, () => StepSection(direction));
		UiButton button = ButtonFactory.Create(pagerRoot, "Pager" + text, spec, PageBuilder.StepperButtonWidth, PagerHeight, 8, 10f, context.RequestRefresh);
		UiKit.PlaceTopLeft(button.Rect, x, 0f, PageBuilder.StepperButtonWidth, PagerHeight);
		return button;
	}

	private void BuildRail(IReadOnlyList<MenuPage> menuPages)
	{
		rail = UiKit.NewRect("Rail", frame);
		UiKit.PlaceTopLeft(rail, 0f, 0f, RailWidth, MainHeight);
		rail.pivot = new Vector2(1f, 1f);
		rail.anchoredPosition = new Vector2(RailWidth, 0f);
		railBackground = rail.gameObject.AddComponent<Image>();
		railBackground.sprite = SpriteFactory.Rounded(RailRadius);
		railBackground.type = Image.Type.Sliced;

		railIndicator = UiKit.NewImage("Indicator", rail, SpriteFactory.Rounded(RailButtonRadius), false);
		Isolate(railIndicator.gameObject);

		float x = (RailWidth - RailIconSize) * 0.5f;
		railUp = AddRailArrow("up", -1, x, 6f);
		railDown = AddRailArrow("down", 1, x, MainHeight - 6f - RailArrowHeight);

		for (int i = 0; i < menuPages.Count; i++)
		{
			int index = i;
			UiButton button = ButtonFactory.CreateIcon(rail, menuPages[i].Title, menuPages[i].IconId, RailIconSize, RailButtonRadius,
				() => context.Nav.GoToPage(index), () => context.Nav.Page == index, context.RequestRefresh);
			railButtons.Add(button);
		}

		LayoutRail();
	}

	private UiButton AddRailArrow(string iconId, int direction, float x, float y)
	{
		UiButton arrow = ButtonFactory.CreateIcon(rail, "Rail" + iconId, iconId, RailIconSize, RailButtonRadius,
			() => ScrollRail(direction), null, context.RequestRefresh, ButtonStyle.Solid);
		UiKit.PlaceTopLeft(arrow.Rect, x, y, RailIconSize, RailArrowHeight);
		return arrow;
	}

	private void BuildGlance()
	{
		glance = UiKit.NewRect("Glance", frame);
		UiKit.PlaceTopLeft(glance, TotalWidth + GlanceGap, 0f, GlanceWidth, MainHeight);
		glanceBackground = glance.gameObject.AddComponent<Image>();
		glanceBackground.sprite = SpriteFactory.Rounded(PanelRadius);
		glanceBackground.type = Image.Type.Sliced;
		glanceGroup = glance.gameObject.AddComponent<CanvasGroup>();
		glanceGroup.interactable = false;
		glanceGroup.blocksRaycasts = false;

		RectTransform inner = UiKit.NewRect("GlanceContent", glance);
		UiKit.PlaceTopLeft(inner, 10f, 14f, GlanceWidth - 20f, MainHeight - 28f);
		glanceView = BuildView(new GlancePage(context), inner, GlanceWidth - 20f, MainHeight - 28f);
		glanceView.Root.gameObject.SetActive(true);
		if (glanceView.Sections.Count > 0)
		{
			glanceView.Sections[0].Root.gameObject.SetActive(true);
		}

		glance.gameObject.SetActive(false);
	}

	private void BuildRipples()
	{
		for (int i = 0; i < ripples.Length; i++)
		{
			Image image = UiKit.NewImage("Ripple", frame, SpriteFactory.Ring(), false);
			image.type = Image.Type.Simple;
			RectTransform rect = image.rectTransform;
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(RippleSize, RippleSize);
			Isolate(image.gameObject);
			image.gameObject.SetActive(false);
			ripples[i] = new Ripple { Image = image, Age = -1f };
		}
	}

	private int VisibleRailSlots => Mathf.Max(1, Mathf.FloorToInt((MainHeight - 2f * (RailTop + 2f)) / RailPitch));

	private bool RailScrolls => railButtons.Count > VisibleRailSlots;

	private void LayoutRail()
	{
		int slots = VisibleRailSlots;
		railOffset = Mathf.Clamp(railOffset, 0, Mathf.Max(0, railButtons.Count - slots));
		float x = (RailWidth - RailIconSize) * 0.5f;
		float top = RailScrolls ? RailTop : RailTop - 18f;
		for (int i = 0; i < railButtons.Count; i++)
		{
			bool shown = i >= railOffset && i < railOffset + slots;
			railButtons[i].Rect.gameObject.SetActive(shown);
			UiKit.PlaceTopLeft(railButtons[i].Rect, x, top + (i - railOffset) * RailPitch, RailIconSize, RailIconSize);
		}

		railUp.Rect.gameObject.SetActive(RailScrolls);
		railDown.Rect.gameObject.SetActive(RailScrolls);
	}

	private void EnsureCurrentPageOnRail()
	{
		int slots = VisibleRailSlots;
		int page = context.Nav.Page;
		if (page < railOffset)
		{
			railOffset = page;
		}
		else if (page >= railOffset + slots)
		{
			railOffset = page - slots + 1;
		}

		LayoutRail();
	}

	private void StepSection(int direction)
	{
		PageView view = CurrentPage();
		int count = view.Sections.Count;
		context.Nav.GoToSection((context.Nav.Section + direction + count) % count);
	}

	private string PagerText()
	{
		PageView view = CurrentPage();
		int section = SectionIndex(view);
		string sectionName = view.Sections[section].Name ?? view.Page.Title;
		return sectionName + "  " + (section + 1) + "/" + view.Sections.Count;
	}

	private void Subscribe()
	{
		context.Nav.Changed += ApplyNavigation;
		context.RefreshRequested += MarkDirty;
		UiButton.Pressed += OnButtonPressed;
	}

	private int CurrentPageIndex()
	{
		return pages.Count == 0 ? -1 : Mathf.Clamp(context.Nav.Page, 0, pages.Count - 1);
	}

	private PageView CurrentPage()
	{
		return pages[Mathf.Clamp(context.Nav.Page, 0, pages.Count - 1)];
	}

	private int SectionIndex(PageView view)
	{
		return Mathf.Clamp(context.Nav.Section, 0, view.Sections.Count - 1);
	}

	private SectionView CurrentSection()
	{
		if (pages.Count == 0)
		{
			return null;
		}

		PageView view = CurrentPage();
		return view.Sections[SectionIndex(view)];
	}

	private void ApplyNavigation()
	{
		int pageIndex = CurrentPageIndex();
		PageView current = CurrentPage();
		int sectionIndex = SectionIndex(current);
		foreach (PageView view in pages)
		{
			bool isCurrent = ReferenceEquals(view, current);
			view.Root.gameObject.SetActive(isCurrent);
			for (int i = 0; i < view.Sections.Count; i++)
			{
				view.Sections[i].Root.gameObject.SetActive(isCurrent && i == sectionIndex);
			}
		}

		for (int i = 0; i < tabRoots.Count; i++)
		{
			tabRoots[i].gameObject.SetActive(i == pageIndex);
		}

		if (shownPage >= 0 && gameObject.activeInHierarchy && !opening)
		{
			if (pageIndex != shownPage)
			{
				StartSlide(new Vector2(pageIndex > shownPage ? SlideDistance : -SlideDistance, 0f));
			}
			else if (sectionIndex != shownSection)
			{
				StartSlide(new Vector2(sectionIndex > shownSection ? SlideDistance : -SlideDistance, 0f));
			}
		}

		shownPage = pageIndex;
		shownSection = sectionIndex;
		pagerRoot.gameObject.SetActive(current.Sections.Count > 1);
		EnsureCurrentPageOnRail();
		dirty = true;
	}

	private void StartSlide(Vector2 from)
	{
		if (!context.Theme.MotionEnabled)
		{
			slideClock = -1f;
			contentRoot.anchoredPosition = contentBase;
			contentGroup.alpha = 1f;
			return;
		}

		slideDirection = from;
		slideClock = 0f;
		contentRoot.anchoredPosition = contentBase + from;
		contentGroup.alpha = 0f;
	}

	private void Update()
	{
		if (context == null)
		{
			return;
		}

		float deltaTime = Time.unscaledDeltaTime;
		AdvanceOpenClose(deltaTime);
		if (!gameObject.activeSelf)
		{
			return;
		}

		AdvanceFacing();
		AdvanceHome(deltaTime);
		AdvanceSlide(deltaTime);
		AdvanceCurve(deltaTime);
		AdvanceUnderline(deltaTime);
		AdvanceIndicator(deltaTime);
		AdvanceRipples(deltaTime);
		AdvanceGradient(deltaTime);
		TickButtons(deltaTime);
		if (dirty || Time.unscaledTime >= nextRefreshTime)
		{
			Refresh();
		}
	}

	private void OnDestroy()
	{
		UiButton.Pressed -= OnButtonPressed;
		mainBody?.Destroy();
		railBody?.Destroy();
		glanceBody?.Destroy();
		handleBody?.Destroy();
		phoneBody?.Destroy();
		if (context == null)
		{
			return;
		}

		context.Nav.Changed -= ApplyNavigation;
		context.RefreshRequested -= MarkDirty;
	}

	private void AdvanceOpenClose(float deltaTime)
	{
		if (opening)
		{
			animationClock += deltaTime;
			float t = animationClock / OpenDuration;
			reveal = Mathf.Max(0.02f, UiMotion.EaseOutBack(t));
			animationAlpha = Mathf.Clamp01(t * 3f);
			if (t >= 1f)
			{
				opening = false;
				reveal = 1f;
				animationAlpha = 1f;
			}

			ApplyScale();
		}
		else if (closing)
		{
			animationClock += deltaTime;
			float t = animationClock / CloseDuration;
			reveal = Mathf.Max(0.02f, 1f - 0.85f * UiMotion.EaseInCubic(t));
			animationAlpha = 1f - Mathf.Clamp01(t);
			ApplyScale();
			if (t >= 1f)
			{
				FinishClose();
			}
		}
	}

	private void FinishClose()
	{
		closing = false;
		opening = false;
		reveal = 1f;
		animationAlpha = 1f;
		gameObject.SetActive(false);
	}

	private void AdvanceSlide(float deltaTime)
	{
		if (slideClock < 0f)
		{
			return;
		}

		slideClock += deltaTime;
		float eased = UiMotion.EaseOutCubic(slideClock / SlideDuration);
		contentRoot.anchoredPosition = contentBase + slideDirection * (1f - eased);
		contentGroup.alpha = Mathf.Clamp01(eased * 1.3f);
		if (slideClock >= SlideDuration)
		{
			slideClock = -1f;
			contentRoot.anchoredPosition = contentBase;
			contentGroup.alpha = 1f;
		}
	}

	private void AdvanceCurve(float deltaTime)
	{
		if (glance == null || Mathf.Approximately(curve, curveTarget) && glance.gameObject.activeSelf == curveTarget > 0f)
		{
			return;
		}

		curve = context.Theme.MotionEnabled ? Mathf.MoveTowards(curve, curveTarget, deltaTime * 3.2f) : curveTarget;
		ApplyCurve();
	}

	private void ApplyCurve()
	{
		if (glance == null)
		{
			return;
		}

		float eased = UiMotion.EaseOutCubic(curve);
		rail.localRotation = Quaternion.Euler(0f, -RailCurveAngle * eased, 0f);
		glance.localRotation = Quaternion.Euler(0f, GlanceCurveAngle * eased, 0f);
		glanceGroup.alpha = eased;
		bool shown = curve > 0.001f;
		if (glance.gameObject.activeSelf != shown)
		{
			glance.gameObject.SetActive(shown);
			dirty = true;
		}
	}

	private void AdvanceUnderline(float deltaTime)
	{
		int page = CurrentPageIndex();
		if (page < 0 || page >= pageTabs.Count || pageTabs[page].Count == 0)
		{
			return;
		}

		List<UiButton> tabs = pageTabs[page];
		UiButton active = tabs[Mathf.Clamp(context.Nav.Section, 0, tabs.Count - 1)];
		float tabX = active.Rect.anchoredPosition.x;
		float tabWidth = active.Rect.sizeDelta.x;
		float targetWidth = Mathf.Max(14f, tabWidth - 18f);
		float targetX = tabX + (tabWidth - targetWidth) * 0.5f;
		if (!underlineReady || !context.Theme.MotionEnabled)
		{
			underlineX = targetX;
			underlineWidth = targetWidth;
			underlineReady = true;
		}
		else
		{
			underlineX = UiMotion.Damp(underlineX, targetX, 16f, deltaTime);
			underlineWidth = UiMotion.Damp(underlineWidth, targetWidth, 16f, deltaTime);
		}

		Vector2 underlinePosition = new Vector2(ContentX + underlineX, -UnderlineY);
		if ((underline.rectTransform.anchoredPosition - underlinePosition).sqrMagnitude > 0.0004f || Mathf.Abs(underline.rectTransform.sizeDelta.x - underlineWidth) > 0.02f)
		{
			UiKit.PlaceTopLeft(underline.rectTransform, ContentX + underlineX, UnderlineY, underlineWidth, UnderlineHeight);
		}
	}

	private void AdvanceIndicator(float deltaTime)
	{
		int page = CurrentPageIndex();
		if (page < 0 || page >= railButtons.Count)
		{
			return;
		}

		UiButton active = railButtons[page];
		bool visible = active.Rect.gameObject.activeSelf;
		if (railIndicator.gameObject.activeSelf != visible)
		{
			railIndicator.gameObject.SetActive(visible);
		}

		if (!visible)
		{
			indicatorReady = false;
			return;
		}

		float targetY = -active.Rect.anchoredPosition.y;
		if (!indicatorReady || !context.Theme.MotionEnabled)
		{
			indicatorY = targetY;
			indicatorReady = true;
		}
		else
		{
			indicatorY = UiMotion.Damp(indicatorY, targetY, 16f, deltaTime);
		}

		Vector2 indicatorPosition = new Vector2(active.Rect.anchoredPosition.x, -indicatorY);
		if ((railIndicator.rectTransform.anchoredPosition - indicatorPosition).sqrMagnitude > 0.0004f)
		{
			UiKit.PlaceTopLeft(railIndicator.rectTransform, active.Rect.anchoredPosition.x, indicatorY, RailIconSize, RailIconSize);
		}
	}

	private void OnButtonPressed(UiButton button, Vector3 worldPoint)
	{
		if (!gameObject.activeInHierarchy || button.Rect == null || !button.Rect.IsChildOf(transform) || !context.Theme.MotionEnabled)
		{
			return;
		}

		Transform parent = button.Rect.parent;
		if (parent == null)
		{
			return;
		}

		int slot = nextRipple;
		Ripple ripple = ripples[slot];
		nextRipple = (nextRipple + 1) % ripples.Length;
		RectTransform rect = ripple.Image.rectTransform;
		rect.SetParent(parent, false);
		Vector3 local = parent.InverseTransformPoint(worldPoint);
		rect.localPosition = new Vector3(local.x, local.y, 0f);
		rect.localRotation = Quaternion.identity;
		rect.localScale = Vector3.one * 0.2f;
		rect.SetAsLastSibling();
		ripple.Image.gameObject.SetActive(true);
		ripple.Age = 0f;
		ripples[slot] = ripple;
	}

	private void AdvanceRipples(float deltaTime)
	{
		Color tint = appliedTheme != null ? appliedTheme.Accent : Color.white;
		for (int i = 0; i < ripples.Length; i++)
		{
			Ripple ripple = ripples[i];
			if (ripple.Age < 0f)
			{
				continue;
			}

			ripple.Age += deltaTime;
			float t = ripple.Age / RippleDuration;
			if (t >= 1f)
			{
				ripple.Age = -1f;
				ripple.Image.gameObject.SetActive(false);
			}
			else
			{
				float scale = Mathf.Lerp(0.2f, 1.25f, UiMotion.EaseOutCubic(t));
				ripple.Image.rectTransform.localScale = new Vector3(scale, scale, 1f);
				ripple.Image.color = new Color(tint.r, tint.g, tint.b, 0.75f * (1f - t));
			}

			ripples[i] = ripple;
		}
	}

	private void AdvanceGradient(float deltaTime)
	{
		if (appliedTheme == null || !appliedTheme.IsAnimated || !context.Theme.AnimationEnabled)
		{
			return;
		}

		gradientClock += deltaTime;
		if (gradientClock < AnimationInterval)
		{
			return;
		}

		gradientPhase = Mathf.Repeat(gradientPhase + gradientClock * appliedTheme.GradientSpeed, 1f);
		gradientClock = 0f;
		gradient.SetPhase(gradientPhase);
	}

	private void TickButtons(float deltaTime)
	{
		foreach (UiButton button in railButtons)
		{
			button.Tick(deltaTime);
		}

		railUp.Tick(deltaTime);
		railDown.Tick(deltaTime);
		if (phone)
		{
			homePill.Tick(deltaTime);
			foreach (UiButton app in appButtons)
			{
				app.Tick(deltaTime);
			}
		}

		pagerPrevious.Tick(deltaTime);
		pagerNext.Tick(deltaTime);
		int page = CurrentPageIndex();
		if (page >= 0 && page < pageTabs.Count)
		{
			foreach (UiButton tab in pageTabs[page])
			{
				tab.Tick(deltaTime);
			}
		}

		SectionView section = CurrentSection();
		if (section != null)
		{
			foreach (UiButton button in section.Buttons)
			{
				button.Tick(deltaTime);
			}
		}

		if (GlanceVisible && glanceView.Sections.Count > 0)
		{
			foreach (UiButton button in glanceView.Sections[0].Buttons)
			{
				button.Tick(deltaTime);
			}
		}
	}

	private void Refresh()
	{
		dirty = false;
		nextRefreshTime = Time.unscaledTime + PerformanceGuard.Interval(RefreshInterval);

		MenuTheme theme = context.Theme.Current;
		ApplyTheme(theme);
		if (!opening && !closing)
		{
			ApplyScale();
		}

		PageView page = CurrentPage();
		cornerText.text = DateTime.Now.ToString("h:mm tt") + "   " + (context.Nav.Page + 1) + "/" + pages.Count;
		emblem.Busy = context.Busy;

		foreach (UiButton button in railButtons)
		{
			button.Refresh(theme);
		}

		railUp.Refresh(theme);
		railDown.Refresh(theme);
		int pageIndex = CurrentPageIndex();
		if (pageIndex >= 0 && pageIndex < pageTabs.Count)
		{
			foreach (UiButton tab in pageTabs[pageIndex])
			{
				tab.Refresh(theme);
			}
		}

		if (page.Sections.Count > 1)
		{
			pagerPrevious.Refresh(theme);
			pagerNext.Refresh(theme);
			pagerLabel.Refresh(theme);
		}

		if (phone)
		{
			RefreshPhone(theme);
		}

		RefreshSection(page, CurrentSection(), theme);
		if (GlanceVisible && glanceView.Sections.Count > 0)
		{
			RefreshSection(glanceView, glanceView.Sections[0], theme);
		}
	}

	private void RefreshPhone(MenuTheme theme)
	{
		string time = DateTime.Now.ToString("h:mm");
		phoneClock.text = time;
		phoneClock.color = Color.white;
		phoneStatus.text = Mathf.RoundToInt(context.Fps) + " fps   " + (context.Lobby.InRoom ? context.Lobby.PlayerCountText : "offline");
		phoneStatus.color = new Color(0.8f, 0.82f, 0.9f, 1f);
		homePill.Refresh(theme);
		if (!homeScreen.gameObject.activeSelf)
		{
			return;
		}

		homeBackground.color = theme.IsAnimated ? theme.Gradient[1] : theme.Panel;
		homeScenery.sprite = scenery.sprite;
		homeScenery.gameObject.SetActive(scenery.sprite != null && scenery.gameObject.activeSelf);
		homeScenery.color = new Color(1f, 1f, 1f, theme.SceneryAlpha);
		homeClock.text = time;
		homeClock.color = theme.Text;
		homeDate.text = DateTime.Now.ToString("dddd, MMMM d");
		homeDate.color = theme.Muted;
		foreach (UiButton app in appButtons)
		{
			app.Refresh(theme);
		}

		foreach (TextMeshProUGUI label in appLabels)
		{
			label.color = theme.Text;
		}
	}

	private void RefreshSection(PageView page, SectionView section, MenuTheme theme)
	{
		if (section == null)
		{
			return;
		}

		foreach (UiLabel label in section.Labels)
		{
			try
			{
				label.Refresh(theme);
			}
			catch (Exception exception)
			{
				ReportRefreshFailure(page, exception);
			}
		}

		foreach (UiButton button in section.Buttons)
		{
			try
			{
				button.Refresh(theme);
			}
			catch (Exception exception)
			{
				ReportRefreshFailure(page, exception);
			}
		}

		foreach (UiBar bar in section.Bars)
		{
			try
			{
				bar.Refresh(theme);
			}
			catch (Exception exception)
			{
				ReportRefreshFailure(page, exception);
			}
		}
	}

	private void ReportRefreshFailure(PageView page, Exception exception)
	{
		string key = page.Page.Title + ":" + exception.GetType().Name + ":" + exception.Message;
		if (reportedFailures.Add(key))
		{
			Debug.LogError("[AceUtilsPad] Page '" + page.Page.Title + "' failed to refresh: " + exception);
		}
	}

	private void ApplyScale()
	{
		float factor = context.Settings.SizeScale;
		if (mode == SurfaceMode.World)
		{
			transform.localScale = Vector3.one * (WorldScale * factor * PlacementScale * reveal);
		}
		else
		{
			frame.localScale = Vector3.one * (ScreenScale * factor * reveal);
		}
	}

	private void ApplyTheme(MenuTheme theme)
	{
		float opacity = context.Theme.Opacity;
		bool useGradient = theme.IsAnimated;

		Color panel = useGradient ? Color.white : theme.Panel;
		mainBackground.color = new Color(panel.r, panel.g, panel.b, opacity);
		railBackground.color = new Color(theme.Rail.r, theme.Rail.g, theme.Rail.b, opacity);
		if (glanceBackground != null)
		{
			glanceBackground.color = new Color(theme.Panel.r, theme.Panel.g, theme.Panel.b, opacity);
		}

		railIndicator.color = theme.Accent;
		separator.color = new Color(theme.Separator.r, theme.Separator.g, theme.Separator.b, theme.Separator.a * 0.5f);
		underline.color = theme.Accent;
		titleText.color = theme.Text;
		cornerText.color = theme.Muted;
		scenery.color = new Color(1f, 1f, 1f, theme.SceneryAlpha * Mathf.Lerp(0.75f, 1f, opacity));

		bool particlesEnabled = context.Theme.ParticlesEnabled;
		bool sceneryEnabled = context.Theme.SceneryEnabled;
		if (!ReferenceEquals(theme, appliedTheme) || particlesEnabled != appliedParticles || sceneryEnabled != appliedScenery)
		{
			appliedTheme = theme;
			appliedParticles = particlesEnabled;
			appliedScenery = sceneryEnabled;
			gradient.enabled = useGradient;
			gradient.SetPalette(theme.Gradient);
			gradient.SetPhase(gradientPhase);
			particles.Configure(theme.Particles, particlesEnabled, theme.ParticleColor);
			mainBackground.SetVerticesDirty();
		}

		Sprite sprite = sceneryEnabled ? SpriteFactory.Scenery(theme.Scenery) : null;
		if (!ReferenceEquals(sprite, appliedScenerySprite) || scenery.gameObject.activeSelf != (sprite != null))
		{
			appliedScenerySprite = sprite;
			scenery.sprite = sprite;
			scenery.gameObject.SetActive(sprite != null);
		}

		if (mainBody != null)
		{
			Color shell = Color.Lerp(theme.Rail, Color.black, theme.IsLight ? 0.05f : 0.25f);
			mainBody.Tint(shell, theme.Accent);
			railBody.Tint(shell, theme.Accent);
			handleBody.Tint(shell, theme.Accent);
			glanceBody?.Tint(shell, theme.Accent);
			phoneBody.Tint(theme.IsLight ? new Color(0.85f, 0.86f, 0.9f, 1f) : new Color(0.09f, 0.09f, 0.11f, 1f), theme.Accent);
			backText.color = theme.IsLight ? theme.Text : Color.white;
		}
	}

	private static void AddPressable(List<UiButton> targets, IEnumerable<UiButton> candidates)
	{
		foreach (UiButton candidate in candidates)
		{
			if (candidate != null && candidate.CanPress)
			{
				targets.Add(candidate);
			}
		}
	}

	private static void AddPressable(List<UiButton> targets, params UiButton[] candidates)
	{
		AddPressable(targets, (IEnumerable<UiButton>)candidates);
	}

	private struct Ripple
	{
		public Image Image;
		public float Age;
	}
}

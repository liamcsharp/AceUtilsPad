using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

public sealed class AlertMessage
{
	public string Text;
	public string ActionText;
	public Action Action;
	public float Seconds = 6f;
	public string Prefix;
	public string IconId;
	public Color IconColor = Color.white;
	public int Sound = -1;
}

public sealed class AlertHub
{
	private const int HistoryLimit = 6;

	private readonly List<AlertBanner> banners = new List<AlertBanner>();
	private readonly List<string> history = new List<string>();

	public MenuSettings Settings;

	public IReadOnlyList<string> History => history;

	public int HistoryVersion { get; private set; }

	public void Register(AlertBanner banner)
	{
		banners.Add(banner);
	}

	public void Post(AlertMessage message)
	{
		string plain = System.Text.RegularExpressions.Regex.Replace((message.Prefix ?? string.Empty) + " " + (message.Text ?? string.Empty), "<[^>]*>", string.Empty)
			.Replace('\n', ' ').Trim();
		history.Insert(0, DateTime.Now.ToString("h:mm") + "  " + plain);
		if (history.Count > HistoryLimit)
		{
			history.RemoveAt(history.Count - 1);
		}

		HistoryVersion++;
		if (Settings != null)
		{
			NotifySound.Play(message.Sound >= 0 ? message.Sound : Settings.AlertSound.Value, Settings.SoundVolume.Value);
		}

		foreach (AlertBanner banner in banners)
		{
			banner.Show(message);
		}
	}

	public AlertBanner Find(SurfaceMode mode)
	{
		return banners.Find(banner => banner.Mode == mode);
	}

	public void DestroyAll()
	{
		foreach (AlertBanner banner in banners)
		{
			if (banner != null)
			{
				UnityEngine.Object.Destroy(banner.gameObject);
			}
		}

		banners.Clear();
	}
}

public sealed class AlertBanner : MonoBehaviour
{
	public const float Width = MenuSurface.MainWidth;
	public const float Height = 64f;

	private const float RefreshInterval = 0.2f;
	private const float Padding = 12f;
	private const float ButtonHeight = 30f;
	private const float ActionWidth = 84f;
	private const float DismissWidth = 30f;
	private const float IconSize = 20f;
	private const int Radius = 12;
	private const int ScreenSortingOrder = 32001;

	private SurfaceMode mode;
	private MenuContext context;
	private AlertMessage message;
	private RectTransform frame;
	private Image background;
	private TextMeshProUGUI text;
	private TextMeshProUGUI prefix;
	private Image icon;
	private UiButton action;
	private UiButton dismiss;
	private float hideTime;
	private float nextRefreshTime;

	public SurfaceMode Mode => mode;

	public float PlacementScale { get; set; } = 1f;

	public bool IsShowing => gameObject.activeSelf;

	public static AlertBanner Create(SurfaceMode bannerMode, MenuContext menuContext)
	{
		GameObject root = new GameObject(bannerMode == SurfaceMode.World ? "AUPAlertWorld" : "AUPAlertScreen");
		Canvas canvas = root.AddComponent<Canvas>();
		AlertBanner banner = root.AddComponent<AlertBanner>();
		banner.mode = bannerMode;
		banner.context = menuContext;
		banner.ConfigureCanvas(canvas);
		banner.Build();
		DontDestroyOnLoad(root);
		root.SetActive(false);
		return banner;
	}

	public void Show(AlertMessage alert)
	{
		message = alert;
		text.text = alert.Text;
		hideTime = Time.unscaledTime + alert.Seconds;
		nextRefreshTime = 0f;
		gameObject.SetActive(true);
		LayoutLead(alert);
	}

	private void LayoutLead(AlertMessage alert)
	{
		float textWidth = Width - 2f * Padding - ActionWidth - DismissWidth - 12f;
		float x = Padding;
		bool hasPrefix = !string.IsNullOrEmpty(alert.Prefix);
		bool hasIcon = !string.IsNullOrEmpty(alert.IconId);
		prefix.gameObject.SetActive(hasPrefix);
		icon.gameObject.SetActive(hasIcon);
		if (hasPrefix)
		{
			prefix.text = alert.Prefix;
			float width = 60f;
			try
			{
				width = Mathf.Clamp(prefix.GetPreferredValues(alert.Prefix, 400f, Height).x + 2f, 10f, 140f);
			}
			catch (Exception)
			{
			}

			UiKit.PlaceTopLeft(prefix.rectTransform, x, 4f, width, Height - 8f);
			x += width + 4f;
		}

		if (hasIcon)
		{
			icon.sprite = SpriteFactory.Icon(alert.IconId);
			icon.color = alert.IconColor;
			UiKit.PlaceTopLeft(icon.rectTransform, x, (Height - IconSize) * 0.5f, IconSize, IconSize);
			x += IconSize + 5f;
		}

		UiKit.PlaceTopLeft(text.rectTransform, x, 4f, Mathf.Max(40f, textWidth - (x - Padding)), Height - 8f);
	}

	public void Hide()
	{
		message = null;
		gameObject.SetActive(false);
	}

	public void CollectTouchTargets(List<UiButton> targets)
	{
		if (!IsShowing)
		{
			return;
		}

		if (action.CanPress)
		{
			targets.Add(action);
		}

		if (dismiss.CanPress)
		{
			targets.Add(dismiss);
		}
	}

	private void ConfigureCanvas(Canvas canvas)
	{
		RectTransform rect = (RectTransform)transform;
		if (mode == SurfaceMode.World)
		{
			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = Camera.main;
			gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1000f;
			rect.sizeDelta = new Vector2(Width, Height);
			rect.localScale = Vector3.one * MenuSurface.WorldScale;
			return;
		}

		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.overrideSorting = true;
		canvas.sortingOrder = ScreenSortingOrder;
		gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
	}

	private void Build()
	{
		frame = UiKit.NewRect("Frame", transform);
		if (mode == SurfaceMode.World)
		{
			UiKit.Stretch(frame);
		}
		else
		{
			frame.anchorMin = new Vector2(0.5f, 1f);
			frame.anchorMax = new Vector2(0.5f, 1f);
			frame.pivot = new Vector2(0.5f, 1f);
			frame.anchoredPosition = new Vector2(0f, -32f);
			frame.sizeDelta = new Vector2(Width, Height);
			frame.localScale = Vector3.one * MenuSurface.ScreenScale;
		}

		background = frame.gameObject.AddComponent<Image>();
		background.sprite = SpriteFactory.Rounded(Radius);
		background.type = Image.Type.Sliced;

		text = UiKit.NewText("Message", frame, 9.5f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
		UiKit.PlaceTopLeft(text.rectTransform, Padding, 4f, Width - 2f * Padding - ActionWidth - DismissWidth - 12f, Height - 8f);

		prefix = UiKit.NewText("Prefix", frame, 9.5f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
		prefix.textWrappingMode = TextWrappingModes.NoWrap;
		prefix.overflowMode = TextOverflowModes.Overflow;
		icon = UiKit.NewImage("Icon", frame, null, false);
		icon.type = Image.Type.Simple;
		icon.preserveAspect = true;
		prefix.gameObject.SetActive(false);
		icon.gameObject.SetActive(false);

		float buttonY = (Height - ButtonHeight) * 0.5f;
		ButtonSpec actionSpec = new ButtonSpec(() => message == null ? string.Empty : message.ActionText, RunAction, null,
			() => message != null && !string.IsNullOrEmpty(message.ActionText));
		action = ButtonFactory.Create(frame, "Action", actionSpec, ActionWidth, ButtonHeight, 8, 9f, null);
		UiKit.PlaceTopLeft(action.Rect, Width - Padding - DismissWidth - 6f - ActionWidth, buttonY, ActionWidth, ButtonHeight);

		ButtonSpec dismissSpec = new ButtonSpec("x", Hide);
		dismiss = ButtonFactory.Create(frame, "Dismiss", dismissSpec, DismissWidth, ButtonHeight, 8, 10f, null);
		UiKit.PlaceTopLeft(dismiss.Rect, Width - Padding - DismissWidth, buttonY, DismissWidth, ButtonHeight);
	}

	private void ApplyScale()
	{
		float factor = context.Settings.SizeScale;
		if (mode == SurfaceMode.World)
		{
			transform.localScale = Vector3.one * (MenuSurface.WorldScale * factor * PlacementScale);
		}
		else
		{
			frame.localScale = Vector3.one * (MenuSurface.ScreenScale * factor);
		}
	}

	private void RunAction()
	{
		AlertMessage current = message;
		Hide();
		if (current != null && current.Action != null)
		{
			current.Action();
		}
	}

	private void Update()
	{
		if (Time.unscaledTime >= hideTime)
		{
			Hide();
			return;
		}

		float deltaTime = Time.unscaledDeltaTime;
		action.Tick(deltaTime);
		dismiss.Tick(deltaTime);
		if (Time.unscaledTime < nextRefreshTime)
		{
			return;
		}

		nextRefreshTime = Time.unscaledTime + RefreshInterval;
		ApplyScale();
		MenuTheme theme = context.Theme.Current;
		Color panel = theme.IsAnimated ? theme.Gradient[0] : theme.Panel;
		background.color = new Color(panel.r, panel.g, panel.b, Mathf.Max(0.92f, context.Theme.Opacity));
		text.color = theme.Text;
		prefix.color = theme.Text;
		action.Refresh(theme);
		dismiss.Refresh(theme);
	}
}

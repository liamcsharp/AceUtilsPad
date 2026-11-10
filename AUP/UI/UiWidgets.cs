using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

public enum ButtonStyle
{
	Solid,
	Tab,
	Rail
}

public sealed class UiLabel
{
	private readonly TextMeshProUGUI text;
	private readonly Func<string> source;
	private readonly ColorPick color;

	internal UiLabel(TextMeshProUGUI text, Func<string> source, ColorPick color)
	{
		this.text = text;
		this.source = source;
		this.color = color;
	}

	public string CurrentText => text.text;

	public bool IsVisible => text.gameObject.activeInHierarchy;

	internal void Refresh(MenuTheme theme)
	{
		string value = source();
		if (!string.Equals(text.text, value, StringComparison.Ordinal))
		{
			text.text = value;
		}

		text.color = color(theme);
	}
}

public sealed class UiBar
{
	private readonly Image track;
	private readonly Image fill;
	private readonly Func<float> source;
	private readonly Func<bool> visible;
	private readonly bool paintTrack;

	internal UiBar(Image track, Image fill, Func<float> source, Func<bool> visible, bool paintTrack = true)
	{
		this.track = track;
		this.fill = fill;
		this.source = source;
		this.visible = visible;
		this.paintTrack = paintTrack;
	}

	internal void Refresh(MenuTheme theme)
	{
		bool shown = visible == null || visible();
		if (track.gameObject.activeSelf != shown)
		{
			track.gameObject.SetActive(shown);
		}

		if (!shown)
		{
			return;
		}

		float value = Mathf.Clamp01(source());
		if (Mathf.Abs(fill.rectTransform.anchorMax.x - value) > 0.002f)
		{
			fill.rectTransform.anchorMax = new Vector2(value, 1f);
		}

		if (paintTrack)
		{
			track.color = theme.Element;
		}

		fill.color = theme.Accent;
	}
}

public sealed class UiButton
{
	private const float PulseDecayPerSecond = 4f;
	private const float PulseStrength = 0.35f;
	private const float HoverStrength = 0.22f;
	private const float SquishDepth = 0.05f;
	private const float SquishDuration = 0.55f;

	internal static event Action<UiButton, Vector3> Pressed;

	private readonly Image background;
	private readonly TextMeshProUGUI label;
	private readonly Image icon;
	private readonly Button button;
	private readonly Func<string> textSource;
	private readonly Func<bool> activeSource;
	private readonly Func<bool> visibleSource;
	private readonly ButtonStyle style;
	private Color baseFill;
	private Color hoverTint = Color.white;
	private float pulse;
	private float squishTime = -1f;
	private bool hovered;
	private bool hasPressPoint;
	private Vector3 pressPoint;

	internal UiButton(RectTransform rect, Image background, TextMeshProUGUI label, Image icon, Button button,
		Func<string> textSource, Func<bool> activeSource, Func<bool> visibleSource, ButtonStyle style)
	{
		Rect = rect;
		this.background = background;
		this.label = label;
		this.icon = icon;
		this.button = button;
		this.textSource = textSource;
		this.activeSource = activeSource;
		this.visibleSource = visibleSource;
		this.style = style;
	}

	public RectTransform Rect { get; }

	public TextMeshProUGUI Label => label;

	public bool CanPress => Rect.gameObject.activeInHierarchy && button.interactable;

	public bool IsActive => activeSource != null && activeSource();

	public bool Hovered
	{
		get => hovered;
		set
		{
			if (hovered == value)
			{
				return;
			}

			hovered = value;
			ApplyFill();
		}
	}

	public Vector3 Center => Rect.TransformPoint(Rect.rect.center);

	public float PressFraction { get; private set; } = 0.5f;

	public void Invoke()
	{
		button.onClick.Invoke();
	}

	public void PressAt(Vector3 worldPoint)
	{
		pressPoint = worldPoint;
		hasPressPoint = true;
		Rect bounds = Rect.rect;
		PressFraction = bounds.width > 0f ? Mathf.Clamp01((Rect.InverseTransformPoint(worldPoint).x - bounds.xMin) / bounds.width) : 0.5f;
		button.onClick.Invoke();
	}

	public bool ContainsWorldPoint(Vector3 worldPoint, float margin)
	{
		Rect bounds = Rect.rect;
		Vector3 local = Rect.InverseTransformPoint(worldPoint);
		return local.x >= bounds.xMin - margin && local.x <= bounds.xMax + margin
			   && local.y >= bounds.yMin - margin && local.y <= bounds.yMax + margin;
	}

	internal void NotifyPressed(bool animate)
	{
		Vector3 point = hasPressPoint ? pressPoint : Center;
		hasPressPoint = false;
		pulse = 1f;
		squishTime = animate ? 0f : -1f;
		if (!animate)
		{
			Rect.localScale = Vector3.one;
		}

		Pressed?.Invoke(this, point);
	}

	internal void Tick(float deltaTime)
	{
		if (pulse > 0f)
		{
			pulse = Mathf.MoveTowards(pulse, 0f, deltaTime * PulseDecayPerSecond);
			ApplyFill();
		}

		if (squishTime < 0f)
		{
			return;
		}

		squishTime += deltaTime;
		if (squishTime >= SquishDuration)
		{
			squishTime = -1f;
			Rect.localScale = Vector3.one;
			return;
		}

		float scale = 1f - SquishDepth * Mathf.Exp(-squishTime * 9f) * Mathf.Cos(squishTime * 26f);
		Rect.localScale = new Vector3(scale, scale, 1f);
	}

	internal void Refresh(MenuTheme theme)
	{
		if (visibleSource != null)
		{
			bool visible = visibleSource();
			if (Rect.gameObject.activeSelf != visible)
			{
				Rect.gameObject.SetActive(visible);
			}

			if (!visible)
			{
				return;
			}
		}

		if (label != null && textSource != null)
		{
			string value = textSource();
			if (!string.Equals(label.text, value, StringComparison.Ordinal))
			{
				label.text = value;
			}
		}

		bool active = IsActive;
		Color content;
		switch (style)
		{
			case ButtonStyle.Tab:
				baseFill = new Color(theme.Text.r, theme.Text.g, theme.Text.b, 0f);
				content = active ? theme.Accent : theme.Muted;
				break;
			case ButtonStyle.Rail:
				baseFill = active ? new Color(theme.Element.r, theme.Element.g, theme.Element.b, 0f) : theme.Element;
				content = active ? theme.OnAccent : theme.ElementText;
				break;
			default:
				baseFill = active ? theme.Accent : theme.Element;
				content = active ? theme.OnAccent : theme.ElementText;
				break;
		}

		hoverTint = theme.Accent;
		ApplyFill();
		if (label != null)
		{
			label.color = content;
		}

		if (icon != null)
		{
			icon.color = content;
		}
	}

	private void ApplyFill()
	{
		Color fill = baseFill;
		if (hovered && !IsActive)
		{
			fill = style == ButtonStyle.Tab
				? new Color(hoverTint.r, hoverTint.g, hoverTint.b, 0.14f)
				: Color.Lerp(fill, hoverTint, HoverStrength);
		}

		if (pulse > 0f)
		{
			Color flash = Color.Lerp(fill, Color.white, pulse * PulseStrength);
			flash.a = Mathf.Max(fill.a, pulse * 0.35f);
			fill = flash;
		}

		background.color = fill;
	}
}

public struct ButtonSpec
{
	public ButtonSpec(string text, Action click, Func<bool> active = null, Func<bool> visible = null)
		: this(() => text, click, active, visible)
	{
	}

	public ButtonSpec(Func<string> text, Action click, Func<bool> active = null, Func<bool> visible = null)
	{
		Text = text;
		Click = click;
		Active = active;
		Visible = visible;
	}

	public Func<string> Text;
	public Action Click;
	public Func<bool> Active;
	public Func<bool> Visible;
}

using System;
using System.Collections.Generic;
using AUP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP.Widgets;

internal static class WidgetFonts
{
	private const int MaxFonts = 6;

	private static List<UnityEngine.Object> fonts;
	private static System.Reflection.PropertyInfo fontProperty;

	public static int Count
	{
		get
		{
			Ensure();
			return fonts.Count + 1;
		}
	}

	public static string Name(int index)
	{
		Ensure();
		index = WidgetOptions.Wrap(index, fonts.Count + 1);
		if (index == 0)
		{
			return "Default";
		}

		string name = fonts[index - 1] != null ? fonts[index - 1].name : "Font " + index;
		return name.Length > 18 ? name.Substring(0, 18) : name;
	}

	public static void Apply(TextMeshProUGUI text, int index)
	{
		Ensure();
		index = WidgetOptions.Wrap(index, fonts.Count + 1);
		if (index == 0 || fontProperty == null || fonts[index - 1] == null)
		{
			return;
		}

		try
		{
			fontProperty.SetValue(text, fonts[index - 1], null);
		}
		catch (Exception)
		{
		}
	}

	private static void Ensure()
	{
		if (fonts != null)
		{
			return;
		}

		fonts = new List<UnityEngine.Object>();
		try
		{
			fontProperty = typeof(TMP_Text).GetProperty("font");
			if (fontProperty == null || !fontProperty.CanWrite)
			{
				fontProperty = null;
				return;
			}

			foreach (UnityEngine.Object font in Resources.FindObjectsOfTypeAll(fontProperty.PropertyType))
			{
				if (font != null && !string.IsNullOrEmpty(font.name) && !fonts.Exists(existing => existing.name == font.name))
				{
					fonts.Add(font);
					if (fonts.Count >= MaxFonts)
					{
						break;
					}
				}
			}
		}
		catch (Exception)
		{
			fonts.Clear();
		}
	}
}

internal sealed class WidgetView
{
	public const float IconSize = 36f;

	private const float TitleHeight = 16f;
	private const float RowGap = 3f;
	private const float ButtonHeight = 24f;
	private const float BarHeight = 5f;
	private const float SliderHeight = 14f;

	private readonly List<TextBinding> texts = new List<TextBinding>();
	private readonly List<BarBinding> bars = new List<BarBinding>();
	private readonly List<UiButton> buttons = new List<UiButton>();
	private readonly MenuTheme skin = new MenuTheme();

	private Image shadow;
	private Image border;
	private Image background;
	private TextMeshProUGUI title;
	private Image titleIcon;
	private float fullWidth;
	private float fullHeight;

	public WidgetView(Transform parent, WidgetConfig config, List<WidgetRow> rows, Action onMinimize, Action onExpand, Action afterClick)
	{
		Config = config;
		Root = UiKit.NewRect("Widget_" + config.Id, parent);
		Root.anchorMin = new Vector2(0.5f, 0.5f);
		Root.anchorMax = new Vector2(0.5f, 0.5f);
		Root.pivot = new Vector2(0.5f, 0.5f);
		Group = Root.gameObject.AddComponent<CanvasGroup>();
		Group.interactable = false;
		Group.blocksRaycasts = false;
		if (config.Minimized)
		{
			BuildIcon(onExpand, afterClick);
		}
		else
		{
			BuildFull(rows, onMinimize, afterClick);
		}
	}

	public WidgetConfig Config { get; }

	public RectTransform Root { get; }

	public CanvasGroup Group { get; }

	public List<UiButton> Buttons => buttons;

	public Vector2 Size => new Vector2(fullWidth, fullHeight);

	private static float WidthFor(WidgetConfig config)
	{
		switch (config.KindValue)
		{
			case WidgetKind.Feed:
				return 210f;
			case WidgetKind.Music:
			case WidgetKind.Custom:
				return 176f;
			case WidgetKind.Room:
			case WidgetKind.Dock:
			case WidgetKind.Launcher:
			case WidgetKind.Battery:
				return 156f;
			default:
				return 132f;
		}
	}

	private void BuildIcon(Action onExpand, Action afterClick)
	{
		fullWidth = IconSize;
		fullHeight = IconSize;
		Root.sizeDelta = new Vector2(IconSize, IconSize);
		AddChrome(IconSize, IconSize);
		UiButton button = ButtonFactory.CreateIcon(Root, "Expand", WidgetOptions.KindIcons[Config.Kind], IconSize, Config.Square ? 2 : 10, onExpand, null, afterClick,
			ButtonStyle.Solid);
		UiKit.PlaceTopLeft(button.Rect, 0f, 0f, IconSize, IconSize);
		buttons.Add(button);
	}

	private void BuildFull(List<WidgetRow> rows, Action onMinimize, Action afterClick)
	{
		float padding = WidgetOptions.Paddings[Config.Padding];
		float width = WidthFor(Config);
		float inner = width - 2f * padding;
		float height = padding + TitleHeight + RowGap;
		foreach (WidgetRow row in rows)
		{
			height += RowHeight(row) + RowGap;
		}

		height += padding - RowGap;
		fullWidth = width;
		fullHeight = height;
		Root.sizeDelta = new Vector2(width, height);
		AddChrome(width, height);
		int font = Config.Font;

		titleIcon = UiKit.NewImage("Icon", Root, SpriteFactory.Icon(WidgetOptions.KindIcons[Config.Kind]), false);
		titleIcon.type = Image.Type.Simple;
		titleIcon.preserveAspect = true;
		UiKit.PlaceTopLeft(titleIcon.rectTransform, padding, padding + 2f, 11f, 11f);
		title = NewText(8f, FontStyles.Bold, font);
		title.text = Rich.Escape((string.IsNullOrEmpty(Config.Title) ? WidgetOptions.KindNames[Config.Kind] : Config.Title).ToUpperInvariant());
		UiKit.PlaceTopLeft(title.rectTransform, padding + 15f, padding, inner - 33f, TitleHeight);

		UiButton minimize = ButtonFactory.Create(Root, "Minimize", new ButtonSpec("-", onMinimize), 16f, 14f, Config.Square ? 1 : 5, 9f, afterClick);
		UiKit.PlaceTopLeft(minimize.Rect, width - padding - 16f, padding, 16f, 14f);
		buttons.Add(minimize);

		float y = padding + TitleHeight + RowGap;
		foreach (WidgetRow row in rows)
		{
			float rowHeight = RowHeight(row);
			AddRow(row, padding, y, inner, rowHeight, font, afterClick);
			y += rowHeight + RowGap;
		}
	}

	private void AddChrome(float width, float height)
	{
		int radius = Config.Square ? 2 : 12;
		float borderSize = WidgetOptions.BorderSizes[Config.Border];
		shadow = UiKit.NewImage("Shadow", Root, SpriteFactory.Rounded(radius), false);
		UiKit.PlaceTopLeft(shadow.rectTransform, 3f, 4f, width, height);
		shadow.gameObject.SetActive(Config.Shadow && Config.Background != 7);
		border = UiKit.NewImage("Border", Root, SpriteFactory.Rounded(radius), false);
		UiKit.PlaceTopLeft(border.rectTransform, -borderSize, -borderSize, width + 2f * borderSize, height + 2f * borderSize);
		border.gameObject.SetActive(borderSize > 0f);
		background = UiKit.NewImage("Background", Root, SpriteFactory.Rounded(radius), false);
		UiKit.PlaceTopLeft(background.rectTransform, 0f, 0f, width, height);
	}

	private static float RowHeight(WidgetRow row)
	{
		switch (row.Type)
		{
			case RowType.Bar:
				return BarHeight;
			case RowType.Buttons:
				return ButtonHeight;
			case RowType.Slider:
				return SliderHeight;
			case RowType.Separator:
				return 2f;
			case RowType.Panel:
				return 12f;
			case RowType.Icon:
				return 26f;
			default:
				return (row.Size + 4f) * Mathf.Max(1, row.Lines);
		}
	}

	private void AddRow(WidgetRow row, float x, float y, float width, float height, int font, Action afterClick)
	{
		switch (row.Type)
		{
			case RowType.Text:
				TextMeshProUGUI text = NewText(row.Size, row.Bold ? FontStyles.Bold : FontStyles.Normal, font);
				UiKit.PlaceTopLeft(text.rectTransform, x, y, width, height);
				texts.Add(new TextBinding { Text = text, Source = row.Text, Muted = row.Muted });
				break;
			case RowType.Bar:
				Image track = UiKit.NewImage("Track", Root, SpriteFactory.Rounded(8), false);
				UiKit.PlaceTopLeft(track.rectTransform, x, y, width, height);
				Image fill = NewFill(track.rectTransform);
				bars.Add(new BarBinding { Track = track, Fill = fill, Source = row.Value, PaintTrack = true });
				break;
			case RowType.Slider:
				UiButton holder = null;
				WidgetRow captured = row;
				holder = ButtonFactory.Create(Root, "Slider", new ButtonSpec(string.Empty, () =>
				{
					if (holder != null)
					{
						captured.Set(Mathf.Clamp01((holder.PressFraction - 0.04f) / 0.92f));
					}
				}), width, height, 7, 8f, afterClick);
				UiKit.PlaceTopLeft(holder.Rect, x, y, width, height);
				buttons.Add(holder);
				bars.Add(new BarBinding { Track = holder.Rect.GetComponent<Image>(), Fill = NewFill(holder.Rect), Source = row.Value });
				break;
			case RowType.Buttons:
				int count = Mathf.Max(1, row.Buttons.Length);
				float buttonWidth = (width - (count - 1) * 4f) / count;
				for (int i = 0; i < row.Buttons.Length; i++)
				{
					UiButton button = ButtonFactory.Create(Root, "Button", row.Buttons[i], buttonWidth, height, Config.Square ? 2 : 7, 8.5f, afterClick);
					UiKit.PlaceTopLeft(button.Rect, x + i * (buttonWidth + 4f), y, buttonWidth, height);
					buttons.Add(button);
				}

				break;
			case RowType.Separator:
				Image line = UiKit.NewImage("Separator", Root, null, false);
				UiKit.PlaceTopLeft(line.rectTransform, x, y, width, height);
				bars.Add(new BarBinding { Track = line, Decor = 1 });
				break;
			case RowType.Panel:
				Image panel = UiKit.NewImage("Panel", Root, SpriteFactory.Rounded(8), false);
				UiKit.PlaceTopLeft(panel.rectTransform, x, y, width, height);
				bars.Add(new BarBinding { Track = panel, Decor = 2 });
				break;
			case RowType.Icon:
				Image icon = UiKit.NewImage("RowIcon", Root, SpriteFactory.Icon(row.Icon), false);
				icon.type = Image.Type.Simple;
				icon.preserveAspect = true;
				UiKit.PlaceTopLeft(icon.rectTransform, x + (width - height) * 0.5f, y, height, height);
				bars.Add(new BarBinding { Track = icon, Decor = 3 });
				break;
		}
	}

	private static Image NewFill(RectTransform track)
	{
		Image fill = UiKit.NewImage("Fill", track, SpriteFactory.Rounded(8), false);
		fill.rectTransform.anchorMin = Vector2.zero;
		fill.rectTransform.anchorMax = new Vector2(0f, 1f);
		fill.rectTransform.pivot = new Vector2(0f, 0.5f);
		fill.rectTransform.offsetMin = Vector2.zero;
		fill.rectTransform.offsetMax = Vector2.zero;
		return fill;
	}

	private TextMeshProUGUI NewText(float size, FontStyles style, int font)
	{
		TextMeshProUGUI text = UiKit.NewText("Text", Root, size, TextAlignmentOptions.MidlineLeft, style);
		text.textWrappingMode = TextWrappingModes.NoWrap;
		WidgetFonts.Apply(text, font);
		return text;
	}

	public void Refresh(MenuTheme theme, float globalAlpha)
	{
		Color baseColor = Config.Background == 0
			? (theme.IsAnimated ? theme.Gradient[0] : theme.Panel)
			: WidgetOptions.Backgrounds[Config.Background];
		bool clear = Config.Background == 7;
		Color textColor = Config.TextColor == 0
			? (Config.Background == 0 || clear ? theme.Text : Luminance(baseColor) > 0.6f ? new Color(0.08f, 0.08f, 0.12f, 1f) : Color.white)
			: Config.TextColor == 3 ? theme.Accent : WidgetOptions.TextColors[Config.TextColor];
		Color muted = new Color(textColor.r, textColor.g, textColor.b, 0.68f);
		float alpha = Mathf.Clamp01(Config.Alpha * globalAlpha);

		background.color = clear ? new Color(0f, 0f, 0f, 0f) : new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
		border.color = new Color(theme.Accent.r, theme.Accent.g, theme.Accent.b, Mathf.Clamp01(alpha + 0.1f));
		shadow.color = new Color(0f, 0f, 0f, 0.32f * alpha);
		if (title != null)
		{
			title.color = muted;
			titleIcon.color = theme.Accent;
		}

		skin.Panel = baseColor;
		skin.Rail = baseColor;
		skin.Element = clear ? new Color(0f, 0f, 0f, 0.45f) : Color.Lerp(baseColor, Luminance(baseColor) > 0.5f ? Color.black : Color.white, 0.14f);
		skin.Accent = theme.Accent;
		skin.Text = textColor;
		skin.ElementText = textColor;
		skin.Muted = muted;

		foreach (TextBinding binding in texts)
		{
			string value = binding.Source != null ? binding.Source() : string.Empty;
			if (!string.Equals(binding.Text.text, value, StringComparison.Ordinal))
			{
				binding.Text.text = value;
			}

			binding.Text.color = binding.Muted ? muted : textColor;
		}

		foreach (BarBinding bar in bars)
		{
			switch (bar.Decor)
			{
				case 1:
					bar.Track.color = new Color(textColor.r, textColor.g, textColor.b, 0.3f);
					continue;
				case 2:
					bar.Track.color = skin.Element;
					continue;
				case 3:
					bar.Track.color = theme.Accent;
					continue;
			}

			if (bar.PaintTrack)
			{
				bar.Track.color = skin.Element;
			}

			float value = Mathf.Clamp01(bar.Source != null ? bar.Source() : 0f);
			if (Mathf.Abs(bar.Fill.rectTransform.anchorMax.x - value) > 0.004f)
			{
				bar.Fill.rectTransform.anchorMax = new Vector2(value, 1f);
			}

			bar.Fill.color = theme.Accent;
		}

		foreach (UiButton button in buttons)
		{
			button.Refresh(skin);
		}
	}

	public void Tick(float deltaTime)
	{
		foreach (UiButton button in buttons)
		{
			button.Tick(deltaTime);
		}
	}

	private static float Luminance(Color color)
	{
		return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
	}

	private sealed class TextBinding
	{
		public TextMeshProUGUI Text;
		public Func<string> Source;
		public bool Muted;
	}

	private sealed class BarBinding
	{
		public Image Track;
		public Image Fill;
		public Func<float> Source;
		public bool PaintTrack;
		public int Decor;
	}
}

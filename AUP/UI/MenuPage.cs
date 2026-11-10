using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

public abstract class MenuPage
{
	protected MenuPage(MenuContext context)
	{
		Context = context;
	}

	public abstract string Title { get; }
	public abstract string IconId { get; }

	protected MenuContext Context { get; }

	public abstract void Build(PageBuilder ui);
}

internal sealed class SectionView
{
	public string Name;
	public RectTransform Root;
	public readonly List<UiLabel> Labels = new List<UiLabel>();
	public readonly List<UiButton> Buttons = new List<UiButton>();
	public readonly List<UiBar> Bars = new List<UiBar>();
}

internal sealed class PageView
{
	public MenuPage Page;
	public RectTransform Root;
	public readonly List<SectionView> Sections = new List<SectionView>();
}

public sealed class PageBuilder
{
	public const float RowHeight = 30f;
	public const float RowGap = 6f;
	public const float LabelGap = 4f;
	public const float StepperButtonWidth = 40f;
	public const float RowButtonWidth = 84f;

	private const float DefaultFontSize = 9.5f;
	private const float ButtonFontSize = 9f;
	private const int ButtonRadius = 8;

	private readonly MenuContext context;
	private readonly PageView view;
	private readonly float sectionHeight;
	private SectionView section;
	private float cursor;

	internal PageBuilder(MenuContext context, PageView view)
		: this(context, view, MenuSurface.ContentWidth, MenuSurface.ContentHeight)
	{
	}

	internal PageBuilder(MenuContext context, PageView view, float width, float height)
	{
		this.context = context;
		this.view = view;
		Width = width;
		sectionHeight = height;
	}

	public float Width { get; }

	public void Section(string name)
	{
		CloseSection();
		RectTransform root = UiKit.NewRect("Section", view.Root);
		UiKit.Stretch(root);
		section = new SectionView { Name = name, Root = root };
		view.Sections.Add(section);
		cursor = 0f;
	}

	public UiLabel Heading(string text)
	{
		return Label(text, 1, 11f, FontStyles.Bold, Pick.Accent);
	}

	public UiLabel Label(string text, int lines = 1, float size = DefaultFontSize, FontStyles style = FontStyles.Normal,
		ColorPick color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Top)
	{
		return Label(() => text, lines, size, style, color, alignment);
	}

	public UiLabel Label(Func<string> source, int lines = 1, float size = DefaultFontSize, FontStyles style = FontStyles.Normal,
		ColorPick color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Top)
	{
		EnsureSection();
		float height = lines * (size + 5f);
		TextMeshProUGUI text = UiKit.NewText("Label", section.Root, size, alignment, style);
		UiKit.PlaceTopLeft(text.rectTransform, 0f, cursor, Width, height);

		UiLabel label = new UiLabel(text, source, color ?? Pick.Text);
		section.Labels.Add(label);
		cursor += height + LabelGap;
		return label;
	}

	public void Space(float amount = RowGap)
	{
		EnsureSection();
		cursor += amount;
	}

	public void Pair(Func<string> left, Func<string> right, float size = DefaultFontSize, ColorPick color = null)
	{
		EnsureSection();
		float rowHeight = size + 5f;
		TextMeshProUGUI leftText = UiKit.NewText("PairLeft", section.Root, size, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
		UiKit.PlaceTopLeft(leftText.rectTransform, 0f, cursor, Width * 0.5f, rowHeight);
		TextMeshProUGUI rightText = UiKit.NewText("PairRight", section.Root, size, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
		UiKit.PlaceTopLeft(rightText.rectTransform, Width * 0.5f, cursor, Width * 0.5f, rowHeight);
		section.Labels.Add(new UiLabel(leftText, left, color ?? Pick.Muted));
		section.Labels.Add(new UiLabel(rightText, right, color ?? Pick.Muted));
		cursor += rowHeight + LabelGap;
	}

	public RectTransform Box(float boxHeight)
	{
		EnsureSection();
		RectTransform box = UiKit.NewRect("Box", section.Root);
		UiKit.PlaceTopLeft(box, 0f, cursor, Width, boxHeight);
		cursor += boxHeight + LabelGap;
		return box;
	}

	public UiButton Slider(Func<float> value, Action<float> changed, float barHeight = 16f)
	{
		EnsureSection();
		UiButton holder = null;
		ButtonSpec spec = new ButtonSpec(string.Empty, () =>
		{
			if (holder != null)
			{
				changed(Mathf.Clamp01((holder.PressFraction - 0.04f) / 0.92f));
			}
		});
		holder = ButtonFactory.Create(section.Root, "Slider", spec, Width, barHeight, 8, 8f, context.RequestRefresh);
		UiKit.PlaceTopLeft(holder.Rect, 0f, cursor, Width, barHeight);
		Image fill = UiKit.NewImage("SliderFill", holder.Rect, SpriteFactory.Rounded(8), false);
		fill.rectTransform.anchorMin = Vector2.zero;
		fill.rectTransform.anchorMax = new Vector2(0f, 1f);
		fill.rectTransform.pivot = new Vector2(0f, 0.5f);
		fill.rectTransform.offsetMin = Vector2.zero;
		fill.rectTransform.offsetMax = Vector2.zero;
		Image track = holder.Rect.GetComponent<Image>();
		section.Buttons.Add(holder);
		section.Bars.Add(new UiBar(track, fill, value, null, false));
		cursor += barHeight + LabelGap;
		return holder;
	}

	public UiBar Progress(Func<float> value, Func<bool> visible = null, float barHeight = 6f)
	{
		EnsureSection();
		Image track = UiKit.NewImage("ProgressTrack", section.Root, SpriteFactory.Rounded(8), false);
		UiKit.PlaceTopLeft(track.rectTransform, 0f, cursor, Width, barHeight);
		Image fill = UiKit.NewImage("ProgressFill", track.rectTransform, SpriteFactory.Rounded(8), false);
		fill.rectTransform.anchorMin = Vector2.zero;
		fill.rectTransform.anchorMax = new Vector2(0f, 1f);
		fill.rectTransform.pivot = new Vector2(0f, 0.5f);
		fill.rectTransform.offsetMin = Vector2.zero;
		fill.rectTransform.offsetMax = Vector2.zero;
		UiBar bar = new UiBar(track, fill, value, visible);
		section.Bars.Add(bar);
		cursor += barHeight + LabelGap;
		return bar;
	}

	public UiButton Button(string text, Action click, Func<bool> active = null, Func<bool> visible = null)
	{
		return Buttons(new ButtonSpec(text, click, active, visible))[0];
	}

	public UiButton Button(Func<string> text, Action click, Func<bool> active = null, Func<bool> visible = null)
	{
		return Buttons(new ButtonSpec(text, click, active, visible))[0];
	}

	public UiButton Toggle(string text, Func<bool> state, Action toggle)
	{
		return Button(() => text + "  " + (state() ? "ON" : "OFF"), toggle, state);
	}

	public UiButton[] Buttons(params ButtonSpec[] specs)
	{
		EnsureSection();
		UiButton[] created = new UiButton[specs.Length];
		float width = (Width - RowGap * (specs.Length - 1)) / specs.Length;
		for (int i = 0; i < specs.Length; i++)
		{
			created[i] = AddButton(specs[i], i * (width + RowGap), cursor, width);
		}

		cursor += RowHeight + RowGap;
		return created;
	}

	public void Grid(int columns, IList<ButtonSpec> specs)
	{
		for (int start = 0; start < specs.Count; start += columns)
		{
			int count = Math.Min(columns, specs.Count - start);
			ButtonSpec[] row = new ButtonSpec[columns];
			for (int i = 0; i < columns; i++)
			{
				row[i] = i < count ? specs[start + i] : new ButtonSpec(string.Empty, null, null, () => false);
			}

			Buttons(row);
		}
	}

	public void Stepper(Func<string> text, Action minus, Action plus)
	{
		EnsureSection();
		AddButton(new ButtonSpec("-", minus), 0f, cursor, StepperButtonWidth);
		AddButton(new ButtonSpec("+", plus), Width - StepperButtonWidth, cursor, StepperButtonWidth);

		TextMeshProUGUI label = UiKit.NewText("StepperLabel", section.Root, DefaultFontSize, TextAlignmentOptions.Center, FontStyles.Normal);
		UiKit.PlaceTopLeft(label.rectTransform, StepperButtonWidth + RowGap, cursor, Width - 2f * (StepperButtonWidth + RowGap), RowHeight);
		section.Labels.Add(new UiLabel(label, text, Pick.Text));
		cursor += RowHeight + RowGap;
	}

	public void Row(Func<string> text, string buttonText, Action click, Func<bool> visible)
	{
		EnsureSection();
		float labelWidth = Width - RowButtonWidth - RowGap;
		TextMeshProUGUI label = UiKit.NewText("RowLabel", section.Root, DefaultFontSize, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
		UiKit.PlaceTopLeft(label.rectTransform, 0f, cursor, labelWidth, RowHeight);
		section.Labels.Add(new UiLabel(label, () => visible() ? text() : string.Empty, Pick.Text));

		AddButton(new ButtonSpec(buttonText, click, null, visible), Width - RowButtonWidth, cursor, RowButtonWidth);
		cursor += RowHeight + RowGap;
	}

	internal void Finish()
	{
		EnsureSection();
		CloseSection();
		foreach (SectionView built in view.Sections)
		{
			built.Root.gameObject.SetActive(false);
		}
	}

	private UiButton AddButton(ButtonSpec spec, float x, float y, float width)
	{
		UiButton button = ButtonFactory.Create(section.Root, "Button", spec, width, RowHeight, ButtonRadius, ButtonFontSize, context.RequestRefresh);
		UiKit.PlaceTopLeft(button.Rect, x, y, width, RowHeight);
		section.Buttons.Add(button);
		return button;
	}

	private void EnsureSection()
	{
		if (section == null)
		{
			Section(view.Page.Title);
		}
	}

	private void CloseSection()
	{
		if (section != null && cursor - RowGap > sectionHeight)
		{
			Debug.LogWarning("[AceUtilsPad] Page '" + view.Page.Title + "' overflows its section by "
							 + (cursor - RowGap - sectionHeight).ToString("0") + " units.");
		}
	}
}

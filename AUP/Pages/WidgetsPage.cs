using System.Collections.Generic;
using AUP.UI;
using AUP.Widgets;
using TMPro;
using UnityEngine;

namespace AUP.Pages;

public sealed class WidgetsPage : MenuPage
{
	private const int PageSize = 10;

	public WidgetsPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "AceWidgets";

	public override string IconId => "grid";

	private WidgetManager Manager => Context.Widgets;

	private WidgetConfig Current => Manager.Selected;

	public override void Build(PageBuilder ui)
	{
		BuildLibrary(ui);
		BuildEdit(ui);
		BuildStyle(ui);
		BuildBehavior(ui);
	}

	private void BuildLibrary(PageBuilder ui)
	{
		ui.Section("Library");
		ui.Toggle("AceWidgets", () => Manager.Profile.Enabled, () =>
		{
			Manager.Profile.Enabled = !Manager.Profile.Enabled;
			Manager.Touch(false);
		});
		ui.Buttons(
			new ButtonSpec(() => Manager.Search.Length > 0 ? "Search: " + Rich.Escape(Manager.Search) : "Search Widgets", StartSearch),
			new ButtonSpec("Clear Search", () => Manager.SetSearch(string.Empty), null, () => Manager.Search.Length > 0));
		List<ButtonSpec> slots = new List<ButtonSpec>();
		for (int i = 0; i < PageSize; i++)
		{
			int slot = i;
			slots.Add(new ButtonSpec(
				() => SlotName(slot),
				() => ToggleSlot(slot),
				() => Item(slot) != null && Item(slot).Enabled,
				() => Item(slot) != null));
		}

		ui.Grid(2, slots);
		ui.Stepper(() => "Page " + (Manager.LibraryPage + 1) + "   " + Manager.Library().Count + " widgets",
			() => Manager.StepLibraryPage(-1, PageSize), () => Manager.StepLibraryPage(1, PageSize));
		ui.Label("Tap a widget to show or hide it. Grab one with a grip, or drag it with the mouse, to move it.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private WidgetConfig Item(int slot)
	{
		return Manager.LibraryItem(slot, PageSize);
	}

	private string SlotName(int slot)
	{
		WidgetConfig item = Item(slot);
		return item == null ? string.Empty : Rich.Escape(WidgetManager.Name(item));
	}

	private void ToggleSlot(int slot)
	{
		WidgetConfig item = Item(slot);
		if (item == null)
		{
			return;
		}

		Manager.Select(item);
		Manager.ToggleEnabled(item);
	}

	private void StartSearch()
	{
		Context.Text.Begin("Search widgets", Manager.Search, Manager.SetSearch);
		Context.ShowPage<KeyboardPage>();
	}

	private void BuildEdit(PageBuilder ui)
	{
		ui.Section("Edit");
		ui.Stepper(() => Rich.Escape(Manager.SelectedName), () => Manager.StepSelected(-1), () => Manager.StepSelected(1));
		ui.Stepper(() => "Size " + Mathf.RoundToInt(Current.Scale * 100f) + "%", () => Scale(-0.1f), () => Scale(0.1f));
		ui.Stepper(() => "Rotation " + Mathf.RoundToInt(Mathf.Repeat(Current.Roll, 360f)), () => Rotate(-15f), () => Rotate(15f));
		ui.Stepper(() => "Transparency " + Mathf.RoundToInt((1f - Current.Alpha) * 100f) + "%", () => Fade(0.1f), () => Fade(-0.1f));
		ui.Buttons(
			Flag("Shown", () => Current.Enabled, () => Manager.ToggleEnabled(Current)),
			Flag("Locked", () => Current.Locked, () => Change(() => Current.Locked = !Current.Locked, false)));
		ui.Buttons(
			Flag("Small Icon", () => Current.Minimized, () => Change(() => Current.Minimized = !Current.Minimized, true)),
			Flag("Favorite", () => Current.Favorite, () => Change(() => Current.Favorite = !Current.Favorite, true)));
		ui.Buttons(
			new ButtonSpec("Bring Here", () => Manager.PlaceInFront(Current)),
			new ButtonSpec("Reset Spot", () => Manager.ResetSpot(Current)));
		ui.Button("Edit Its Text", EditText, null, () => Current.KindValue == WidgetKind.Text);
		ui.Label("Locked widgets cannot be grabbed. Favorites show up in the Widget Dock.", 1, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildStyle(PageBuilder ui)
	{
		ui.Section("Style");
		ui.Label(() => "Styling: <b>" + Rich.Escape(Manager.SelectedName) + "</b>", 1, 9f);
		ui.Stepper(() => "Background: " + WidgetOptions.BackgroundNames[Current.Background],
			() => Restyle(() => Current.Background = WidgetOptions.Wrap(Current.Background - 1, WidgetOptions.Backgrounds.Length)),
			() => Restyle(() => Current.Background = WidgetOptions.Wrap(Current.Background + 1, WidgetOptions.Backgrounds.Length)));
		ui.Stepper(() => "Text: " + WidgetOptions.TextColorNames[Current.TextColor],
			() => Restyle(() => Current.TextColor = WidgetOptions.Wrap(Current.TextColor - 1, WidgetOptions.TextColors.Length)),
			() => Restyle(() => Current.TextColor = WidgetOptions.Wrap(Current.TextColor + 1, WidgetOptions.TextColors.Length)));
		ui.Stepper(() => "Font: " + WidgetFonts.Name(Current.Font),
			() => Restyle(() => Current.Font = WidgetOptions.Wrap(Current.Font - 1, WidgetFonts.Count)),
			() => Restyle(() => Current.Font = WidgetOptions.Wrap(Current.Font + 1, WidgetFonts.Count)));
		ui.Buttons(
			new ButtonSpec(() => Current.Square ? "Square Corners" : "Round Corners", () => Restyle(() => Current.Square = !Current.Square)),
			new ButtonSpec(() => WidgetOptions.BorderNames[Current.Border],
				() => Restyle(() => Current.Border = WidgetOptions.Wrap(Current.Border + 1, WidgetOptions.BorderSizes.Length))));
		ui.Buttons(
			Flag("Shadow", () => Current.Shadow, () => Restyle(() => Current.Shadow = !Current.Shadow)),
			new ButtonSpec(() => "Spacing: " + WidgetOptions.PaddingNames[Current.Padding],
				() => Restyle(() => Current.Padding = WidgetOptions.Wrap(Current.Padding + 1, WidgetOptions.Paddings.Length))));
		ui.Button("Rename Widget", Rename);
		ui.Label("Theme colors follow the menu theme you picked.", 1, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildBehavior(PageBuilder ui)
	{
		ui.Section("Behavior");
		ui.Label(() => "Rules for: <b>" + Rich.Escape(Manager.SelectedName) + "</b>", 1, 9f);
		ui.Stepper(() => WidgetOptions.AnchorNames[Current.Anchor],
			() => Manager.SetAnchor(Current, Current.Anchor - 1), () => Manager.SetAnchor(Current, Current.Anchor + 1));
		ui.Stepper(() => "Show: " + WidgetOptions.VisibilityNames[Current.Visibility],
			() => Change(() => Current.Visibility = WidgetOptions.Wrap(Current.Visibility - 1, WidgetOptions.VisibilityNames.Length), false),
			() => Change(() => Current.Visibility = WidgetOptions.Wrap(Current.Visibility + 1, WidgetOptions.VisibilityNames.Length), false));
		ui.Stepper(() => "Updates every " + WidgetOptions.Intervals[Current.Interval].ToString("0.##") + " s",
			() => Change(() => Current.Interval = WidgetOptions.Wrap(Current.Interval - 1, WidgetOptions.Intervals.Length), false),
			() => Change(() => Current.Interval = WidgetOptions.Wrap(Current.Interval + 1, WidgetOptions.Intervals.Length), false));
		ui.Stepper(() => "Animation: " + WidgetOptions.AnimationNames[Current.Animation],
			() => Change(() => Current.Animation = WidgetOptions.Wrap(Current.Animation - 1, WidgetOptions.AnimationNames.Length), false),
			() => Change(() => Current.Animation = WidgetOptions.Wrap(Current.Animation + 1, WidgetOptions.AnimationNames.Length), false));
		ui.Stepper(RuleText, () => StepRule(-1), () => StepRule(1));
		ui.Stepper(() => WidgetOptions.GroupNames[Current.Group],
			() => Change(() => Current.Group = WidgetOptions.Wrap(Current.Group - 1, WidgetOptions.GroupNames.Length), false),
			() => Change(() => Current.Group = WidgetOptions.Wrap(Current.Group + 1, WidgetOptions.GroupNames.Length), false));
		ui.Label("Widgets in the same group move together. Alerts pop up as banners.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private string RuleText()
	{
		string[] names = WidgetOptions.RuleNames(Current.KindValue);
		return "Alert: " + names[Mathf.Clamp(Current.Rule, 0, names.Length - 1)];
	}

	private void StepRule(int direction)
	{
		string[] names = WidgetOptions.RuleNames(Current.KindValue);
		Change(() => Current.Rule = WidgetOptions.Wrap(Mathf.Clamp(Current.Rule, 0, names.Length - 1) + direction, names.Length), false);
	}

	private void EditText()
	{
		WidgetConfig target = Current;
		Context.Text.Begin("Widget text", target.Text, value =>
		{
			target.Text = PlayerTags.Clean(value);
			Manager.Restyle(target);
		});
		Context.ShowPage<KeyboardPage>();
	}

	private void Rename()
	{
		WidgetConfig target = Current;
		Context.Text.Begin("Widget name", WidgetManager.Name(target), value =>
		{
			target.Title = PlayerTags.Clean(value);
			Manager.Restyle(target);
		});
		Context.ShowPage<KeyboardPage>();
	}

	private void Scale(float delta)
	{
		Change(() => Current.Scale = Mathf.Clamp(Mathf.Round((Current.Scale + delta) * 10f) / 10f, 0.4f, 2.5f), false);
	}

	private void Rotate(float delta)
	{
		Change(() => Current.Roll = Mathf.Repeat(Current.Roll + delta, 360f), false);
	}

	private void Fade(float delta)
	{
		Change(() => Current.Alpha = Mathf.Clamp(Mathf.Round((Current.Alpha + delta) * 10f) / 10f, 0.2f, 1f), true);
	}

	private void Change(System.Action change, bool rebuild)
	{
		change();
		if (rebuild)
		{
			Manager.Restyle(Current);
		}
		else
		{
			Manager.Touch(false);
		}
	}

	private void Restyle(System.Action change)
	{
		Change(change, true);
	}

	private static ButtonSpec Flag(string text, System.Func<bool> value, System.Action flip)
	{
		return new ButtonSpec(() => text + "  " + (value() ? "ON" : "OFF"), flip, value);
	}
}

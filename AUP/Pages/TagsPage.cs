using System;
using System.Collections.Generic;
using AUP.UI;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;

namespace AUP.Pages;

public sealed class TagsPage : MenuPage
{
	private static readonly string[] TintNames = { "White", "Sky", "Orange", "Lime" };
	private static readonly string[] ColorRuleNames = { "Plain", "Platform", "FPS", "Ping", "Account Age" };
	private static readonly string[] TagPresets = { "GOOD VIBES", "CHILL", "PRO", "NEW HERE", "TAGGER", "RUNNER" };

	public TagsPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Name Tags";

	public override string IconId => "tag";

	private TagSettings Tags => Context.Settings.Tags;

	public override void Build(PageBuilder ui)
	{
		BuildFieldsSection(ui);
		BuildLookSection(ui);
		BuildRulesSection(ui);
		BuildSelfTagSection(ui);
	}

	private void BuildFieldsSection(PageBuilder ui)
	{
		ui.Section("Fields");
		ui.Toggle("Name Tags", () => Tags.Enabled.Value, () => Flip(Tags.Enabled));
		ui.Label("Pick what shows on each tag", 1, 8.5f, FontStyles.Normal, Pick.Muted);
		ui.Buttons(
			Toggle("Ping Dot", Tags.ShowPing),
			Toggle("FPS", Tags.ShowFps));
		ui.Buttons(
			Toggle("Platform Icon", Tags.ShowPlatform),
			Toggle("Account Age", Tags.ShowAccountAge));
		ui.Buttons(
			Toggle("NEW Badge", Tags.ShowNewBadge),
			Toggle("Friend Star", Tags.ShowFriendStar));
		ui.Buttons(
			Toggle("My Notes", Tags.ShowNotes),
			Toggle("Their Tags", Tags.ShowCustomTags));
		ui.Label("Ping is estimated from how late each player's data arrives. Their Tags shows tags other AceUtilsPad users made.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildLookSection(PageBuilder ui)
	{
		ui.Section("Look");
		ui.Heading("Tag size");
		ui.Stepper(() => "Size " + Tags.FontSize.Value.ToString("0"), () => Tags.StepFontSize(-2f), () => Tags.StepFontSize(2f));
		ui.Heading("Name color");
		ui.Button(() => "Color by: " + ColorRuleNames[(int)Tags.ColorMode], Tags.CycleColorMode);
		ui.Button(() => "Name tint: " + TintNames[Mathf.Clamp(Tags.NameTint.Value, 0, TintNames.Length - 1)], CycleTint);
		ui.Buttons(
			Toggle("Color Values", Tags.ColorValues),
			Toggle("Dark Card", Tags.Background));
		ui.Label("The dark card keeps names readable on bright maps. Plain uses the tint, other rules color the name by that stat.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildRulesSection(PageBuilder ui)
	{
		ui.Section("Rules");
		ui.Heading("New player marker");
		ui.Stepper(() => "Under " + Tags.NewAccountDays.Value + " days", () => Tags.StepNewAccountDays(-1), () => Tags.StepNewAccountDays(1));
		ui.Heading("Tag distance");
		ui.Stepper(() => "Hide past " + Tags.MaxDistance.Value.ToString("0") + " m", () => Tags.StepDistance(-5f), () => Tags.StepDistance(5f));
		ui.Buttons(
			Toggle("Fade Out", Tags.FadeEnabled),
			Toggle("Show Mine", Tags.ShowOwnTag));
		ui.Label("Tags fade over the last 30% of the distance. Show Mine puts your own tag above your head.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildSelfTagSection(PageBuilder ui)
	{
		ui.Section("My tag");
		ui.Label(Preview, 1, 14f, FontStyles.Bold, null, TextAlignmentOptions.Center);
		ui.Buttons(
			new ButtonSpec("Type My Tag", TypeTag),
			new ButtonSpec("Clear", () => Tags.SelfTag.Value = string.Empty, null, () => Tags.SelfTag.Value.Length > 0));
		ui.Stepper(() => "Color: " + TagPalette.SelfTagNames[TagPalette.SelfTagIndex(Tags.SelfTagColor.Value)], () => StepColor(-1), () => StepColor(1));
		ui.Grid(3, PresetButtons());
		ui.Label("Everyone running AceUtilsPad sees your tag above your name. Up to " + TextEntry.MaxLength + " letters.", 3, 8f, FontStyles.Normal, Pick.Muted);
	}

	private List<ButtonSpec> PresetButtons()
	{
		List<ButtonSpec> buttons = new List<ButtonSpec>();
		foreach (string preset in TagPresets)
		{
			string value = preset;
			buttons.Add(new ButtonSpec(value, () => Tags.SelfTag.Value = value, () => Tags.SelfTag.Value == value));
		}

		return buttons;
	}

	private string Preview()
	{
		string text = PlayerTags.Clean(Tags.SelfTag.Value);
		if (text.Length == 0)
		{
			return "<color=#9AA0B8>No tag yet</color>";
		}

		return "<color=#" + ColorUtility.ToHtmlStringRGB(TagPalette.SelfTag(Tags.SelfTagColor.Value)) + ">[ " + Rich.Escape(text) + " ]</color>";
	}

	private void TypeTag()
	{
		Context.Text.Begin("Your tag", Tags.SelfTag.Value, value => Tags.SelfTag.Value = PlayerTags.Clean(value));
		Context.ShowPage<KeyboardPage>();
	}

	private void StepColor(int direction)
	{
		int count = TagPalette.SelfTagColors.Length;
		Tags.SelfTagColor.Value = ((TagPalette.SelfTagIndex(Tags.SelfTagColor.Value) + direction) % count + count) % count;
	}

	private static ButtonSpec Toggle(string text, ConfigEntry<bool> entry)
	{
		return new ButtonSpec(() => text + "  " + (entry.Value ? "ON" : "OFF"), () => Flip(entry), () => entry.Value);
	}

	private static void Flip(ConfigEntry<bool> entry)
	{
		entry.Value = !entry.Value;
	}

	private void CycleTint()
	{
		Tags.NameTint.Value = (Tags.NameTint.Value + 1) % Math.Min(TintNames.Length, TagPalette.NameTints.Length);
	}
}

using System.Collections.Generic;
using AUP.UI;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;

namespace AUP.Pages;

public sealed class ThemesPage : MenuPage
{
	private const int GridColumns = 2;
	private const float OpacityStep = 0.05f;

	public ThemesPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Themes";

	public override string IconId => "palette";

	public override void Build(PageBuilder ui)
	{
		BuildThemeSection(ui);
		BuildLookSection(ui);
	}

	private void BuildThemeSection(PageBuilder ui)
	{
		ui.Section("Themes");
		ui.Label(() => "<b>" + Context.Theme.Current.Name + "</b>", 1, 12f, FontStyles.Normal, Pick.Accent);
		ui.Label(() => Context.Theme.Current.Description, 1, 8.5f, FontStyles.Normal, Pick.Muted);
		ui.Space(2f);
		ui.Grid(GridColumns, ThemeButtons());
		ui.Label("Lava is the default and matches the orb logo.", 1, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildLookSection(PageBuilder ui)
	{
		ui.Section("Look");
		ui.Heading("Opacity");
		ui.Stepper(() => "Opacity " + Mathf.RoundToInt(Context.Settings.Opacity.Value * 100f) + "%",
			() => Context.Settings.StepOpacity(-OpacityStep), () => Context.Settings.StepOpacity(OpacityStep));
		ui.Buttons(
			Toggle("Scenery", Context.Settings.ThemeScenery),
			Toggle("Particles", Context.Settings.ThemeParticles));
		ui.Buttons(
			Toggle("Moving Colors", Context.Settings.AnimatedThemes),
			Toggle("Animations", Context.Settings.MenuAnimations));
		ui.Label("Scenery draws the theme's trees, crystals or skyline along the bottom. Animations covers the pop open, "
				 + "press squish, ripples and sliding pages.", 3, 8f, FontStyles.Normal, Pick.Muted);
	}

	private List<ButtonSpec> ThemeButtons()
	{
		List<ButtonSpec> buttons = new List<ButtonSpec>();
		foreach (MenuTheme theme in ThemeLibrary.All)
		{
			MenuTheme captured = theme;
			string swatch = "<color=#" + ColorUtility.ToHtmlStringRGB(captured.Accent) + ">||</color>  ";
			buttons.Add(new ButtonSpec(swatch + captured.Name, () => Context.Theme.Select(captured.Name), () => Context.Theme.IsSelected(captured)));
		}

		return buttons;
	}

	private static ButtonSpec Toggle(string text, ConfigEntry<bool> entry)
	{
		return new ButtonSpec(() => text + "  " + (entry.Value ? "ON" : "OFF"), () => Flip(entry), () => entry.Value);
	}

	private static void Flip(ConfigEntry<bool> entry)
	{
		entry.Value = !entry.Value;
	}
}

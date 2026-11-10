using AUP.UI;
using TMPro;

namespace AUP.Pages;

public sealed class WorldPage : MenuPage
{
	private static readonly string[] StrengthNames = { "Off", "Breeze", "Gale", "Storm" };
	private static readonly string[] CompassNames = { "North", "North-East", "East", "South-East", "South", "South-West", "West", "North-West" };

	public WorldPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "World";

	public override string IconId => "sun";

	private WorldEnvironmentController World => Context.World;

	private WindSettings Wind => Context.Settings.Wind;

	public override void Build(PageBuilder ui)
	{
		BuildWeatherSection(ui);
		BuildWindSection(ui);
	}

	private void BuildWeatherSection(PageBuilder ui)
	{
		ui.Section("Time & Weather");
		ui.Label(World.GetStatus, 2, 9f, FontStyles.Normal, Pick.Accent);
		ui.Space(4f);
		ui.Buttons(
			new ButtonSpec("Time -", () => World.StepTime(-1)),
			new ButtonSpec("Auto Time", World.ResetTime, () => !World.TimeOverridden),
			new ButtonSpec("Time +", () => World.StepTime(1)));
		ui.Buttons(
			new ButtonSpec("Clear Sky", () => World.SetWeather(false), () => World.ClearForced),
			new ButtonSpec("Auto Weather", World.ResetWeather, () => !World.WeatherOverridden),
			new ButtonSpec("Rain", () => World.SetWeather(true), () => World.RainForced));
		ui.Button("Reset World", World.ResetAll);
		ui.Label(() => World.LastMessage, 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildWindSection(PageBuilder ui)
	{
		ui.Section("Wind");
		ui.Label(() => Context.Wind.Describe(), 1, 10f, FontStyles.Bold, Pick.Accent);
		ui.Space(4f);
		ui.Heading("Strength");
		ui.Stepper(() => StrengthNames[Wind.StrengthLevel] + " (" + Wind.StrengthLevel + "/" + WindSettings.MaxStrength + ")",
			() => Wind.StepStrength(-1), () => Wind.StepStrength(1));
		ui.Heading("Direction");
		ui.Stepper(() => "From the " + CompassNames[Wind.DirectionIndex],
			() => Wind.StepDirection(-1), () => Wind.StepDirection(1));
		ui.Toggle("Gusts", () => Wind.Gusts.Value, () => Wind.Gusts.Value = !Wind.Gusts.Value);
		ui.Button("Wind Off", () => Wind.Strength.Value = 0, () => Wind.StrengthLevel == 0);
		ui.Label("Wind is a local visual effect. Other players never see it.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}
}

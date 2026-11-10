using AUP.UI;
using TMPro;

namespace AUP.Pages;

public sealed class CamPage : MenuPage
{
	public CamPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "AceCam";

	public override string IconId => "camera";

	private AceCam Cam => Context.Cam;

	public override void Build(PageBuilder ui)
	{
		ui.Section("Camera");
		ui.Toggle("AceCam", () => Cam.Settings.Enabled.Value, Cam.Toggle);
		ui.Buttons(
			new ButtonSpec("Bring Tablet", Cam.SummonTablet),
			new ButtonSpec("Take Photo", Cam.TakePhoto));
		ui.Buttons(
			new ButtonSpec(() => Cam.Recording ? "Stop Recording" : "Record Video", Cam.ToggleRecording, () => Cam.Recording),
			new ButtonSpec(() => "Cinematic  " + (Cam.Settings.Cinematic.Value ? "ON" : "OFF"),
				() => Cam.Settings.Cinematic.Value = !Cam.Settings.Cinematic.Value, () => Cam.Settings.Cinematic.Value));
		CamUi.BuildModes(ui, Cam, 3);
		ui.Stepper(() => "Preset: " + AceCam.PresetNames[Cam.PresetIndex], () => Cam.StepPreset(-1), () => Cam.StepPreset(1));
		ui.Button("Use Preset", Cam.ApplyPreset);
		ui.Label(() => Cam.StatusLine + "  |  " + Cam.Status, 2, 8f, FontStyles.Normal, Pick.Muted);

		ui.Section("Lens");
		CamUi.BuildLens(ui, Cam);

		ui.Section("Move");
		CamUi.BuildMove(ui, Cam);
		ui.Label("Move buttons switch to Free mode. Rotation also tilts the other modes.", 2, 8f, FontStyles.Normal, Pick.Muted);

		ui.Section("Gallery");
		ui.Label(() => "<b>" + Cam.ShotCount + "</b> photos saved", 1, 11f);
		CamUi.BuildShotControls(ui, Cam);
		ui.Space(4f);
		ui.Label("Photos are full HD PNG files. Videos are 720p AVI files in the videos folder. "
				 + "The tablet's Shots tab shows a preview of each photo. Grab the tablet by its side grips to move it, "
				 + "then let go to leave it floating as a tripod.", 6, 8.5f, FontStyles.Normal, Pick.Muted);
	}
}

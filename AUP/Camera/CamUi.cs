using System.Collections.Generic;
using AUP.UI;
using TMPro;
using UnityEngine;

namespace AUP;

internal static class CamUi
{
	public static void BuildModes(PageBuilder ui, AceCam cam, int columns)
	{
		List<ButtonSpec> modes = new List<ButtonSpec>();
		for (int i = 0; i < CamSettings.ModeNames.Length; i++)
		{
			CamMode mode = (CamMode)i;
			modes.Add(new ButtonSpec(CamSettings.ModeNames[i], () => cam.SetMode(mode), () => cam.Active && cam.Settings.CurrentMode == mode));
		}

		ui.Grid(columns, modes);
	}

	public static void BuildLens(PageBuilder ui, AceCam cam)
	{
		CamSettings settings = cam.Settings;
		ui.Label(() => "FOV  " + Mathf.RoundToInt(settings.Fov.Value), 1, 8.5f);
		ui.Slider(() => Mathf.InverseLerp(CamSettings.MinFov, CamSettings.MaxFov, settings.Fov.Value),
			value => settings.Fov.Value = Mathf.Round(Mathf.Lerp(CamSettings.MinFov, CamSettings.MaxFov, value)));
		ui.Label(() => "Smoothness  " + Mathf.RoundToInt(settings.Smooth.Value * 100f) + "%", 1, 8.5f);
		ui.Slider(() => settings.Smooth.Value, value => settings.Smooth.Value = Mathf.Round(value * 20f) / 20f);
		ui.Label(() => "Clipping  " + settings.Near.Value.ToString("0.00") + " m", 1, 8.5f);
		ui.Slider(() => Mathf.InverseLerp(CamSettings.MinNear, CamSettings.MaxNear, settings.Near.Value),
			value => settings.Near.Value = Mathf.Max(CamSettings.MinNear, Mathf.Round(Mathf.Lerp(CamSettings.MinNear, CamSettings.MaxNear, value) * 100f) / 100f));
		ui.Label(() => "Distance  " + settings.Distance.Value.ToString("0.0") + " m", 1, 8.5f);
		ui.Slider(() => Mathf.InverseLerp(CamSettings.MinDistance, CamSettings.MaxDistance, settings.Distance.Value),
			value => settings.Distance.Value = Mathf.Round(Mathf.Lerp(CamSettings.MinDistance, CamSettings.MaxDistance, value) * 10f) / 10f);
		ui.Label(() => "Camera size  " + settings.Size.Value.ToString("0.0") + "x", 1, 8.5f);
		ui.Slider(() => Mathf.InverseLerp(CamSettings.MinSize, CamSettings.MaxSize, settings.Size.Value),
			value => settings.Size.Value = Mathf.Round(Mathf.Lerp(CamSettings.MinSize, CamSettings.MaxSize, value) * 10f) / 10f);
		ui.Button("Reset Camera", cam.ResetCamera);
	}

	public static void BuildMove(PageBuilder ui, AceCam cam)
	{
		CamSettings settings = cam.Settings;
		ui.Label(() => "Move speed  " + settings.Speed.Value.ToString("0.0") + "x", 1, 8.5f);
		ui.Slider(() => Mathf.InverseLerp(CamSettings.MinSpeed, CamSettings.MaxSpeed, settings.Speed.Value),
			value => settings.Speed.Value = Mathf.Round(Mathf.Lerp(CamSettings.MinSpeed, CamSettings.MaxSpeed, value) * 10f) / 10f);
		ui.Grid(3, new List<ButtonSpec>
		{
			new ButtonSpec("Left", () => cam.Move(Vector3.left)),
			new ButtonSpec("Forward", () => cam.Move(Vector3.forward)),
			new ButtonSpec("Right", () => cam.Move(Vector3.right)),
			new ButtonSpec("Down", () => cam.Move(Vector3.down)),
			new ButtonSpec("Back", () => cam.Move(Vector3.back)),
			new ButtonSpec("Up", () => cam.Move(Vector3.up)),
			new ButtonSpec("Yaw -", () => cam.Turn(Vector3.down)),
			new ButtonSpec("Pitch +", () => cam.Turn(Vector3.left)),
			new ButtonSpec("Yaw +", () => cam.Turn(Vector3.up)),
			new ButtonSpec("Roll -", () => cam.Turn(Vector3.forward)),
			new ButtonSpec("Pitch -", () => cam.Turn(Vector3.right)),
			new ButtonSpec("Roll +", () => cam.Turn(Vector3.back))
		});
		ui.Button("Center View", cam.CenterView);
	}

	public static void BuildShotControls(PageBuilder ui, AceCam cam)
	{
		ui.Label(() => cam.GalleryLabel, 1, 8f, FontStyles.Normal, Pick.Muted);
		ui.Buttons(
			new ButtonSpec("<", () => cam.StepGallery(-1)),
			new ButtonSpec(">", () => cam.StepGallery(1)));
		ui.Buttons(
			new ButtonSpec("Delete", cam.DeleteShown, null, () => cam.ShotCount > 0),
			new ButtonSpec("Folder", cam.OpenFolder));
	}
}

using AUP.UI;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;

namespace AUP.Pages;

public sealed class SettingsPage : MenuPage
{
	public SettingsPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Settings";

	public override string IconId => "gear";

	private MenuSettings Settings => Context.Settings;

	public override void Build(PageBuilder ui)
	{
		BuildGeneralSection(ui);
		BuildShapeSection(ui);
		BuildSizeSection(ui);
		BuildAlertSection(ui);
		BuildPcSection(ui);
	}

	private void BuildGeneralSection(PageBuilder ui)
	{
		ui.Section("General");
		ui.Toggle("Gun Pointer", () => Settings.GunRay.Value, () => Flip(Settings.GunRay));
		ui.Toggle("Auto Lock", () => Settings.AutoLock.Value, () => Flip(Settings.AutoLock));
		ui.Toggle("Menu Animations", () => Settings.MenuAnimations.Value, () => Flip(Settings.MenuAnimations));
		ui.Toggle("Welcome Screen", () => Settings.Splash.Value, () => Flip(Settings.Splash));
		ui.Button(() => "Anti-Lag: " + MenuSettings.AntiLagNames[Mathf.Clamp(Settings.AntiLag.Value, 0, 2)], Settings.CycleAntiLag, () => Settings.AntiLag.Value > 0);
		ui.Label(() => PerformanceGuard.Status, 1, 8.5f, FontStyles.Bold, Pick.Accent);
		ui.Label("Auto pauses particles and moving colors when your frame rate drops, then turns them back on.", 2, 8f, FontStyles.Normal, Pick.Muted);
		ui.Button("Reset All Settings", Settings.ResetMenu);
		ui.Label(() => Context.KillSwitch.Status, 1, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildShapeSection(PageBuilder ui)
	{
		ui.Section("Shapes");
		ui.Toggle("Orb Menu", () => Settings.OrbMode.Value, () => Flip(Settings.OrbMode));
		ui.Button("Summon Orb Here", () => Context.SummonOrb?.Invoke(), null, () => Settings.OrbMode.Value);
		ui.Label(() => Context.OrbStatus != null ? Context.OrbStatus() : string.Empty, 1, 8.5f, FontStyles.Bold, Pick.Accent);
		ui.Label("Grab the orb with a grip, throw it, and the menu unfolds where it lands. Grab it again to fold it up. "
				 + "Left Y or G on PC calls it back.", 3, 8f, FontStyles.Normal, Pick.Muted);
		ui.Space(4f);
		ui.Toggle("AceOne Phone", () => Settings.PhoneMode.Value, () => Flip(Settings.PhoneMode));
		ui.Label("Turns the wrist menu into the AceOne smartphone. Every page becomes an app on the home screen, "
				 + "and the bar at the bottom takes you home. Orb and phone cannot be on together.", 4, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildSizeSection(PageBuilder ui)
	{
		ui.Section("Size");
		ui.Heading("Menu size");
		ui.Buttons(SizeButton(0), SizeButton(1), SizeButton(2), SizeButton(3), SizeButton(4));
		ui.Heading("Wrist position");
		ui.Stepper(() => "Slide " + Mathf.RoundToInt(Settings.WristOffset.Value * 100f) + " cm", () => Settings.StepWristOffset(-0.02f), () => Settings.StepWristOffset(0.02f));
		ui.Label("The wrist menu sits beside your hand on its grip. Slide it further away or closer if your hand covers it.", 2, 8f, FontStyles.Normal, Pick.Muted);
		ui.Heading("Pointer range");
		ui.Stepper(() => "Reach " + Settings.PointerRange.Value.ToString("0.#") + " m", () => Settings.StepPointerRange(-2.5f), () => Settings.StepPointerRange(2.5f));
	}

	private void BuildAlertSection(PageBuilder ui)
	{
		ui.Section("Alerts");
		ui.Buttons(
			Toggle("Owner Joins", Settings.OwnerAlerts),
			Toggle("Friend Joins", Settings.FriendJoinAlerts));
		ui.Buttons(
			Toggle("Friend Lobbies", Settings.FriendAlerts),
			Toggle("Mod Detection", Settings.DetectionAlerts));
		ui.Heading("Notification sounds");
		ui.Stepper(() => "Owner: " + NotifySound.Name(Settings.OwnerSound.Value), () => StepSound(Settings.OwnerSound, -1), () => StepSound(Settings.OwnerSound, 1));
		ui.Stepper(() => "Friends: " + NotifySound.Name(Settings.FriendSound.Value), () => StepSound(Settings.FriendSound, -1), () => StepSound(Settings.FriendSound, 1));
		ui.Stepper(() => "Everything else: " + NotifySound.Name(Settings.AlertSound.Value), () => StepSound(Settings.AlertSound, -1), () => StepSound(Settings.AlertSound, 1));
		ui.Stepper(() => "Volume " + Mathf.RoundToInt(Settings.SoundVolume.Value * 100f) + "%", () => StepVolume(-0.1f), () => StepVolume(0.1f));
		ui.Label("Each sound plays when you pick it so you can hear it first.", 1, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildPcSection(PageBuilder ui)
	{
		ui.Section("PC");
		ui.Label("<b>J</b>  open or close the screen menu\n"
				 + "<b>Mouse</b>  hover and click any button, wheel to flip sections\n"
				 + "<b>Tab / Shift+Tab</b>  next or previous page\n"
				 + "<b>Q / E</b> or <b>Left / Right</b>  previous or next section\n"
				 + "<b>1 - 9</b>  jump to a page\n"
				 + "<b>Esc</b>  close the menu\n"
				 + "<b>Middle click</b> or <b>F</b>  lock onto the player under the cursor\n"
				 + "<b>G</b>  call the orb (Orb Menu on)\n"
				 + "<b>Hold right mouse</b>  grab the orb, wheel to push or pull, let go to throw\n"
				 + "<b>Widgets</b>  drag with left mouse, wheel over one to resize it\n"
				 + "<b>AceCam</b>  click the tablet buttons with the mouse\n"
				 + "<b>Typing</b>  goes straight into tags, notes and names",
			19, 8.5f);
	}

	private void StepSound(ConfigEntry<int> entry, int direction)
	{
		int count = NotifySound.Names.Length;
		entry.Value = ((NotifySound.Clamp(entry.Value) + direction) % count + count) % count;
		NotifySound.Play(entry.Value, Settings.SoundVolume.Value);
	}

	private void StepVolume(float delta)
	{
		Settings.StepVolume(delta);
		NotifySound.Play(Settings.AlertSound.Value, Settings.SoundVolume.Value);
	}

	private ButtonSpec SizeButton(int index)
	{
		return new ButtonSpec(MenuSettings.SizeNames[index], () => Settings.MenuSize.Value = index, () => Settings.SizeIndex == index);
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

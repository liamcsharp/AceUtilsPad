using AUP.UI;
using TMPro;
using UnityEngine;

namespace AUP.Pages;

public sealed class PlayerInfoPage : MenuPage
{
	private const string NoSelection = "No player selected";

	public PlayerInfoPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Player Info";

	public override string IconId => "info";

	private VRRig Selected => Context.Selection.Selected;

	public override void Build(PageBuilder ui)
	{
		ui.Label(NameLine, 1, 12f, FontStyles.Bold);
		ui.Label(PointerLine, 1, 8f, FontStyles.Normal, Pick.Muted);
		ui.Space(4f);
		ui.Label(() => Field("Color code", PlayerInfo.ColorMarkup(Selected)), 1, 10f);
		ui.Label(() => Field("Platform", PlatformValue()), 1, 10f);
		ui.Label(() => Field("Headset", Selected == null ? "-" : PlayerInfo.Headset(Selected)), 1, 10f);
		ui.Label(() => Field("FPS", FpsValue()), 1, 10f);
		ui.Label(() => Field("Ping", PingValue()), 1, 10f);
		ui.Label(() => Field("Created", CreatedValue()), 1, 9f);
		ui.Label(() => Field("Account age", AgeValue()), 1, 9f);
		ui.Label(() => Field("Your note", NoteValue()), 1, 9.5f);
		ui.Space(2f);
		ui.Buttons(
			new ButtonSpec("Next Note", () => Context.Notes.CycleNote(Selected), null, HasSelection),
			new ButtonSpec("Type Note", TypeNote, null, HasSelection),
			new ButtonSpec(() => Context.Notes.IsStarred(Selected) ? "Starred" : "Star", () => Context.Notes.ToggleStar(Selected),
				() => Context.Notes.IsStarred(Selected), HasSelection));
		ui.Button(() => PlayerMuting.IsMuted(Selected) ? "Unmute Player" : "Mute Player", ToggleMute, () => PlayerMuting.IsMuted(Selected));
		ui.Label("VR: hold the right grip, aim and pull the trigger. PC: middle-click a player or press F over them.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private bool HasSelection()
	{
		return Selected != null;
	}

	private string NoteValue()
	{
		if (Selected == null)
		{
			return "-";
		}

		string note = Context.Notes.NoteFor(Selected);
		return note.Length == 0 ? "<color=#9AA0B8>none</color>" : "<i>" + Rich.Escape(note) + "</i>";
	}

	private void TypeNote()
	{
		VRRig target = Selected;
		if (target == null)
		{
			return;
		}

		Context.Text.Begin("Note for " + PlayerInfo.DisplayName(target), Context.Notes.NoteFor(target), value => Context.Notes.SetNote(target, value));
		Context.ShowPage<KeyboardPage>();
	}

	private static string Field(string name, string value)
	{
		return "<b>" + name + ":</b> " + value;
	}

	private string NameLine()
	{
		return Selected == null ? NoSelection : Rich.Escape(PlayerInfo.DisplayName(Selected));
	}

	private string PointerLine()
	{
		if (Selected == null)
		{
			return "Aim at a player and pull the trigger to lock on";
		}

		return Context.Selection.Locked ? "Locked on. Pull the trigger again to release." : "Selected";
	}

	private string PlatformValue()
	{
		if (Selected == null)
		{
			return "-";
		}

		string platform = PlayerInfo.Platform(Selected);
		return Colored(PlatformNames.Friendly(platform), TagPalette.PlatformIcon(platform)) + " <color=#9AA0B8>(" + Rich.Escape(platform) + ")</color>";
	}

	private string FpsValue()
	{
		int fps = Selected == null ? 0 : PlayerInfo.Fps(Selected);
		return fps > 0 ? Colored(fps.ToString(), TagPalette.Fps(fps)) : "N/A";
	}

	private string PingValue()
	{
		int? ping = Selected == null ? null : LatencyTracker.GetPingMs(Selected);
		return ping.HasValue ? Colored(ping.Value + " ms", TagPalette.Ping(ping.Value)) + " (estimate)" : "N/A";
	}

	private string CreatedValue()
	{
		return Selected == null ? "-" : PlayerInfo.GetAccountAge(Selected).DateText;
	}

	private string AgeValue()
	{
		if (Selected == null)
		{
			return "-";
		}

		AccountAge age = PlayerInfo.GetAccountAge(Selected);
		if (!age.IsReady)
		{
			return age.ShortText;
		}

		int threshold = Context.Settings.Tags.NewAccountDays.Value;
		string text = age.Days + " days";
		return age.Days < threshold ? Colored(text, TagPalette.NewBadge) + " <b>NEW</b>" : text;
	}

	private void ToggleMute()
	{
		PlayerMuting.Toggle(Selected);
	}

	private static string Colored(string text, Color color)
	{
		return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
	}
}

using System.Collections.Generic;
using AUP.UI;
using TMPro;

namespace AUP.Pages;

public sealed class KeyboardPage : MenuPage
{
	private const int Columns = 9;
	private const string Keys = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.!?&#+*";

	public KeyboardPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Keyboard";

	public override string IconId => "keys";

	private TextEntry Text => Context.Text;

	public override void Build(PageBuilder ui)
	{
		ui.Label(() => Text.Active ? "Typing: " + Text.Purpose : "Nothing to type right now", 1, 9f, FontStyles.Bold, Pick.Accent);
		ui.Label(Display, 1, 13f, FontStyles.Bold);
		ui.Grid(Columns, KeyButtons());
		ui.Buttons(
			new ButtonSpec("Caps", Text.ToggleCaps, () => Text.Caps),
			new ButtonSpec("Space", Text.Space),
			new ButtonSpec("Back", Text.Backspace),
			new ButtonSpec("Clear", Text.Clear),
			new ButtonSpec("Cancel", Text.Cancel),
			new ButtonSpec("Save", Text.Commit, () => Text.Active));
		ui.Label("On PC just type. Enter saves and Esc cancels.", 1, 8f, FontStyles.Normal, Pick.Muted);
	}

	private string Display()
	{
		if (!Text.Active)
		{
			return "<color=#9AA0B8>Pick Type My Tag or Type Note first</color>";
		}

		string value = Rich.Escape(Text.Value);
		bool blink = UnityEngine.Time.unscaledTime % 1f < 0.5f;
		return value + (blink ? "_" : " ") + "  <size=60%><color=#9AA0B8>" + Text.Value.Length + "/" + TextEntry.MaxLength + "</color></size>";
	}

	private List<ButtonSpec> KeyButtons()
	{
		List<ButtonSpec> buttons = new List<ButtonSpec>();
		foreach (char key in Keys)
		{
			string upper = key.ToString();
			string lower = upper.ToLowerInvariant();
			buttons.Add(new ButtonSpec(() => Text.Caps ? upper : lower, () => Text.Type(upper)));
		}

		return buttons;
	}
}

using AUP.UI;
using TMPro;

namespace AUP.Pages;

public sealed class LobbyPage : MenuPage
{
	public LobbyPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Lobby";

	public override string IconId => "group";

	private LobbyService Lobby => Context.Lobby;

	public override void Build(PageBuilder ui)
	{
		ui.Label(() => "<b>Lobby:</b> " + (Lobby.InRoom ? Lobby.RoomName : "Not in room"), 1, 11f);
		ui.Label(() => "<b>Players:</b> " + Lobby.PlayerCountText, 1, 10f);
		ui.Label(() => "<b>Game Mode:</b> " + Lobby.GameMode, 1, 10f);
		ui.Space(8f);
		ui.Buttons(
			new ButtonSpec("Disconnect", Lobby.Disconnect),
			new ButtonSpec("Keyboard", ToggleKeyboard, () => Context.Keyboard.Visible),
			new ButtonSpec("Lobby Hop", Lobby.Hop));
		ui.Buttons(
			new ButtonSpec(() => RejoinText(), Lobby.Rejoin),
			new ButtonSpec("Copy Code", CopyCode));
		ui.Space(4f);
		ui.Label(() => Lobby.Status, 2, 8.5f, FontStyles.Normal, Pick.Muted);
	}

	private string RejoinText()
	{
		return string.IsNullOrEmpty(Lobby.LastRoom) ? "Rejoin" : "Rejoin " + Lobby.LastRoom;
	}

	private void ToggleKeyboard()
	{
		Context.Keyboard.Toggle();
	}

	private void CopyCode()
	{
		if (!Lobby.InRoom)
		{
			return;
		}

		UnityEngine.GUIUtility.systemCopyBuffer = Lobby.RoomName;
	}
}

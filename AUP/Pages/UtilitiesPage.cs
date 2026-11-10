using System;
using AUP.UI;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace AUP.Pages;

public sealed class UtilitiesPage : MenuPage
{
	private string status = "Utilities are ready.";

	public UtilitiesPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Utilities";

	public override string IconId => "wrench";

	public override void Build(PageBuilder ui)
	{
		ui.Label(InfoText, 4, 9.5f);
		ui.Space(6f);
		ui.Buttons(
			new ButtonSpec("Rejoin", Rejoin),
			new ButtonSpec("Mute All", () => MuteAll(true)),
			new ButtonSpec("Unmute All", () => MuteAll(false)));
		ui.Buttons(
			new ButtonSpec("Refresh Lists", RefreshLists),
			new ButtonSpec("Hide Menu", Context.HideMenu),
			new ButtonSpec("Quit Game", QuitGame));
		ui.Space(4f);
		ui.Label(() => status, 2, 8.5f, FontStyles.Normal, Pick.Muted);
	}

	private string InfoText()
	{
		string ping = PhotonNetwork.IsConnected ? PhotonNetwork.GetPing() + " ms" : "---";
		string region = PhotonNetwork.IsConnected && !string.IsNullOrEmpty(PhotonNetwork.CloudRegion)
			? PhotonNetwork.CloudRegion.ToUpperInvariant()
			: "---";
		string room = Context.Lobby.InRoom
			? Context.Lobby.RoomName + " (" + Context.Lobby.PlayerCountText + ")"
			: "Not in room";

		return "<b>Name:</b> " + Rich.Escape(PlayerInfo.LocalName())
			   + "\n<b>FPS:</b> " + Mathf.RoundToInt(Context.Fps) + "   <b>Ping:</b> " + ping
			   + "\n<b>Room:</b> " + Rich.Escape(room) + "   <b>Region:</b> " + region
			   + "\n<b>Time:</b> " + DateTime.Now.ToString("h:mm tt") + "   <b>Muted:</b> " + PlayerMuting.MutedCount();
	}

	private void Rejoin()
	{
		Context.Lobby.Rejoin();
		status = Context.Lobby.Status;
	}

	private void MuteAll(bool muted)
	{
		int changed = PlayerMuting.SetAll(muted);
		string plural = changed == 1 ? string.Empty : "s";
		status = (muted ? "Muted " : "Unmuted ") + changed + " player" + plural + (muted ? " locally." : ".");
	}

	private void RefreshLists()
	{
		DetectionService.RefreshLists();
		Context.DetectionAlerts.Forget();
		status = "Refreshing mod and cheat lists from aceutils.org...";
	}

	private void QuitGame()
	{
		status = "Closing Gorilla Tag...";
		PresenceService.Withdraw();
		Application.Quit();
	}
}

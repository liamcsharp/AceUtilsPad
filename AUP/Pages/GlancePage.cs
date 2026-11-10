using System;
using AUP.UI;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace AUP.Pages;

public sealed class GlancePage : MenuPage
{
	public GlancePage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Glance";

	public override string IconId => "info";

	private AudioHandler Audio => Context.Audio;

	public override void Build(PageBuilder ui)
	{
		ui.Label(() => DateTime.Now.ToString("h:mm tt"), 1, 18f, FontStyles.Bold, Pick.Accent);
		ui.Label(() => DateTime.Now.ToString("dddd, MMM d"), 1, 8f, FontStyles.Normal, Pick.Muted);
		ui.Space(8f);
		ui.Label("Now playing", 1, 8f, FontStyles.Bold, Pick.Muted);
		ui.Label(() => Rich.Escape(Audio.CurrentTitle), 2, 9.5f, FontStyles.Bold);
		ui.Label(() => Rich.Escape(Audio.CurrentArtist), 1, 8f, FontStyles.Normal, Pick.Muted);
		ui.Progress(() => Audio.Progress, () => Audio.HasTimeline, 5f);
		ui.Pair(() => Audio.PositionText, () => Audio.DurationText, 7.5f);
		ui.Buttons(
			new ButtonSpec("<<", Audio.PreviousTrack),
			new ButtonSpec(() => Audio.IsPlaying ? "Pause" : "Play", Audio.TogglePlayPause, () => Audio.IsPlaying),
			new ButtonSpec(">>", Audio.NextTrack));
		ui.Space(6f);
		ui.Label("Lobby", 1, 8f, FontStyles.Bold, Pick.Muted);
		ui.Label(LobbyLine, 2, 9f);
		ui.Label(StatsLine, 1, 8.5f, FontStyles.Normal, Pick.Muted);
		ui.Space(6f);
		ui.Label("Selected player", 1, 8f, FontStyles.Bold, Pick.Muted);
		ui.Label(SelectedLine, 2, 9f);
		ui.Space(6f);
		ui.Label("Grab the orb to fold the menu away.", 2, 7.5f, FontStyles.Normal, Pick.Muted);
	}

	private string LobbyLine()
	{
		return Context.Lobby.InRoom
			? "<b>" + Rich.Escape(Context.Lobby.RoomName) + "</b>  " + Context.Lobby.PlayerCountText
			: "Not in a room";
	}

	private string StatsLine()
	{
		string ping = PhotonNetwork.IsConnected ? PhotonNetwork.GetPing() + " ms" : "--";
		return Mathf.RoundToInt(Context.Fps) + " fps   " + ping;
	}

	private string SelectedLine()
	{
		VRRig rig = Context.Selection.Selected;
		if (rig == null)
		{
			return "Nobody yet";
		}

		return "<b>" + Rich.Escape(PlayerInfo.DisplayName(rig)) + "</b>\n" + PlatformNames.Friendly(PlayerInfo.Platform(rig));
	}
}

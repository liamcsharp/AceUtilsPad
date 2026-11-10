using System.Collections.Generic;
using AUP.UI;
using Photon.Pun;
using UnityEngine;

namespace AUP;

public sealed class LobbyJoinAlerts : MonoBehaviour
{
	public const string OwnerName = "Wicked";

	private const float ScanInterval = 1f;
	private const float SettleSeconds = 2.5f;

	private readonly HashSet<string> announced = new HashSet<string>();
	private readonly List<AlertMessage> pending = new List<AlertMessage>();

	private MenuSettings settings;
	private AlertHub alerts;
	private FriendAlertService friends;
	private PlayerNotes notes;
	private string room;
	private float nextScanTime;
	private float roomJoinedTime;
	private float nextPostTime;

	public void Initialize(MenuSettings menuSettings, AlertHub alertHub, FriendAlertService friendService, PlayerNotes playerNotes)
	{
		settings = menuSettings;
		alerts = alertHub;
		friends = friendService;
		notes = playerNotes;
	}

	private void Update()
	{
		if (settings == null || alerts == null)
		{
			return;
		}

		if (pending.Count > 0 && Time.unscaledTime >= nextPostTime)
		{
			AlertMessage next = pending[0];
			pending.RemoveAt(0);
			nextPostTime = Time.unscaledTime + Mathf.Min(next.Seconds, 4f);
			alerts.Post(next);
		}

		if (Time.unscaledTime < nextScanTime)
		{
			return;
		}

		nextScanTime = Time.unscaledTime + ScanInterval;
		string current = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : null;
		if (current != room)
		{
			room = current;
			announced.Clear();
			pending.Clear();
			roomJoinedTime = Time.unscaledTime;
		}

		if (room == null || Time.unscaledTime - roomJoinedTime < SettleSeconds)
		{
			return;
		}

		foreach (VRRig rig in PlayerInfo.RemoteRigs())
		{
			string userId = PlayerInfo.UserId(rig);
			if (userId == PlayerInfo.Unknown || !announced.Add(userId))
			{
				continue;
			}

			if (OwnerConfiguration.IsOwner(userId))
			{
				if (settings.OwnerAlerts.Value)
				{
					pending.Add(new AlertMessage
					{
						Prefix = OwnerName,
						IconId = "crown",
						IconColor = TagPalette.Star,
						Text = "<b>Has Joined Your Lobby</b>",
						Seconds = 9f,
						Sound = settings.OwnerSound.Value
					});
				}

				continue;
			}

			bool friend = friends != null && friends.IsFriend(rig) || notes != null && notes.IsStarred(rig);
			if (friend && settings.FriendJoinAlerts.Value)
			{
				pending.Add(new AlertMessage
				{
					Prefix = PlayerInfo.DisplayName(rig),
					IconId = "star",
					IconColor = TagPalette.Star,
					Text = "is in your lobby",
					Seconds = 7f,
					Sound = settings.FriendSound.Value
				});
			}
		}
	}
}
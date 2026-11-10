using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace AUP;

public sealed class FriendEntry
{
	public string Key;
	public string Name;
	public string Room;
	public string Zone;
	public bool IsPublic;
}

public sealed class FriendAlertService : MonoBehaviour
{
	private const float ScanInterval = 2f;
	private const float BackendRefreshInterval = 30f;

	private readonly List<FriendEntry> online = new List<FriendEntry>();
	private readonly Dictionary<string, string> lastRooms = new Dictionary<string, string>();
	private readonly HashSet<string> present = new HashSet<string>();
	private readonly HashSet<string> friendIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> friendNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private MenuSettings settings;
	private float nextScanTime;
	private float nextBackendRefreshTime;
	private bool baselineReady;

	public event Action<FriendEntry> FriendJoined;

	public IReadOnlyList<FriendEntry> OnlineFriends => online;

	public FriendEntry LatestAlert { get; private set; }

	public int TotalFriends { get; private set; }

	public string Status { get; private set; } = "Waiting for the friends list...";

	public void Initialize(MenuSettings menuSettings)
	{
		settings = menuSettings;
	}

	public bool IsFriend(VRRig rig)
	{
		if (rig == null || friendIds.Count == 0 && friendNames.Count == 0)
		{
			return false;
		}

		string userId = PlayerInfo.UserId(rig);
		if (userId != PlayerInfo.Unknown && friendIds.Contains(userId))
		{
			return true;
		}

		string name = rig.Creator != null ? rig.Creator.NickName : null;
		return !string.IsNullOrEmpty(name) && friendNames.Contains(name);
	}

	public void RefreshNow()
	{
		nextBackendRefreshTime = 0f;
		Status = "Refreshing friends...";
	}

	private void Update()
	{
		if (settings == null || Time.unscaledTime < nextScanTime)
		{
			return;
		}

		nextScanTime = Time.unscaledTime + ScanInterval;
		RequestBackendRefresh();
		Scan();
	}

	private void RequestBackendRefresh()
	{
		if (Time.unscaledTime < nextBackendRefreshTime || FriendSystem.Instance == null || FriendBackendController.Instance == null)
		{
			return;
		}

		nextBackendRefreshTime = Time.unscaledTime + BackendRefreshInterval;
		FriendSystem.Instance.RefreshFriendsList();
	}

	private void Scan()
	{
		FriendBackendController backend = FriendBackendController.Instance;
		if (backend == null || backend.FriendsList == null)
		{
			return;
		}

		List<FriendBackendController.Friend> friends = new List<FriendBackendController.Friend>(backend.FriendsList);
		TotalFriends = friends.Count;
		online.Clear();
		present.Clear();
		friendIds.Clear();
		friendNames.Clear();

		string currentRoom = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : string.Empty;
		foreach (FriendBackendController.Friend friend in friends)
		{
			FriendBackendController.FriendPresence presence = friend == null ? null : friend.Presence;
			if (presence == null)
			{
				continue;
			}

			string key = !string.IsNullOrEmpty(presence.FriendLinkId) ? presence.FriendLinkId : presence.UserName;
			if (string.IsNullOrEmpty(key))
			{
				continue;
			}

			present.Add(key);
			if (!string.IsNullOrEmpty(presence.FriendLinkId))
			{
				friendIds.Add(presence.FriendLinkId);
			}

			if (!string.IsNullOrEmpty(presence.UserName))
			{
				friendNames.Add(presence.UserName);
			}

			string room = presence.RoomId ?? string.Empty;
			FriendEntry entry = new FriendEntry
			{
				Key = key,
				Name = string.IsNullOrEmpty(presence.UserName) ? "Friend" : presence.UserName,
				Room = room,
				Zone = presence.Zone,
				IsPublic = presence.IsPublic == true
			};

			if (room.Length > 0)
			{
				online.Add(entry);
			}

			DetectJoin(entry, currentRoom);
		}

		ForgetMissingFriends();
		baselineReady = baselineReady || friends.Count > 0;
		Status = online.Count + " of " + TotalFriends + " friends in a lobby.";
	}

	private void DetectJoin(FriendEntry entry, string currentRoom)
	{
		lastRooms.TryGetValue(entry.Key, out string previousRoom);
		lastRooms[entry.Key] = entry.Room;

		bool movedIntoLobby = entry.Room.Length > 0 && entry.Room != (previousRoom ?? string.Empty);
		if (baselineReady && movedIntoLobby && entry.Room != currentRoom && settings.FriendAlerts.Value)
		{
			LatestAlert = entry;
			FriendJoined?.Invoke(entry);
		}
	}

	private void ForgetMissingFriends()
	{
		List<string> stale = new List<string>();
		foreach (string key in lastRooms.Keys)
		{
			if (!present.Contains(key))
			{
				stale.Add(key);
			}
		}

		foreach (string key in stale)
		{
			lastRooms.Remove(key);
		}
	}
}

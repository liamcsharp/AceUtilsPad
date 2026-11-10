using System;
using System.Collections;
using System.Threading.Tasks;
using GorillaGameModes;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace AUP;

public sealed class LobbyService : MonoBehaviour
{
	private const float LeaveTimeoutSeconds = 10f;
	private const string DefaultStatus = "Room actions are ready.";

	private static readonly string[] GameModeNames = Enum.GetNames(typeof(GameModeType));

	private Coroutine pendingJoin;

	public string Status { get; private set; } = DefaultStatus;

	public string LastRoom { get; private set; }

	public bool InRoom => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null;

	public string RoomName => InRoom ? PhotonNetwork.CurrentRoom.Name : null;

	public string PlayerCountText
	{
		get
		{
			if (!InRoom)
			{
				return "---";
			}

			Room room = PhotonNetwork.CurrentRoom;
			return room.PlayerCount + "/" + room.MaxPlayers;
		}
	}

	public string GameMode
	{
		get
		{
			if (!InRoom)
			{
				return "---";
			}

			return MatchGameMode(ReadRoomProperty(PhotonNetwork.CurrentRoom, "gameMode", "mode"));
		}
	}

	private void Update()
	{
		if (InRoom && !string.IsNullOrEmpty(PhotonNetwork.CurrentRoom.Name))
		{
			LastRoom = PhotonNetwork.CurrentRoom.Name;
		}
	}

	public void Disconnect()
	{
		if (!InRoom)
		{
			Status = "You are not in a room.";
			return;
		}

		Status = "Leaving the current room...";
		NetworkSystem.Instance.ReturnToSinglePlayer();
	}

	public void Hop()
	{
		PhotonNetworkController controller = PhotonNetworkController.Instance;
		if (controller == null || NetworkSystem.Instance == null)
		{
			Status = "Networking is not ready yet.";
			return;
		}

		GorillaNetworkJoinTrigger trigger = controller.currentJoinTrigger;
		if (trigger == null || trigger == controller.privateTrigger)
		{
			Status = "Stand in a map join area first to hop public lobbies.";
			return;
		}

		Status = "Hopping to a new public lobby...";
		StartJoin(() => controller.AttemptToJoinPublicRoom(trigger));
	}

	public void Rejoin()
	{
		if (string.IsNullOrEmpty(LastRoom))
		{
			Status = "No previous room to rejoin yet.";
			return;
		}

		JoinCode(LastRoom);
	}

	public void JoinCode(string code)
	{
		JoinRoom(code, JoinType.Solo);
	}

	public void JoinFriend(string room, bool isPublic)
	{
		JoinRoom(room, isPublic ? JoinType.FriendStationPublic : JoinType.FriendStationPrivate);
	}

	private void JoinRoom(string room, JoinType joinType)
	{
		PhotonNetworkController controller = PhotonNetworkController.Instance;
		if (string.IsNullOrWhiteSpace(room) || controller == null || NetworkSystem.Instance == null)
		{
			Status = "Networking is not ready yet.";
			return;
		}

		if (InRoom && PhotonNetwork.CurrentRoom.Name == room)
		{
			Status = "You are already in " + room + ".";
			return;
		}

		Status = "Joining " + room + "...";
		StartJoin(() =>
		{
			if (GorillaComputer.instance != null)
			{
				GorillaComputer.instance.roomToJoin = room;
			}

			controller.AttemptToJoinSpecificRoom(room, joinType);
		});
	}

	private void StartJoin(Action join)
	{
		if (pendingJoin != null)
		{
			StopCoroutine(pendingJoin);
		}

		pendingJoin = StartCoroutine(JoinAfterLeaving(join));
	}

	private IEnumerator JoinAfterLeaving(Action join)
	{
		float deadline = Time.realtimeSinceStartup + LeaveTimeoutSeconds;
		if (InRoom)
		{
			Task leaving = NetworkSystem.Instance.ReturnToSinglePlayer();
			while (!leaving.IsCompleted && Time.realtimeSinceStartup < deadline)
			{
				yield return null;
			}
		}

		while ((InRoom || NetworkBusy) && Time.realtimeSinceStartup < deadline)
		{
			yield return null;
		}

		if (InRoom || NetworkBusy)
		{
			Status = InRoom ? "Could not leave the current room." : "Networking is busy. Try again in a moment.";
			pendingJoin = null;
			yield break;
		}

		join();
		pendingJoin = null;
	}

	private static bool NetworkBusy
	{
		get
		{
			NetSystemState state = NetworkSystem.Instance.netState;
			return state == NetSystemState.Initialization || state == NetSystemState.PingRecon
			       || state == NetSystemState.Connecting || state == NetSystemState.Disconnecting;
		}
	}

	private static string ReadRoomProperty(Room room, params string[] keys)
	{
		if (room == null || room.CustomProperties == null)
		{
			return null;
		}

		foreach (string key in keys)
		{
			if (room.CustomProperties.TryGetValue(key, out object value) && value != null)
			{
				return value.ToString();
			}
		}

		return null;
	}

	private static string MatchGameMode(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return "Unknown";
		}

		string best = null;
		foreach (string name in GameModeNames)
		{
			bool candidate = name != nameof(GameModeType.Count) && name != nameof(GameModeType.None);
			bool matches = candidate && raw.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;
			if (matches && (best == null || name.Length > best.Length))
			{
				best = name;
			}
		}

		return best ?? "Unknown";
	}
}

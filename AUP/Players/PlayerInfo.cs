using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace AUP;

public enum AccountStatus
{
	Unknown,
	Loading,
	Failed,
	Ready
}

public readonly struct AccountAge
{
	public AccountAge(AccountStatus status, DateTime createdUtc)
	{
		Status = status;
		CreatedUtc = createdUtc;
	}

	public AccountStatus Status { get; }
	public DateTime CreatedUtc { get; }
	public bool IsReady => Status == AccountStatus.Ready;

	public int Days => IsReady ? Math.Max(0, (int)(DateTime.UtcNow - CreatedUtc).TotalDays) : -1;

	public string DateText
	{
		get
		{
			switch (Status)
			{
				case AccountStatus.Ready:
					return CreatedUtc.ToString("MMM dd, yyyy HH:mm").ToUpperInvariant();
				case AccountStatus.Loading:
					return "LOADING";
				case AccountStatus.Failed:
					return "ERROR";
				default:
					return "UNKNOWN";
			}
		}
	}

	public string ShortText
	{
		get
		{
			if (!IsReady)
			{
				return Status == AccountStatus.Loading ? "..." : "?";
			}

			int days = Days;
			if (days < 1)
			{
				return "<1d";
			}

			if (days < 60)
			{
				return days + "d";
			}

			return days < 730 ? days / 30 + "mo" : days / 365 + "y";
		}
	}
}

public static class PlayerInfo
{
	public const string Unknown = "Unknown";

	private const float AccountRetrySeconds = 60f;

	private static readonly FieldInfo FpsField = AccessTools.Field(typeof(VRRig), "fps");
	private static readonly Dictionary<string, AccountRecord> Accounts = new Dictionary<string, AccountRecord>();

	private const float RigCacheSeconds = 0.75f;

	private static readonly List<VRRig> RigCache = new List<VRRig>();
	private static float rigCacheTime = -10f;

	public static IEnumerable<VRRig> RemoteRigs()
	{
		if (Time.unscaledTime - rigCacheTime > RigCacheSeconds)
		{
			rigCacheTime = Time.unscaledTime;
			RigCache.Clear();
			foreach (VRRig rig in UnityEngine.Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None))
			{
				if (rig != null && !rig.isLocal && !rig.isOfflineVRRig)
				{
					RigCache.Add(rig);
				}
			}
		}

		for (int i = 0; i < RigCache.Count; i++)
		{
			VRRig rig = RigCache[i];
			if (rig != null)
			{
				yield return rig;
			}
		}
	}

	public static Dictionary<object, object> Properties(VRRig rig)
	{
		if (rig == null || rig.Creator == null)
		{
			return null;
		}

		Player player = rig.Creator.GetPlayerRef();
		return player == null ? null : player.CustomProperties;
	}

	public static string LocalName()
	{
		VRRig rig = VRRig.LocalRig;
		return rig != null && rig.Creator != null ? rig.Creator.NickName : Unknown;
	}

	public static string DisplayName(VRRig rig)
	{
		if (rig == null)
		{
			return Unknown;
		}

		if (!string.IsNullOrWhiteSpace(rig.playerNameVisible))
		{
			return rig.playerNameVisible;
		}

		return rig.Creator != null && !string.IsNullOrWhiteSpace(rig.Creator.NickName) ? rig.Creator.NickName : "PLAYER";
	}

	public static string UserId(VRRig rig)
	{
		return rig != null && rig.Creator != null && rig.Creator.UserId != null ? rig.Creator.UserId : Unknown;
	}

	public static string StableKey(VRRig rig)
	{
		string userId = UserId(rig);
		return userId == Unknown ? rig.GetInstanceID().ToString() : userId;
	}

	public static string ColorMarkup(VRRig rig)
	{
		if (rig == null)
		{
			return "-";
		}

		Color color = rig.playerColor;
		string code = Mathf.RoundToInt(color.r * 9f) + " " + Mathf.RoundToInt(color.g * 9f) + " " + Mathf.RoundToInt(color.b * 9f);
		return "<mark=#" + ColorUtility.ToHtmlStringRGB(color) + "FF>    </mark> <b>" + code + "</b>";
	}

	public static int Fps(VRRig rig)
	{
		if (rig == null || FpsField == null)
		{
			return 0;
		}

		object value = FpsField.GetValue(rig);
		return value is int fps && fps > 0 ? fps : 0;
	}

	public static string FpsText(VRRig rig)
	{
		int fps = Fps(rig);
		return fps > 0 ? fps.ToString() : "N/A";
	}

	public static string Platform(VRRig rig)
	{
		return PlayerConnectionInfo.GetPlatform(rig);
	}

	public static string Headset(VRRig rig)
	{
		return PlayerConnectionInfo.GetHeadset(rig);
	}

	public static AccountAge GetAccountAge(VRRig rig)
	{
		if (rig == null || rig.Creator == null || string.IsNullOrEmpty(rig.Creator.UserId))
		{
			return new AccountAge(AccountStatus.Unknown, DateTime.MinValue);
		}

		string userId = rig.Creator.UserId;
		if (Accounts.TryGetValue(userId, out AccountRecord record))
		{
			bool retry = record.Status == AccountStatus.Failed && Time.realtimeSinceStartup - record.FailedAt > AccountRetrySeconds;
			if (!retry)
			{
				return new AccountAge(record.Status, record.CreatedUtc);
			}
		}

		record = new AccountRecord { Status = AccountStatus.Loading };
		Accounts[userId] = record;
		RequestAccountInfo(userId, record);
		return new AccountAge(AccountStatus.Loading, DateTime.MinValue);
	}

	private static void RequestAccountInfo(string userId, AccountRecord record)
	{
		GetAccountInfoRequest request = new GetAccountInfoRequest { PlayFabId = userId };
		PlayFabClientAPI.GetAccountInfo(
			request,
			result =>
			{
				record.CreatedUtc = result.AccountInfo.Created.ToUniversalTime();
				record.Status = AccountStatus.Ready;
			},
			_ =>
			{
				record.Status = AccountStatus.Failed;
				record.FailedAt = Time.realtimeSinceStartup;
			});
	}

	private sealed class AccountRecord
	{
		public AccountStatus Status;
		public DateTime CreatedUtc;
		public float FailedAt;
	}
}

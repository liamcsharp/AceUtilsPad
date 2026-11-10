using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace AUP;

public sealed class PresenceService : MonoBehaviour
{
	private const float PublishInterval = 2f;

	private TagSettings tags;
	private float nextPublishTime;

	public void Initialize(TagSettings tagSettings)
	{
		tags = tagSettings;
	}

	private void Update()
	{
		Player local = PhotonNetwork.LocalPlayer;
		if (Time.unscaledTime < nextPublishTime || !PhotonNetwork.InRoom || local == null)
		{
			return;
		}

		nextPublishTime = Time.unscaledTime + PublishInterval;
		Hashtable changes = new Hashtable();
		if (!Matches(local, PluginInfo.PresenceKey, PluginInfo.PresenceKey))
		{
			changes[PluginInfo.PresenceKey] = PluginInfo.PresenceKey;
		}

		string tag = tags == null ? null : PlayerTags.Encode(tags.SelfTag.Value, tags.SelfTagColor.Value);
		if (!Matches(local, PlayerTags.PropertyKey, tag))
		{
			changes[PlayerTags.PropertyKey] = tag;
		}

		if (changes.Count > 0)
		{
			local.SetCustomProperties(changes);
		}
	}

	private static bool Matches(Player player, string key, string expected)
	{
		object value = null;
		bool present = player.CustomProperties != null && player.CustomProperties.TryGetValue(key, out value);
		if (expected == null)
		{
			return !present || value == null;
		}

		return present && string.Equals(value as string, expected, StringComparison.Ordinal);
	}

	public static void Withdraw()
	{
		Player local = PhotonNetwork.LocalPlayer;
		if (!PhotonNetwork.InRoom || local == null)
		{
			return;
		}

		local.SetCustomProperties(new Hashtable
		{
			{ PluginInfo.PresenceKey, null },
			{ PlayerTags.PropertyKey, null },
			{ "AceUtilsPad.MenuVisible", null },
			{ "AceUtilsPad.MenuPage", null },
			{ "AceUtilsPad.MenuContent", null }
		});
	}
}

using System.Collections.Generic;
using UnityEngine;

namespace AUP;

public sealed class RemotePlayerMute : MonoBehaviour
{
	private const float RefreshInterval = 0.5f;

	private readonly Dictionary<AudioSource, bool> previousMuteStates = new Dictionary<AudioSource, bool>();
	private VRRig rig;
	private string ownerId = PlayerInfo.Unknown;
	private float nextRefreshTime;

	public void Bind(VRRig target)
	{
		rig = target;
		ownerId = PlayerInfo.UserId(target);
	}

	private void Update()
	{
		if (Time.unscaledTime < nextRefreshTime)
		{
			return;
		}

		nextRefreshTime = Time.unscaledTime + RefreshInterval;
		if (OwnerChanged())
		{
			RestoreAudio();
			Destroy(this);
			return;
		}

		ApplyMute();
	}

	private bool OwnerChanged()
	{
		string current = PlayerInfo.UserId(rig);
		if (ownerId == PlayerInfo.Unknown)
		{
			ownerId = current;
			return false;
		}

		return current != ownerId;
	}

	public void ApplyMute()
	{
		foreach (AudioSource source in GetComponentsInChildren<AudioSource>(true))
		{
			if (!previousMuteStates.ContainsKey(source))
			{
				previousMuteStates.Add(source, source.mute);
			}

			source.mute = true;
		}
	}

	public void RestoreAudio()
	{
		foreach (KeyValuePair<AudioSource, bool> state in previousMuteStates)
		{
			if (state.Key != null)
			{
				state.Key.mute = state.Value;
			}
		}

		previousMuteStates.Clear();
	}

	private void OnDestroy()
	{
		RestoreAudio();
	}
}

public static class PlayerMuting
{
	private const float CountInterval = 1f;

	private static float nextCountTime;
	private static int cachedCount;

	public static bool IsMuted(VRRig rig)
	{
		return rig != null && rig.GetComponent<RemotePlayerMute>() != null;
	}

	public static bool CanMute(VRRig rig)
	{
		return rig != null && !rig.isLocal && rig != VRRig.LocalRig;
	}

	public static void Toggle(VRRig rig)
	{
		if (IsMuted(rig))
		{
			Unmute(rig);
		}
		else
		{
			Mute(rig);
		}
	}

	public static bool Mute(VRRig rig)
	{
		if (!CanMute(rig) || IsMuted(rig))
		{
			return false;
		}

		RemotePlayerMute mute = rig.gameObject.AddComponent<RemotePlayerMute>();
		mute.Bind(rig);
		mute.ApplyMute();
		return true;
	}

	public static bool Unmute(VRRig rig)
	{
		RemotePlayerMute mute = rig == null ? null : rig.GetComponent<RemotePlayerMute>();
		if (mute == null)
		{
			return false;
		}

		mute.RestoreAudio();
		Object.Destroy(mute);
		return true;
	}

	public static int SetAll(bool muted)
	{
		int changed = 0;
		foreach (VRRig rig in PlayerInfo.RemoteRigs())
		{
			if (muted ? Mute(rig) : Unmute(rig))
			{
				changed++;
			}
		}

		return changed;
	}

	public static int MutedCount()
	{
		if (Time.unscaledTime >= nextCountTime)
		{
			nextCountTime = Time.unscaledTime + CountInterval;
			cachedCount = Object.FindObjectsByType<RemotePlayerMute>(FindObjectsSortMode.None).Length;
		}

		return cachedCount;
	}
}

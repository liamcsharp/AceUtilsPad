using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace AUP;

public static class LatencyTracker
{
	private const float SmoothingFactor = 0.15f;
	private const float MaxSampleSeconds = 3f;
	private const float SampleLifetimeSeconds = 4f;
	private const int PruneInterval = 400;

	private static readonly Dictionary<int, Sample> Samples = new Dictionary<int, Sample>();
	private static int recordCount;

	public static bool Installed { get; private set; }

	public static bool Install(Harmony harmony)
	{
		MethodInfo target = AccessTools.Method(typeof(VRRig), "AddVelocityToQueue", new[] { typeof(Vector3), typeof(double) });
		if (target == null)
		{
			return false;
		}

		HarmonyMethod postfix = new HarmonyMethod(typeof(LatencyTracker), nameof(OnRigUpdated));
		harmony.Patch(target, null, postfix);
		Installed = true;
		return true;
	}

	public static int? GetPingMs(VRRig rig)
	{
		if (rig == null || !Samples.TryGetValue(rig.GetInstanceID(), out Sample sample))
		{
			return null;
		}

		if (Time.unscaledTime - sample.UpdatedAt > SampleLifetimeSeconds)
		{
			return null;
		}

		float localPing = PhotonNetwork.IsConnected ? PhotonNetwork.GetPing() : 0f;
		return Mathf.Max(1, Mathf.RoundToInt(2f * sample.OneWayMs - localPing));
	}

	private static void OnRigUpdated(VRRig __instance, double serverTime)
	{
		if (__instance == null || __instance.isLocal || NetworkSystem.Instance == null)
		{
			return;
		}

		double seconds = NetworkSystem.Instance.SimTime - serverTime;
		if (seconds < -0.05 || seconds > MaxSampleSeconds)
		{
			return;
		}

		float oneWayMs = (float)Math.Max(0.0, seconds) * 1000f;
		int id = __instance.GetInstanceID();
		if (Samples.TryGetValue(id, out Sample sample))
		{
			sample.OneWayMs = Mathf.Lerp(sample.OneWayMs, oneWayMs, SmoothingFactor);
			sample.UpdatedAt = Time.unscaledTime;
		}
		else
		{
			Samples[id] = new Sample { OneWayMs = oneWayMs, UpdatedAt = Time.unscaledTime };
		}

		recordCount++;
		if (recordCount % PruneInterval == 0)
		{
			Prune();
		}
	}

	private static void Prune()
	{
		List<int> expired = new List<int>();
		foreach (KeyValuePair<int, Sample> pair in Samples)
		{
			if (Time.unscaledTime - pair.Value.UpdatedAt > SampleLifetimeSeconds * 4f)
			{
				expired.Add(pair.Key);
			}
		}

		foreach (int id in expired)
		{
			Samples.Remove(id);
		}
	}

	private sealed class Sample
	{
		public float OneWayMs;
		public float UpdatedAt;
	}
}

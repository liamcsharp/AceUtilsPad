using GorillaLocomotion;
using UnityEngine;

namespace AUP;

public enum HandSide
{
	Left,
	Right
}

public static class Hands
{
	private const string LeftPath = "rig/body/shoulder.L/upper_arm.L/forearm.L/hand.L";
	private const string RightPath = "rig/body/shoulder.R/upper_arm.R/forearm.R/hand.R";

	private const float LookupRetrySeconds = 1f;

	private static Transform cachedLeft;
	private static Transform cachedRight;
	private static float nextLeftLookup;
	private static float nextRightLookup;

	public static Transform Local(HandSide side)
	{
		GTPlayer player = GTPlayer.Instance;
		if (player == null)
		{
			return null;
		}

		if (side == HandSide.Left)
		{
			Resolve(player.transform, side, ref cachedLeft, ref nextLeftLookup);
			return cachedLeft;
		}

		Resolve(player.transform, side, ref cachedRight, ref nextRightLookup);
		return cachedRight;
	}

	private static void Resolve(Transform root, HandSide side, ref Transform cache, ref float nextLookup)
	{
		if (cache != null || Time.unscaledTime < nextLookup)
		{
			return;
		}

		cache = Find(root, side);
		nextLookup = Time.unscaledTime + LookupRetrySeconds;
	}

	public static Transform Find(Transform root, HandSide side)
	{
		if (root == null)
		{
			return null;
		}

		Transform hand = root.Find(side == HandSide.Left ? LeftPath : RightPath);
		if (hand != null)
		{
			return hand;
		}

		foreach (Transform child in root.GetComponentsInChildren<Transform>())
		{
			if (IsHandName(child.name.ToLowerInvariant(), side))
			{
				return child;
			}
		}

		return null;
	}

	private static bool IsHandName(string name, HandSide side)
	{
		string word = side == HandSide.Left ? "left" : "right";
		string suffix = side == HandSide.Left ? "hand.l" : "hand.r";
		return name.Contains(word + "hand") || name.Contains(word + "_hand") || name == suffix || name == word + " hand";
	}
}

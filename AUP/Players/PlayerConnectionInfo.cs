using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace AUP;

public static class PlayerConnectionInfo
{
	private static readonly string[] PlatformKeys = { "platform", "Platform", "device", "Device" };

	private static readonly HashSet<string> DeviceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"platform", "device", "headset", "headsettype", "headsetmodel", "vrheadset",
		"devicemodel", "model", "xrdevice", "hmd", "hardware"
	};

	private static readonly FieldInfo RankedTierField = AccessTools.Field(typeof(VRRig), "currentRankedSubTierPC");
	private static readonly MethodInfo HasCosmeticMethod = AccessTools.Method(typeof(VRRig), "HasCosmetic", new[] { typeof(string) });

	public static string GetPlatform(VRRig rig)
	{
		if (rig == null)
		{
			return PlayerInfo.Unknown;
		}

		try
		{
			string classified = Classify(rig.Creator == null ? null : rig.Creator.Platform);
			if (classified != null)
			{
				return classified;
			}

			Dictionary<object, object> properties = PlayerInfo.Properties(rig);
			if (properties != null)
			{
				foreach (string key in PlatformKeys)
				{
					if (properties.TryGetValue(key, out object value))
					{
						classified = Classify(value as string);
						if (classified != null)
						{
							return classified;
						}
					}
				}
			}

			if (HasCosmetic(rig, "S. FIRST LOGIN"))
			{
				return "Steam";
			}

			int propertyCount = properties == null ? 0 : properties.Count;
			object tier = RankedTierField == null ? null : RankedTierField.GetValue(rig);
			int rankedTier = tier is int value2 ? value2 : 0;
			if (HasCosmetic(rig, "FIRST LOGIN") && propertyCount >= 2 && rankedTier > 0)
			{
				return "Steam";
			}
		}
		catch (Exception)
		{
			return "Oculus";
		}

		return "Oculus";
	}

	public static string GetHeadset(VRRig rig)
	{
		Dictionary<object, object> properties = PlayerInfo.Properties(rig);
		if (properties == null)
		{
			return PlayerInfo.Unknown;
		}

		foreach (KeyValuePair<object, object> entry in properties)
		{
			string key = entry.Key as string;
			string description = entry.Value == null ? null : entry.Value.ToString();
			if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(description) || !DeviceKeys.Contains(key))
			{
				continue;
			}

			string headset = HeadsetModelDetector.Detect(description);
			if (headset != HeadsetModelDetector.Unknown)
			{
				return headset;
			}
		}

		return PlayerInfo.Unknown;
	}

	private static string Classify(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}

		if (text.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "Steam";
		}

		if (text.IndexOf("meta", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "Meta";
		}

		bool oculus = text.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0
					  || text.IndexOf("quest", StringComparison.OrdinalIgnoreCase) >= 0;
		return oculus ? "Oculus" : null;
	}

	private static bool HasCosmetic(VRRig rig, string cosmeticName)
	{
		if (HasCosmeticMethod == null)
		{
			return false;
		}

		object result = HasCosmeticMethod.Invoke(rig, new object[] { cosmeticName });
		return result is bool has && has;
	}
}

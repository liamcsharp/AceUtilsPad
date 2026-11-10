using System;

namespace AUP;

public static class PlatformNames
{
	public static bool IsPc(string platform)
	{
		return !string.IsNullOrEmpty(platform) && platform.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	public static bool IsQuest(string platform)
	{
		return !string.IsNullOrEmpty(platform)
			   && (platform.IndexOf("meta", StringComparison.OrdinalIgnoreCase) >= 0
				   || platform.IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0
				   || platform.IndexOf("quest", StringComparison.OrdinalIgnoreCase) >= 0);
	}

	public static string Friendly(string platform)
	{
		if (IsPc(platform))
		{
			return "PC";
		}

		return IsQuest(platform) ? "Quest" : "Unknown";
	}

	public static string IconId(string platform)
	{
		if (IsPc(platform))
		{
			return "pc";
		}

		return IsQuest(platform) ? "quest" : null;
	}
}

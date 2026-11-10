using UnityEngine;

namespace AUP;

public static class TagPalette
{
	public static readonly Color[] NameTints =
	{
		Color.white,
		new Color(0.45f, 0.78f, 1f, 1f),
		new Color(1f, 0.55f, 0.35f, 1f),
		new Color(0.7f, 1f, 0.5f, 1f)
	};

	public static readonly Color[] SelfTagColors =
	{
		new Color(1f, 0.47f, 0.13f, 1f),
		new Color(1f, 0.55f, 0.78f, 1f),
		new Color(0.3f, 0.9f, 1f, 1f),
		new Color(0.55f, 0.92f, 0.35f, 1f),
		new Color(0.7f, 0.5f, 1f, 1f),
		new Color(1f, 0.82f, 0.3f, 1f),
		new Color(0.95f, 0.95f, 0.97f, 1f),
		new Color(1f, 0.33f, 0.33f, 1f)
	};

	public static readonly string[] SelfTagNames = { "Lava", "Sakura", "Neon", "Moss", "Crystal", "Gold", "Cloud", "Ruby" };

	public static readonly Color Neutral = new Color(0.86f, 0.88f, 0.95f, 1f);
	public static readonly Color Unknown = new Color(0.55f, 0.58f, 0.66f, 1f);
	public static readonly Color Star = new Color(1f, 0.83f, 0.25f, 1f);
	public static readonly Color PcIcon = new Color(0.45f, 0.78f, 1f, 1f);
	public static readonly Color QuestIcon = new Color(0.5f, 0.93f, 0.6f, 1f);
	public static readonly Color NoteText = new Color(0.82f, 0.85f, 0.95f, 1f);
	public static readonly Color Separator = new Color(0.55f, 0.58f, 0.7f, 1f);
	public static readonly Color NewBadge = new Color(1f, 0.85f, 0.29f, 1f);

	private static readonly Color Good = new Color(0.25f, 1f, 0.35f, 1f);
	private static readonly Color Fair = new Color(1f, 0.6f, 0.15f, 1f);
	private static readonly Color Poor = new Color(1f, 0.28f, 0.28f, 1f);
	private static readonly Color Steam = new Color(0.4f, 0.75f, 1f, 1f);
	private static readonly Color Quest = new Color(0.42f, 0.9f, 0.52f, 1f);

	public static int SelfTagIndex(int index)
	{
		return Mathf.Clamp(index, 0, SelfTagColors.Length - 1);
	}

	public static Color SelfTag(int index)
	{
		return SelfTagColors[SelfTagIndex(index)];
	}

	public static Color PingDot(int milliseconds)
	{
		return milliseconds < 0 ? Unknown : Ping(milliseconds);
	}

	public static Color PlatformIcon(string platform)
	{
		if (PlatformNames.IsPc(platform))
		{
			return PcIcon;
		}

		return PlatformNames.IsQuest(platform) ? QuestIcon : Unknown;
	}

	public static Color Tint(int index)
	{
		return NameTints[Mathf.Clamp(index, 0, NameTints.Length - 1)];
	}

	public static Color Fps(int fps)
	{
		if (fps >= 87)
		{
			return Good;
		}

		return fps >= 50 ? Fair : Poor;
	}

	public static Color Ping(int milliseconds)
	{
		if (milliseconds < 60)
		{
			return Good;
		}

		return milliseconds < 120 ? Fair : Poor;
	}

	public static Color Platform(string platform)
	{
		if (string.IsNullOrEmpty(platform))
		{
			return Neutral;
		}

		if (platform.IndexOf("steam", System.StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return Steam;
		}

		bool quest = platform.IndexOf("meta", System.StringComparison.OrdinalIgnoreCase) >= 0
					 || platform.IndexOf("oculus", System.StringComparison.OrdinalIgnoreCase) >= 0;
		return quest ? Quest : Neutral;
	}

	public static Color Age(AccountAge age, int newAccountDays)
	{
		if (!age.IsReady)
		{
			return Neutral;
		}

		if (age.Days < newAccountDays)
		{
			return Poor;
		}

		return age.Days < newAccountDays * 3 ? Fair : Good;
	}
}

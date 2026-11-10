using System;
using System.Collections.Generic;

namespace AUP;

public static class PlayerTags
{
	public const string PropertyKey = "AceUtilsPad.Tag";
	public const int MaxLength = TextEntry.MaxLength;

	public static string Encode(string text, int color)
	{
		string cleaned = Clean(text);
		return cleaned.Length == 0 ? null : TagPalette.SelfTagIndex(color) + "|" + cleaned;
	}

	public static bool TryRead(VRRig rig, out string text, out int color)
	{
		text = null;
		color = 0;
		Dictionary<object, object> properties = PlayerInfo.Properties(rig);
		if (properties == null || !properties.TryGetValue(PropertyKey, out object value) || !(value is string raw))
		{
			return false;
		}

		int split = raw.IndexOf('|');
		if (split <= 0 || split >= raw.Length - 1 || !int.TryParse(raw.Substring(0, split), out color))
		{
			return false;
		}

		color = TagPalette.SelfTagIndex(color);
		text = Clean(raw.Substring(split + 1));
		return text.Length > 0;
	}

	public static string Clean(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		char[] kept = new char[Math.Min(text.Length, MaxLength)];
		int count = 0;
		foreach (char character in text)
		{
			if (count >= kept.Length)
			{
				break;
			}

			if (!char.IsControl(character) && character != '<' && character != '>' && character != '|')
			{
				kept[count++] = character;
			}
		}

		return new string(kept, 0, count).Trim();
	}
}

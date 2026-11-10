using System;
using System.Text.RegularExpressions;

namespace AUP;

public enum KillSwitchState
{
	Unknown,
	Alive,
	Dead
}

public static class KillSwitchParser
{
	private static readonly Regex AliveKey = new Regex(
		"\"(?:is_online|isOnline|online|enabled|active|alive|live|running|allowed)\"\\s*:\\s*(?<value>true|false|\"true\"|\"false\"|1|0)(?![A-Za-z0-9_])",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex DeadKey = new Regex(
		"\"(?:killed|disabled|shutdown|shut_down|kill|stopped|offline)\"\\s*:\\s*(?<value>true|false|\"true\"|\"false\"|1|0)(?![A-Za-z0-9_])",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex PlainKey = new Regex(
		"^(?:online|enabled|active|alive|live|status)\\s*[:=]\\s*(?<value>true|false|on|off|online|offline|1|0)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly string[] AliveWords = { "true", "online", "on", "enabled", "alive", "live", "ok", "1" };
	private static readonly string[] DeadWords = { "false", "offline", "off", "disabled", "killed", "kill", "shutdown", "dead", "0" };

	public static KillSwitchState Parse(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return KillSwitchState.Unknown;
		}

		string text = body.Trim();
		if (text.StartsWith("<", StringComparison.Ordinal))
		{
			return KillSwitchState.Unknown;
		}

		Match alive = AliveKey.Match(text);
		if (alive.Success)
		{
			return IsTruthy(alive.Groups["value"].Value) ? KillSwitchState.Alive : KillSwitchState.Dead;
		}

		Match dead = DeadKey.Match(text);
		if (dead.Success)
		{
			return IsTruthy(dead.Groups["value"].Value) ? KillSwitchState.Dead : KillSwitchState.Alive;
		}

		return ParsePlain(text.Trim('"').Trim());
	}

	private static KillSwitchState ParsePlain(string text)
	{
		Match plain = PlainKey.Match(text);
		string word = plain.Success ? plain.Groups["value"].Value : text;
		if (Array.IndexOf(AliveWords, word.ToLowerInvariant()) >= 0)
		{
			return KillSwitchState.Alive;
		}

		return Array.IndexOf(DeadWords, word.ToLowerInvariant()) >= 0 ? KillSwitchState.Dead : KillSwitchState.Unknown;
	}

	private static bool IsTruthy(string value)
	{
		string normalized = value.Trim('"').ToLowerInvariant();
		return normalized == "true" || normalized == "1" || normalized == "on" || normalized == "online";
	}
}


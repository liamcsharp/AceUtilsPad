using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AUP;

public enum DetectionKind
{
	Cheat,
	Mod
}

public enum BanRisk
{
	Low,
	PrivateOnly,
	High
}

public sealed class DetectionResult
{
	public DetectionResult(DetectionKind kind, string name)
	{
		Kind = kind;
		Name = name;
		Risk = BanRiskInfo.Classify(kind, name);
	}

	public DetectionKind Kind { get; }
	public string Name { get; }
	public BanRisk Risk { get; }
	public string Signature => Kind + ":" + Name;
}

public static class BanRiskInfo
{
	public const string HighColor = "#FF5B5B";
	public const string PrivateColor = "#FFB347";
	public const string LowColor = "#65E985";

	private static readonly string[] PrivateLobbyWords =
	{
		"fly", "speed", "platform", "ghost", "invis", "noclip", "no clip", "teleport", "tp ", "long arm", "longarm",
		"grav", "wall walk", "wallwalk", "spider", "jump", "slide", "strafe", "freeze", "tag gun", "tagall", "tag all",
		"iron monk", "rocket", "jetpack", "swim", "size", "scale", "physics", "movement"
	};

	public static BanRisk Classify(DetectionKind kind, string name)
	{
		if (kind == DetectionKind.Cheat)
		{
			return BanRisk.High;
		}

		string lower = (name ?? string.Empty).ToLowerInvariant();
		foreach (string word in PrivateLobbyWords)
		{
			if (lower.Contains(word))
			{
				return BanRisk.PrivateOnly;
			}
		}

		return BanRisk.Low;
	}

	public static string Label(BanRisk risk)
	{
		switch (risk)
		{
			case BanRisk.High:
				return "Known to cause bans";
			case BanRisk.PrivateOnly:
				return "Private or modded lobbies only";
			default:
				return "Low risk, cosmetic or info";
		}
	}

	public static string Short(BanRisk risk)
	{
		switch (risk)
		{
			case BanRisk.High:
				return "BAN RISK";
			case BanRisk.PrivateOnly:
				return "PRIVATE ONLY";
			default:
				return "LOW RISK";
		}
	}

	public static string Color(BanRisk risk)
	{
		switch (risk)
		{
			case BanRisk.High:
				return HighColor;
			case BanRisk.PrivateOnly:
				return PrivateColor;
			default:
				return LowColor;
		}
	}

	public static string Explain(BanRisk risk)
	{
		switch (risk)
		{
			case BanRisk.High:
				return "Listed as a cheat. Using it in public lobbies is very likely to get the account banned.";
			case BanRisk.PrivateOnly:
				return "Changes movement or gameplay. Only safe in private or modded lobbies; using it in public can get you banned.";
			default:
				return "Looks cosmetic or informational. These rarely lead to bans, but no mod is ever fully safe.";
		}
	}

	public static BanRisk Worst(List<DetectionResult> detections)
	{
		BanRisk worst = BanRisk.Low;
		foreach (DetectionResult detection in detections)
		{
			if (detection.Risk > worst)
			{
				worst = detection.Risk;
			}
		}

		return worst;
	}
}

public static class DetectionService
{
	public const string CheatColor = "#FF5B5B";
	public const string ModColor = "#65E985";

	private const string CheatsListUrl = "https://aceutils.org/data/knowncheats.txt";
	private const string ModsListUrl = "https://aceutils.org/data/knownmods.txt";

	private static readonly object Gate = new object();
	private static readonly HashSet<string> CheatKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static readonly string[] SeedKeys = { "AceUtilsPad", PluginInfo.PresenceKey };

	private static readonly Dictionary<string, string> KnownKeys = CreateSeededKeys();

	private static readonly Regex EntryPattern = new Regex(
		"^\\s*\"(?<key>(?:\\\\.|[^\"])*)\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"\\s*,?\\s*$",
		RegexOptions.Compiled | RegexOptions.Multiline);

	private static string summary = "Lists not loaded yet.";
	private static volatile bool loading;

	public static bool Loading => loading;

	public static string Summary
	{
		get
		{
			lock (Gate)
			{
				return summary;
			}
		}
	}

	public static void RefreshLists()
	{
		loading = true;
		Task.Run(DownloadLists);
	}

	public static string ColorFor(DetectionKind kind)
	{
		return kind == DetectionKind.Cheat ? CheatColor : ModColor;
	}

	public static List<DetectionResult> Detect(VRRig rig)
	{
		List<DetectionResult> results = new List<DetectionResult>();
		Dictionary<object, object> properties = PlayerInfo.Properties(rig);
		if (properties == null)
		{
			return results;
		}

		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		lock (Gate)
		{
			foreach (KeyValuePair<object, object> entry in properties)
			{
				string key = entry.Key == null ? null : entry.Key.ToString();
				if (key == null || !KnownKeys.TryGetValue(key, out string name))
				{
					continue;
				}

				DetectionKind kind = CheatKeys.Contains(key) ? DetectionKind.Cheat : DetectionKind.Mod;
				DetectionResult result = new DetectionResult(kind, name);
				if (seen.Add(result.Signature))
				{
					results.Add(result);
				}
			}
		}

		return results;
	}

	public static string Signature(IEnumerable<DetectionResult> detections)
	{
		List<string> parts = new List<string>();
		foreach (DetectionResult detection in detections)
		{
			parts.Add(detection.Signature);
		}

		parts.Sort(StringComparer.Ordinal);
		return string.Join("|", parts.ToArray());
	}

	private static void DownloadLists()
	{
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			using (WebClient client = new WebClient())
			{
				client.Headers[HttpRequestHeader.UserAgent] = PluginInfo.Name + "/" + PluginInfo.Version;
				Dictionary<string, string> cheats = ParseEntries(client.DownloadString(CheatsListUrl));
				Dictionary<string, string> mods = ParseEntries(client.DownloadString(ModsListUrl));
				ApplyLists(cheats, mods);
				SetSummary("Lists loaded: " + cheats.Count + " cheats, " + mods.Count + " mods.");
			}
		}
		catch (Exception exception)
		{
			SetSummary("List download failed: " + exception.Message);
		}
		finally
		{
			loading = false;
		}
	}

	private static Dictionary<string, string> CreateSeededKeys()
	{
		Dictionary<string, string> keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string seed in SeedKeys)
		{
			keys[seed] = "AUP";
		}

		return keys;
	}

	private static Dictionary<string, string> ParseEntries(string contents)
	{
		Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match match in EntryPattern.Matches(contents ?? string.Empty))
		{
			string key = Regex.Unescape(match.Groups["key"].Value);
			string value = Regex.Unescape(match.Groups["value"].Value);
			if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
			{
				entries[key] = value;
			}
		}

		return entries;
	}

	private static void ApplyLists(Dictionary<string, string> cheats, Dictionary<string, string> mods)
	{
		lock (Gate)
		{
			KnownKeys.Clear();
			foreach (string seed in SeedKeys)
			{
				KnownKeys[seed] = "AUP";
			}

			CheatKeys.Clear();
			foreach (KeyValuePair<string, string> cheat in cheats)
			{
				KnownKeys[cheat.Key] = cheat.Value;
				CheatKeys.Add(cheat.Key);
			}

			foreach (KeyValuePair<string, string> mod in mods)
			{
				KnownKeys[mod.Key] = mod.Value;
			}
		}
	}

	private static void SetSummary(string value)
	{
		lock (Gate)
		{
			summary = value;
		}
	}
}

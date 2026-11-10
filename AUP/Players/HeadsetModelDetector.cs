using System;

namespace AUP;

public static class HeadsetModelDetector
{
	public const string Unknown = "Unknown";

	private static readonly Rule[] Rules =
	{
		new Rule("Meta Quest Pro", "quest pro", "questpro"),
		new Rule("Meta Quest 3S", "quest 3s", "quest3s"),
		new Rule("Meta Quest 3", "quest 3", "quest3"),
		new Rule("Meta Quest 2", "quest 2", "quest2"),
		new Rule("Meta Quest", "quest", "oculus quest"),
		new Rule("Oculus Rift", "rift s", "oculus rift"),
		new Rule("Valve Index", "valve index"),
		new Rule("Steam Frame", "steam frame", "steamframe"),
		new Rule("Pico", "pico"),
		new Rule("HTC Vive", "htc vive", "vive"),
		new Rule("PlayStation VR2", "playstation vr2", "ps vr2", "psvr2"),
		new Rule("Apple Vision Pro", "vision pro"),
		new Rule("Windows Mixed Reality", "windows mixed reality", "windowsmr", "odyssey"),
		new Rule("Varjo", "varjo")
	};

	public static string Detect(string deviceDescription)
	{
		if (string.IsNullOrWhiteSpace(deviceDescription))
		{
			return Unknown;
		}

		string text = deviceDescription.Trim();
		foreach (Rule rule in Rules)
		{
			if (rule.Matches(text))
			{
				return rule.Result;
			}
		}

		return string.Equals(text, "index", StringComparison.OrdinalIgnoreCase) ? "Valve Index" : Unknown;
	}

	private sealed class Rule
	{
		private readonly string[] needles;

		public Rule(string result, params string[] needles)
		{
			Result = result;
			this.needles = needles;
		}

		public string Result { get; }

		public bool Matches(string text)
		{
			foreach (string needle in needles)
			{
				if (text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			return false;
		}
	}
}

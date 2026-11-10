using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace AUP.Widgets;

public enum WidgetKind
{
	Fps,
	Ping,
	Clock,
	Session,
	Players,
	Room,
	Battery,
	Feed,
	Camera,
	Performance,
	Launcher,
	Music,
	Compass,
	Text,
	Dock,
	Custom
}

public sealed class WidgetElement
{
	public int Type;
	public string Text = string.Empty;
	public int Bind;
}

public sealed class WidgetConfig
{
	public string Id = string.Empty;
	public int Kind;
	public string Title = string.Empty;
	public bool Enabled;
	public bool Locked;
	public bool Minimized;
	public bool Favorite;
	public int Anchor;
	public float X;
	public float Y;
	public float Z = 0.85f;
	public float Yaw;
	public float Pitch;
	public float Roll;
	public float ScreenX = 0.12f;
	public float ScreenY = 0.85f;
	public float Scale = 1f;
	public float Alpha = 0.9f;
	public int Background;
	public int TextColor;
	public int Font;
	public bool Square;
	public int Border = 1;
	public bool Shadow = true;
	public int Padding = 1;
	public int Visibility;
	public int Interval = 2;
	public int Animation = 2;
	public int Rule;
	public int Group;
	public string Text = "Your text";
	public List<WidgetElement> Elements = new List<WidgetElement>();

	[JsonIgnore]
	public WidgetKind KindValue => (WidgetKind)Mathf.Clamp(Kind, 0, (int)WidgetKind.Custom);
}

public sealed class WidgetLayout
{
	public string Name = string.Empty;
	public List<WidgetConfig> Widgets = new List<WidgetConfig>();
}

public sealed class WidgetProfile
{
	public int Version = 1;
	public bool Enabled = true;
	public int Active;
	public List<WidgetLayout> Layouts = new List<WidgetLayout>();
	public int[] StyleLayouts = { -1, -1, -1 };
	public float GlobalScale = 1f;
	public float GlobalAlpha = 1f;
	public float AnimationSpeed = 1f;
	public int UpdateRate = 1;
	public bool HideWithMenu = true;
	public bool LobbyOnly;
	public int DefaultSpot;
	public int ClickSound = 5;
	public int DisplayMode;
}

public static class WidgetOptions
{
	public static readonly string[] KindNames =
	{
		"FPS Monitor", "Ping Monitor", "Clock & Date", "Session Tracker", "Player Counter", "Room Info", "Controller Battery",
		"Notification Feed", "AceCam Status", "Performance", "Quick Launcher", "Music Controls", "Compass", "Custom Text", "Widget Dock", "Studio Widget"
	};

	public static readonly string[] KindIcons =
	{
		"bolt", "bolt", "sun", "heart", "group", "info", "bolt", "bell", "camera", "gear", "grid", "play", "up", "tag", "grid", "wrench"
	};

	public static readonly string[] Categories =
	{
		"stats", "stats", "info", "info", "lobby", "lobby", "stats", "info", "tools", "stats", "tools", "media", "info", "info", "tools", "studio"
	};

	public static readonly string[] LayoutNames = { "Minimal", "Competitive", "Cinematic", "Streaming", "Information", "Custom" };
	public static readonly string[] AnchorNames = { "Follows your view", "Stays in the world", "On your right hand" };
	public static readonly string[] VisibilityNames = { "Always", "Only in a lobby", "Only outside lobbies", "Only with menu closed", "Only with menu open" };
	public static readonly float[] Intervals = { 0.1f, 0.25f, 0.5f, 1f, 2f };
	public static readonly string[] AnimationNames = { "None", "Fade", "Pop", "Slide" };
	public static readonly string[] BorderNames = { "No border", "Thin border", "Thick border" };
	public static readonly float[] BorderSizes = { 0f, 1.5f, 3.5f };
	public static readonly string[] PaddingNames = { "Tight", "Normal", "Roomy" };
	public static readonly float[] Paddings = { 5f, 9f, 14f };
	public static readonly string[] GroupNames = { "No group", "Group A", "Group B", "Group C", "Group D" };
	public static readonly string[] RateNames = { "Battery saver", "Normal", "Fast" };
	public static readonly float[] RateFactors = { 2f, 1f, 0.5f };
	public static readonly string[] DisplayNames = { "Auto", "In the world", "On screen" };
	public static readonly string[] SpotNames = { "Top left", "Top right", "Bottom left", "Bottom right", "Center" };
	public static readonly string[] StyleNames = { "Wrist menu", "AceOne phone", "Orb menu" };

	public static readonly string[] BackgroundNames = { "Theme", "Black", "Slate", "Navy", "Wine", "Forest", "White", "Clear" };

	public static readonly Color[] Backgrounds =
	{
		Color.clear, new Color(0.03f, 0.03f, 0.04f), new Color(0.13f, 0.15f, 0.19f), new Color(0.05f, 0.08f, 0.2f), new Color(0.24f, 0.05f, 0.09f),
		new Color(0.05f, 0.16f, 0.08f), new Color(0.95f, 0.96f, 0.98f), new Color(0f, 0f, 0f, 0f)
	};

	public static readonly string[] TextColorNames = { "Theme", "White", "Black", "Accent", "Orange", "Cyan", "Pink", "Lime", "Gold" };

	public static readonly Color[] TextColors =
	{
		Color.clear, Color.white, new Color(0.06f, 0.06f, 0.08f), Color.clear, new Color(1f, 0.55f, 0.15f), new Color(0.35f, 0.92f, 1f),
		new Color(1f, 0.55f, 0.8f), new Color(0.6f, 0.95f, 0.35f), new Color(1f, 0.83f, 0.3f)
	};

	public static readonly string[] ElementTypes = { "Text label", "Info label", "Icon", "Button", "Slider", "Toggle", "Progress bar", "Panel", "Separator" };

	public static string[] RuleNames(WidgetKind kind)
	{
		switch (kind)
		{
			case WidgetKind.Fps:
				return new[] { "No alert", "Below 30 fps", "Below 45 fps", "Below 60 fps" };
			case WidgetKind.Ping:
				return new[] { "No alert", "Above 100 ms", "Above 150 ms", "Above 250 ms" };
			case WidgetKind.Battery:
				return new[] { "No alert", "Below 10%", "Below 20%", "Below 30%" };
			case WidgetKind.Players:
				return new[] { "No alert", "When the count changes" };
			case WidgetKind.Music:
				return new[] { "No alert", "When the song changes" };
			case WidgetKind.Room:
				return new[] { "No alert", "When you change rooms" };
			default:
				return new[] { "No alert" };
		}
	}

	public static int Wrap(int value, int count)
	{
		return count <= 0 ? 0 : (value % count + count) % count;
	}
}

public sealed class WidgetStore
{
	private const string CodePrefix = "ACEW1:";
	private const float SaveDelay = 1.5f;
	private const int MaxCustomWidgets = 8;

	private static readonly Vector2[] HeadSlots =
	{
		new Vector2(-0.38f, 0.25f), new Vector2(0.38f, 0.25f), new Vector2(-0.38f, -0.28f), new Vector2(0.38f, -0.28f),
		new Vector2(-0.13f, 0.27f), new Vector2(0.13f, 0.27f), new Vector2(-0.13f, -0.3f), new Vector2(0.13f, -0.3f),
		new Vector2(-0.52f, 0.08f), new Vector2(0.52f, 0.08f), new Vector2(-0.52f, -0.1f), new Vector2(0.52f, -0.1f),
		new Vector2(-0.52f, 0.25f), new Vector2(0.52f, 0.25f), new Vector2(0f, -0.38f), new Vector2(0f, 0.36f)
	};

	private static readonly Vector2[] ScreenSlots =
	{
		new Vector2(0.09f, 0.93f), new Vector2(0.5f, 0.95f), new Vector2(0.09f, 0.09f), new Vector2(0.5f, 0.07f),
		new Vector2(0.09f, 0.83f), new Vector2(0.3f, 0.95f), new Vector2(0.09f, 0.2f), new Vector2(0.3f, 0.07f),
		new Vector2(0.09f, 0.72f), new Vector2(0.09f, 0.6f), new Vector2(0.09f, 0.48f), new Vector2(0.09f, 0.36f),
		new Vector2(0.28f, 0.83f), new Vector2(0.28f, 0.72f), new Vector2(0.5f, 0.17f), new Vector2(0.7f, 0.95f)
	};

	private readonly string folder;
	private readonly string currentPath;
	private readonly string profileFolder;
	private float saveTime = -1f;

	public WidgetStore()
	{
		folder = Path.Combine(Paths.ConfigPath, "AceWidgets");
		currentPath = Path.Combine(folder, "current.json");
		profileFolder = Path.Combine(folder, "profiles");
		Profile = Load(currentPath) ?? CreateDefault();
		Repair(Profile);
	}

	public WidgetProfile Profile { get; private set; }

	public string LastMessage { get; private set; } = "Ready";

	public void MarkDirty()
	{
		saveTime = Time.unscaledTime + SaveDelay;
	}

	public void Tick()
	{
		if (saveTime > 0f && Time.unscaledTime >= saveTime)
		{
			saveTime = -1f;
			Write(currentPath, Profile);
		}
	}

	public void Flush()
	{
		if (saveTime > 0f)
		{
			saveTime = -1f;
			Write(currentPath, Profile);
		}
	}

	public List<string> SavedProfiles()
	{
		List<string> names = new List<string>();
		try
		{
			if (Directory.Exists(profileFolder))
			{
				foreach (string file in Directory.GetFiles(profileFolder, "*.json"))
				{
					names.Add(Path.GetFileNameWithoutExtension(file));
				}
			}
		}
		catch (Exception)
		{
		}

		names.Sort(StringComparer.OrdinalIgnoreCase);
		return names;
	}

	public void SaveProfile(string name)
	{
		string clean = CleanName(name);
		if (clean.Length == 0)
		{
			LastMessage = "Type a name first";
			return;
		}

		LastMessage = Write(Path.Combine(profileFolder, clean + ".json"), Profile) ? "Saved profile " + clean : "Could not save " + clean;
	}

	public bool LoadProfile(string name)
	{
		WidgetProfile loaded = Load(Path.Combine(profileFolder, CleanName(name) + ".json"));
		if (loaded == null)
		{
			LastMessage = "Could not load " + name;
			return false;
		}

		Repair(loaded);
		Profile = loaded;
		MarkDirty();
		LastMessage = "Loaded profile " + name;
		return true;
	}

	public void DeleteProfile(string name)
	{
		try
		{
			File.Delete(Path.Combine(profileFolder, CleanName(name) + ".json"));
			LastMessage = "Deleted profile " + name;
		}
		catch (Exception exception)
		{
			LastMessage = "Could not delete: " + exception.Message;
		}
	}

	public string ExportCode()
	{
		try
		{
			byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Profile, Formatting.None));
			using (MemoryStream output = new MemoryStream())
			{
				using (GZipStream zip = new GZipStream(output, CompressionMode.Compress, true))
				{
					zip.Write(json, 0, json.Length);
				}

				string code = CodePrefix + Convert.ToBase64String(output.ToArray());
				GUIUtility.systemCopyBuffer = code;
				LastMessage = "Share code copied to your clipboard";
				return code;
			}
		}
		catch (Exception exception)
		{
			LastMessage = "Could not make a share code: " + exception.Message;
			return null;
		}
	}

	public bool ImportCode()
	{
		try
		{
			string code = (GUIUtility.systemCopyBuffer ?? string.Empty).Trim();
			if (!code.StartsWith(CodePrefix, StringComparison.Ordinal))
			{
				LastMessage = "No AceWidgets share code is on your clipboard";
				return false;
			}

			byte[] packed = Convert.FromBase64String(code.Substring(CodePrefix.Length));
			if (packed.Length > 400000)
			{
				LastMessage = "That share code is too large";
				return false;
			}

			using (MemoryStream input = new MemoryStream(packed))
			using (GZipStream zip = new GZipStream(input, CompressionMode.Decompress))
			using (MemoryStream output = new MemoryStream())
			{
				byte[] buffer = new byte[8192];
				int read;
				while ((read = zip.Read(buffer, 0, buffer.Length)) > 0)
				{
					output.Write(buffer, 0, read);
					if (output.Length > 4000000)
					{
						LastMessage = "That share code is too large";
						return false;
					}
				}

				WidgetProfile loaded = JsonConvert.DeserializeObject<WidgetProfile>(Encoding.UTF8.GetString(output.ToArray()));
				if (loaded == null)
				{
					LastMessage = "That share code is empty";
					return false;
				}

				Repair(loaded);
				Profile = loaded;
				MarkDirty();
				LastMessage = "Share code imported";
				return true;
			}
		}
		catch (Exception)
		{
			LastMessage = "That share code could not be read";
			return false;
		}
	}

	public void ResetLayout(int index)
	{
		index = Mathf.Clamp(index, 0, Profile.Layouts.Count - 1);
		Profile.Layouts[index] = BuildLayout(index, Profile.DefaultSpot);
		MarkDirty();
	}

	public void ResetAll()
	{
		Profile = CreateDefault();
		MarkDirty();
		LastMessage = "All widgets reset";
	}

	public WidgetConfig AddCustom(WidgetLayout layout)
	{
		int count = 0;
		foreach (WidgetConfig widget in layout.Widgets)
		{
			if (widget.KindValue == WidgetKind.Custom)
			{
				count++;
			}
		}

		if (count >= MaxCustomWidgets)
		{
			LastMessage = "You can have up to " + MaxCustomWidgets + " studio widgets per layout";
			return null;
		}

		WidgetConfig created = NewWidget(WidgetKind.Custom, layout.Widgets.Count, Profile.DefaultSpot);
		created.Id = "custom-" + Guid.NewGuid().ToString("N").Substring(0, 8);
		created.Title = "My Widget " + (count + 1);
		created.Enabled = true;
		created.Elements.Add(new WidgetElement { Type = 0, Text = "Hello" });
		created.Elements.Add(new WidgetElement { Type = 1, Bind = 0 });
		layout.Widgets.Add(created);
		MarkDirty();
		return created;
	}

	public void PlaceAtSlot(WidgetConfig widget, int slot, int spot)
	{
		Vector2 head = HeadSlots[WidgetOptions.Wrap(slot, HeadSlots.Length)];
		Vector2 screen = ScreenSlots[WidgetOptions.Wrap(slot, ScreenSlots.Length)];
		switch (WidgetOptions.Wrap(spot, WidgetOptions.SpotNames.Length))
		{
			case 1:
				head.x = -head.x;
				screen.x = 1f - screen.x;
				break;
			case 2:
				head.y = -head.y;
				screen.y = 1f - screen.y;
				break;
			case 3:
				head = -head;
				screen = Vector2.one - screen;
				break;
			case 4:
				head *= 0.6f;
				screen = Vector2.Lerp(new Vector2(0.5f, 0.5f), screen, 0.6f);
				break;
		}

		widget.Anchor = 0;
		widget.X = head.x;
		widget.Y = head.y;
		widget.Z = 0.85f;
		widget.Yaw = 0f;
		widget.Pitch = 0f;
		widget.Roll = 0f;
		widget.ScreenX = screen.x;
		widget.ScreenY = screen.y;
	}

	private WidgetProfile CreateDefault()
	{
		WidgetProfile profile = new WidgetProfile();
		for (int i = 0; i < WidgetOptions.LayoutNames.Length; i++)
		{
			profile.Layouts.Add(BuildLayout(i, 0));
		}

		return profile;
	}

	private WidgetLayout BuildLayout(int index, int spot)
	{
		WidgetLayout layout = new WidgetLayout { Name = WidgetOptions.LayoutNames[WidgetOptions.Wrap(index, WidgetOptions.LayoutNames.Length)] };
		WidgetKind[] enabled;
		switch (index)
		{
			case 0:
				enabled = new[] { WidgetKind.Fps, WidgetKind.Clock };
				break;
			case 1:
				enabled = new[] { WidgetKind.Fps, WidgetKind.Ping, WidgetKind.Players, WidgetKind.Room, WidgetKind.Battery };
				break;
			case 2:
				enabled = new[] { WidgetKind.Camera, WidgetKind.Clock };
				break;
			case 3:
				enabled = new[] { WidgetKind.Clock, WidgetKind.Players, WidgetKind.Room, WidgetKind.Music, WidgetKind.Feed, WidgetKind.Text };
				break;
			case 4:
				enabled = new[]
				{
					WidgetKind.Fps, WidgetKind.Ping, WidgetKind.Clock, WidgetKind.Session, WidgetKind.Players, WidgetKind.Room, WidgetKind.Battery,
					WidgetKind.Performance, WidgetKind.Compass, WidgetKind.Feed
				};
				break;
			default:
				enabled = new[] { WidgetKind.Dock, WidgetKind.Launcher, WidgetKind.Music, WidgetKind.Fps };
				break;
		}

		int slot = 0;
		for (int kind = 0; kind < (int)WidgetKind.Custom; kind++)
		{
			WidgetConfig widget = NewWidget((WidgetKind)kind, 0, spot);
			widget.Enabled = Array.IndexOf(enabled, (WidgetKind)kind) >= 0;
			if (widget.Enabled)
			{
				PlaceAtSlot(widget, slot++, spot);
			}
			else
			{
				PlaceAtSlot(widget, 8 + kind, spot);
			}

			if (index == 2)
			{
				widget.Alpha = 0.6f;
				widget.Scale = 0.8f;
				widget.Border = 0;
			}
			else if (index == 0)
			{
				widget.Scale = 0.85f;
				widget.Border = 0;
			}
			else if (index == 3 && widget.KindValue == WidgetKind.Text)
			{
				widget.Text = "LIVE";
				widget.TextColor = 6;
			}

			widget.Favorite = kind == (int)WidgetKind.Fps || kind == (int)WidgetKind.Clock || kind == (int)WidgetKind.Music || kind == (int)WidgetKind.Players;
			layout.Widgets.Add(widget);
		}

		return layout;
	}

	private WidgetConfig NewWidget(WidgetKind kind, int slot, int spot)
	{
		WidgetConfig widget = new WidgetConfig
		{
			Id = kind == WidgetKind.Custom ? "custom" : "builtin-" + kind,
			Kind = (int)kind,
			Title = WidgetOptions.KindNames[(int)kind],
			Interval = kind == WidgetKind.Clock || kind == WidgetKind.Session || kind == WidgetKind.Room || kind == WidgetKind.Battery ? 3 : 2
		};
		PlaceAtSlot(widget, slot, spot);
		return widget;
	}

	private void Repair(WidgetProfile profile)
	{
		if (profile.Layouts == null)
		{
			profile.Layouts = new List<WidgetLayout>();
		}

		for (int i = profile.Layouts.Count; i < WidgetOptions.LayoutNames.Length; i++)
		{
			profile.Layouts.Add(BuildLayout(i, 0));
		}

		if (profile.StyleLayouts == null || profile.StyleLayouts.Length != 3)
		{
			profile.StyleLayouts = new[] { -1, -1, -1 };
		}

		profile.Active = Mathf.Clamp(profile.Active, 0, profile.Layouts.Count - 1);
		profile.GlobalScale = Mathf.Clamp(profile.GlobalScale, 0.5f, 2f);
		profile.GlobalAlpha = Mathf.Clamp(profile.GlobalAlpha, 0.2f, 1f);
		profile.AnimationSpeed = Mathf.Clamp(profile.AnimationSpeed, 0.25f, 3f);
		profile.UpdateRate = Mathf.Clamp(profile.UpdateRate, 0, WidgetOptions.RateNames.Length - 1);
		for (int index = 0; index < profile.Layouts.Count; index++)
		{
			WidgetLayout layout = profile.Layouts[index];
			if (layout == null)
			{
				layout = BuildLayout(index, 0);
				profile.Layouts[index] = layout;
			}

			if (layout.Widgets == null)
			{
				layout.Widgets = new List<WidgetConfig>();
			}

			if (string.IsNullOrEmpty(layout.Name))
			{
				layout.Name = "Layout " + (index + 1);
			}

			layout.Widgets.RemoveAll(widget => widget == null);
			for (int kind = 0; kind < (int)WidgetKind.Custom; kind++)
			{
				string id = "builtin-" + (WidgetKind)kind;
				if (!layout.Widgets.Exists(widget => widget.Id == id))
				{
					layout.Widgets.Add(NewWidget((WidgetKind)kind, 8 + kind, 0));
				}
			}

			foreach (WidgetConfig widget in layout.Widgets)
			{
				widget.Kind = Mathf.Clamp(widget.Kind, 0, (int)WidgetKind.Custom);
				widget.Scale = Mathf.Clamp(widget.Scale, 0.4f, 2.5f);
				widget.Alpha = Mathf.Clamp(widget.Alpha, 0.15f, 1f);
				widget.Background = WidgetOptions.Wrap(widget.Background, WidgetOptions.Backgrounds.Length);
				widget.TextColor = WidgetOptions.Wrap(widget.TextColor, WidgetOptions.TextColors.Length);
				widget.Border = WidgetOptions.Wrap(widget.Border, WidgetOptions.BorderSizes.Length);
				widget.Padding = WidgetOptions.Wrap(widget.Padding, WidgetOptions.Paddings.Length);
				widget.Interval = WidgetOptions.Wrap(widget.Interval, WidgetOptions.Intervals.Length);
				widget.Animation = WidgetOptions.Wrap(widget.Animation, WidgetOptions.AnimationNames.Length);
				widget.Anchor = WidgetOptions.Wrap(widget.Anchor, WidgetOptions.AnchorNames.Length);
				widget.Visibility = WidgetOptions.Wrap(widget.Visibility, WidgetOptions.VisibilityNames.Length);
				widget.Group = WidgetOptions.Wrap(widget.Group, WidgetOptions.GroupNames.Length);
				widget.Font = Mathf.Max(0, widget.Font);
				widget.Title = PlayerTags.Clean(widget.Title);
				widget.Text = PlayerTags.Clean(widget.Text);
				if (string.IsNullOrEmpty(widget.Id))
				{
					widget.Id = "custom-" + Guid.NewGuid().ToString("N").Substring(0, 8);
				}

				if (widget.Elements == null)
				{
					widget.Elements = new List<WidgetElement>();
				}

				widget.Elements.RemoveAll(element => element == null);
				if (widget.Elements.Count > 10)
				{
					widget.Elements.RemoveRange(10, widget.Elements.Count - 10);
				}

				foreach (WidgetElement element in widget.Elements)
				{
					element.Type = WidgetOptions.Wrap(element.Type, WidgetOptions.ElementTypes.Length);
					element.Text = PlayerTags.Clean(element.Text);
					element.Bind = Mathf.Max(0, element.Bind);
				}
			}
		}
	}

	private static string CleanName(string name)
	{
		StringBuilder builder = new StringBuilder();
		foreach (char character in name ?? string.Empty)
		{
			if (char.IsLetterOrDigit(character) || character == ' ' || character == '-' || character == '_')
			{
				builder.Append(character);
			}
		}

		string clean = builder.ToString().Trim();
		return clean.Length > 24 ? clean.Substring(0, 24) : clean;
	}

	private static WidgetProfile Load(string path)
	{
		try
		{
			return File.Exists(path) ? JsonConvert.DeserializeObject<WidgetProfile>(File.ReadAllText(path)) : null;
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Could not read widgets file: " + exception.Message);
			return null;
		}
	}

	private bool Write(string path, WidgetProfile profile)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, JsonConvert.SerializeObject(profile, Formatting.Indented));
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Could not save widgets: " + exception.Message);
			return false;
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;

namespace AUP;

public sealed class PlayerNotes
{
	public static readonly string[] Presets =
	{
		string.Empty, "Good sport", "Friendly", "Skilled", "Funny", "Laggy", "Tryhard", "Toxic", "Cheater?"
	};

	private const string FileName = "AceUtilsPad.players.txt";

	private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
	private readonly string path;

	public PlayerNotes()
	{
		path = Path.Combine(Paths.ConfigPath, FileName);
		Load();
	}

	public int Version { get; private set; }

	public string NoteFor(VRRig rig)
	{
		Entry entry = Find(rig);
		return entry == null ? string.Empty : entry.Note;
	}

	public bool IsStarred(VRRig rig)
	{
		Entry entry = Find(rig);
		return entry != null && entry.Starred;
	}

	public void ToggleStar(VRRig rig)
	{
		Entry entry = Ensure(rig);
		if (entry == null)
		{
			return;
		}

		entry.Starred = !entry.Starred;
		Changed();
	}

	public void SetNote(VRRig rig, string note)
	{
		Entry entry = Ensure(rig);
		if (entry == null)
		{
			return;
		}

		entry.Note = Clean(note);
		Changed();
	}

	public void CycleNote(VRRig rig)
	{
		Entry entry = Ensure(rig);
		if (entry == null)
		{
			return;
		}

		int index = Array.IndexOf(Presets, entry.Note);
		entry.Note = Presets[(index + 1 + Presets.Length) % Presets.Length];
		Changed();
	}

	private Entry Find(VRRig rig)
	{
		string key = Key(rig);
		return key != null && entries.TryGetValue(key, out Entry entry) ? entry : null;
	}

	private Entry Ensure(VRRig rig)
	{
		string key = Key(rig);
		if (key == null)
		{
			return null;
		}

		if (!entries.TryGetValue(key, out Entry entry))
		{
			entry = new Entry { Note = string.Empty };
			entries[key] = entry;
		}

		entry.Name = Clean(PlayerInfo.DisplayName(rig));
		return entry;
	}

	private void Changed()
	{
		Version++;
		Save();
	}

	private static string Key(VRRig rig)
	{
		if (rig == null)
		{
			return null;
		}

		string userId = PlayerInfo.UserId(rig);
		return userId == PlayerInfo.Unknown ? null : userId;
	}

	private static string Clean(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		StringBuilder builder = new StringBuilder(text.Length);
		foreach (char character in text)
		{
			if (!char.IsControl(character) && character != '<' && character != '>' && character != '|')
			{
				builder.Append(character);
			}
		}

		string cleaned = builder.ToString().Trim();
		return cleaned.Length > TextEntry.MaxLength ? cleaned.Substring(0, TextEntry.MaxLength) : cleaned;
	}

	private void Load()
	{
		try
		{
			if (!File.Exists(path))
			{
				return;
			}

			foreach (string line in File.ReadAllLines(path))
			{
				string[] parts = line.Split('|');
				if (parts.Length < 3 || string.IsNullOrEmpty(parts[0]))
				{
					continue;
				}

				entries[parts[0]] = new Entry
				{
					Starred = parts[1] == "1",
					Note = Clean(parts[2]),
					Name = parts.Length > 3 ? Clean(parts[3]) : string.Empty
				};
			}
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Could not read player notes: " + exception.Message);
		}
	}

	private void Save()
	{
		try
		{
			StringBuilder builder = new StringBuilder();
			foreach (KeyValuePair<string, Entry> pair in entries)
			{
				if (!pair.Value.Starred && string.IsNullOrEmpty(pair.Value.Note))
				{
					continue;
				}

				builder.Append(pair.Key).Append('|').Append(pair.Value.Starred ? '1' : '0').Append('|')
					.Append(pair.Value.Note).Append('|').Append(pair.Value.Name).Append('\n');
			}

			File.WriteAllText(path, builder.ToString());
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Could not save player notes: " + exception.Message);
		}
	}

	private sealed class Entry
	{
		public bool Starred;
		public string Note;
		public string Name;
	}
}

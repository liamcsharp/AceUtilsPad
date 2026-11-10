using System.Text;
using UnityEngine;

namespace AUP;

public sealed class LobbyKeyboardOverlay : MonoBehaviour
{
	private const int MaxCodeLength = 12;
	private const float KeyWidth = 54f;
	private const float KeyHeight = 40f;
	private const float KeyGap = 7f;
	private const float WindowWidth = 660f;
	private const string DefaultStatus = "Type a lobby code. This only joins an explicitly entered room.";

	private static readonly string[][] KeyRows =
	{
		new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
		new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" },
		new[] { "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T" },
		new[] { "U", "V", "W", "X", "Y", "Z" }
	};

	private readonly StringBuilder code = new StringBuilder(MaxCodeLength);

	private LobbyService lobby;
	private Rect windowRect = new Rect(230f, 120f, WindowWidth, 430f);
	private string status = DefaultStatus;

	public bool Visible { get; private set; }

	public void Initialize(LobbyService lobbyService)
	{
		lobby = lobbyService;
	}

	public void Toggle()
	{
		Visible = !Visible;
	}

	public void Close()
	{
		Visible = false;
	}

	private void OnGUI()
	{
		if (Visible)
		{
			windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "AceUtilsPad - Join Lobby");
		}
	}

	private void DrawWindow(int windowId)
	{
		HandlePhysicalKeyboard();
		GUI.Label(new Rect(20f, 24f, 620f, 28f), "Lobby code: " + (code.Length == 0 ? "_" : code.ToString()));
		GUI.Label(new Rect(20f, 53f, 620f, 24f), status);

		for (int row = 0; row < KeyRows.Length; row++)
		{
			DrawKeyRow(row);
		}

		float actionsY = 88f + KeyRows.Length * (KeyHeight + KeyGap) + 5f;
		if (GUI.Button(new Rect(20f, actionsY, 145f, KeyHeight), "Backspace"))
		{
			RemoveLastCharacter();
		}

		if (GUI.Button(new Rect(175f, actionsY, 145f, KeyHeight), "Clear"))
		{
			code.Length = 0;
		}

		if (GUI.Button(new Rect(330f, actionsY, 145f, KeyHeight), "Join"))
		{
			RequestJoin();
		}

		if (GUI.Button(new Rect(485f, actionsY, 145f, KeyHeight), "Close"))
		{
			Close();
		}

		GUI.DragWindow(new Rect(0f, 0f, WindowWidth, 20f));
	}

	private void DrawKeyRow(int row)
	{
		string[] keys = KeyRows[row];
		float rowWidth = keys.Length * KeyWidth + (keys.Length - 1) * KeyGap;
		float startX = (WindowWidth - rowWidth) * 0.5f;
		for (int column = 0; column < keys.Length; column++)
		{
			Rect keyRect = new Rect(startX + column * (KeyWidth + KeyGap), 88f + row * (KeyHeight + KeyGap), KeyWidth, KeyHeight);
			if (GUI.Button(keyRect, keys[column]))
			{
				Append(keys[column]);
			}
		}
	}

	private void HandlePhysicalKeyboard()
	{
		Event current = Event.current;
		if (current == null || current.type != EventType.KeyDown)
		{
			return;
		}

		if (current.keyCode == KeyCode.Backspace)
		{
			RemoveLastCharacter();
		}
		else if (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
		{
			RequestJoin();
		}
		else if (current.keyCode == KeyCode.Escape)
		{
			Close();
		}
		else if (char.IsLetterOrDigit(current.character))
		{
			Append(current.character.ToString());
		}
		else
		{
			return;
		}

		current.Use();
	}

	private void RemoveLastCharacter()
	{
		if (code.Length > 0)
		{
			code.Length--;
		}
	}

	private void Append(string value)
	{
		if (code.Length < MaxCodeLength)
		{
			code.Append(value.ToUpperInvariant());
		}
	}

	private void RequestJoin()
	{
		string room = code.ToString().Trim().ToUpperInvariant();
		if (room.Length == 0)
		{
			status = "Enter a lobby code first.";
			return;
		}

		lobby.JoinCode(room);
		status = lobby.Status;
	}
}

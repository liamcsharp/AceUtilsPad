using System;
using System.Text;

namespace AUP;

public sealed class TextEntry
{
	public const int MaxLength = 24;

	private readonly StringBuilder buffer = new StringBuilder(MaxLength);
	private Action<string> commit;

	public bool Active { get; private set; }

	public string Purpose { get; private set; } = string.Empty;

	public bool Caps { get; set; } = true;

	public string Value => buffer.ToString();

	public event Action Changed;

	public void Begin(string purpose, string initial, Action<string> onCommit)
	{
		Purpose = purpose ?? string.Empty;
		commit = onCommit;
		buffer.Length = 0;
		if (!string.IsNullOrEmpty(initial))
		{
			foreach (char character in initial)
			{
				Push(character);
			}
		}

		Active = true;
		Changed?.Invoke();
	}

	public void Type(string key)
	{
		if (!Active || string.IsNullOrEmpty(key))
		{
			return;
		}

		foreach (char character in key)
		{
			Push(Caps ? char.ToUpperInvariant(character) : char.ToLowerInvariant(character));
		}

		Changed?.Invoke();
	}

	public void TypeRaw(char character)
	{
		if (!Active)
		{
			return;
		}

		Push(character);
		Changed?.Invoke();
	}

	public void Space()
	{
		if (Active && buffer.Length > 0 && buffer[buffer.Length - 1] != ' ')
		{
			Push(' ');
			Changed?.Invoke();
		}
	}

	public void Backspace()
	{
		if (Active && buffer.Length > 0)
		{
			buffer.Length--;
			Changed?.Invoke();
		}
	}

	public void Clear()
	{
		if (Active)
		{
			buffer.Length = 0;
			Changed?.Invoke();
		}
	}

	public void ToggleCaps()
	{
		Caps = !Caps;
		Changed?.Invoke();
	}

	public void Commit()
	{
		if (!Active)
		{
			return;
		}

		string value = buffer.ToString().Trim();
		Action<string> callback = commit;
		Active = false;
		commit = null;
		callback?.Invoke(value);
		Changed?.Invoke();
	}

	public void Cancel()
	{
		if (!Active)
		{
			return;
		}

		Active = false;
		commit = null;
		Changed?.Invoke();
	}

	private void Push(char character)
	{
		if (buffer.Length >= MaxLength || char.IsControl(character) || character == '<' || character == '>' || character == '|' || character == '\t')
		{
			return;
		}

		buffer.Append(character);
	}
}

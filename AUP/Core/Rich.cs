using System.Text.RegularExpressions;

namespace AUP;

public static class Rich
{
	private static readonly Regex DisallowedTag = new Regex(
		"<(?!/?b>|/color>|/size>|color=#[0-9a-fA-F]{6}>|size=[0-9]{1,3}%>)[^>]*>",
		RegexOptions.Compiled);

	public static string Escape(string text)
	{
		return (text ?? "Unknown").Replace('<', '(').Replace('>', ')');
	}

	public static string Sanitize(string text)
	{
		return string.IsNullOrEmpty(text) ? string.Empty : DisallowedTag.Replace(text, string.Empty);
	}
}

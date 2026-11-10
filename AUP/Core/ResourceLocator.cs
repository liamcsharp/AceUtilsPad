using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;

namespace AUP;

internal static class ResourceLocator
{
	public static byte[] ReadEmbedded(string fileName)
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		foreach (string name in assembly.GetManifestResourceNames())
		{
			if (!name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			using (Stream stream = assembly.GetManifestResourceStream(name))
			{
				if (stream == null)
				{
					continue;
				}

				using (MemoryStream copy = new MemoryStream())
				{
					stream.CopyTo(copy);
					return copy.ToArray();
				}
			}
		}

		return null;
	}

	public static byte[] ReadAny(string fileName)
	{
		byte[] embedded = ReadEmbedded(fileName);
		if (embedded != null)
		{
			return embedded;
		}

		string path = FindFile(fileName);
		return path == null ? null : File.ReadAllBytes(path);
	}

	public static string FindFile(string fileName)
	{
		foreach (string root in SearchRoots())
		{
			try
			{
				if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
				{
					continue;
				}

				string[] found = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
				if (found.Length > 0)
				{
					return found[0];
				}
			}
			catch (Exception)
			{
			}
		}

		return null;
	}

	private static IEnumerable<string> SearchRoots()
	{
		string location = null;
		try
		{
			location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		}
		catch (Exception)
		{
		}

		yield return location;
		yield return Paths.PluginPath;
	}
}

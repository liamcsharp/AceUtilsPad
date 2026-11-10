using System;
using System.Collections.Generic;
using AUP.UI;

namespace AUP;

internal static class WorldUi
{
	private static readonly List<Action<List<UiButton>>> Collectors = new List<Action<List<UiButton>>>();

	public static void Register(Action<List<UiButton>> collector)
	{
		if (collector != null && !Collectors.Contains(collector))
		{
			Collectors.Add(collector);
		}
	}

	public static void Unregister(Action<List<UiButton>> collector)
	{
		Collectors.Remove(collector);
	}

	public static void Collect(List<UiButton> targets)
	{
		for (int i = 0; i < Collectors.Count; i++)
		{
			Collectors[i](targets);
		}
	}
}

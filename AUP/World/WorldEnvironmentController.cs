using System;
using UnityEngine;
using WeatherType = BetterDayNightManager.WeatherType;

namespace AUP;

public sealed class WorldEnvironmentController : MonoBehaviour
{
	private const float RefreshInterval = 0.5f;
	private const int FallbackTimeCount = 10;
	private const string NotLoaded = "Day/night system is not loaded yet.";

	private int timeOverrideIndex = -1;
	private bool weatherOverridden;
	private WeatherType weatherOverride = WeatherType.None;
	private float nextRefreshTime;
	private string cachedStatus = "Time of day: Loading...\nWeather: Loading...";

	public bool TimeOverridden => timeOverrideIndex >= 0;

	public bool WeatherOverridden => weatherOverridden;

	public bool RainForced => weatherOverridden && weatherOverride == WeatherType.Raining;

	public bool ClearForced => weatherOverridden && weatherOverride == WeatherType.None;

	public string LastMessage { get; private set; } = "Changes are local only. Other players are not affected.";

	public string GetStatus()
	{
		if (Time.unscaledTime >= nextRefreshTime)
		{
			nextRefreshTime = Time.unscaledTime + RefreshInterval;
			cachedStatus = BuildStatus();
		}

		return cachedStatus;
	}

	public void StepTime(int direction)
	{
		BetterDayNightManager manager = BetterDayNightManager.instance;
		if (manager == null)
		{
			LastMessage = NotLoaded;
			return;
		}

		int count = TimeCount(manager);
		int current = timeOverrideIndex >= 0 ? timeOverrideIndex : Mathf.Clamp(manager.currentTimeIndex, 0, count - 1);
		ApplyTime(manager, ((current + direction) % count + count) % count);
	}

	public void ResetTime()
	{
		timeOverrideIndex = -1;
		BetterDayNightManager manager = BetterDayNightManager.instance;
		if (manager == null)
		{
			LastMessage = NotLoaded;
			return;
		}

		try
		{
			manager.ClearTimeOfDay(true);
			LastMessage = "Time of day follows the server again.";
		}
		catch (Exception exception)
		{
			LastMessage = "Could not reset time: " + exception.Message;
		}

		nextRefreshTime = 0f;
	}

	public void SetWeather(bool raining)
	{
		BetterDayNightManager manager = BetterDayNightManager.instance;
		if (manager == null)
		{
			LastMessage = "Weather system is not loaded yet.";
			return;
		}

		WeatherType weather = raining ? WeatherType.Raining : WeatherType.None;
		try
		{
			manager.SetFixedWeather(weather, true);
			weatherOverridden = true;
			weatherOverride = weather;
			LastMessage = raining ? "Rain enabled locally." : "Clear skies enabled locally.";
		}
		catch (Exception exception)
		{
			LastMessage = "Could not change weather: " + exception.Message;
		}

		nextRefreshTime = 0f;
	}

	public void ResetWeather()
	{
		weatherOverridden = false;
		weatherOverride = WeatherType.None;
		BetterDayNightManager manager = BetterDayNightManager.instance;
		if (manager == null)
		{
			LastMessage = "Weather system is not loaded yet.";
			return;
		}

		try
		{
			manager.ClearFixedWeather(true);
			LastMessage = "Weather follows the server again.";
		}
		catch (Exception exception)
		{
			LastMessage = "Could not reset weather: " + exception.Message;
		}

		nextRefreshTime = 0f;
	}

	public void ResetAll()
	{
		ResetTime();
		ResetWeather();
		LastMessage = "Time and weather follow the server again.";
	}

	private void OnDestroy()
	{
		BetterDayNightManager manager = BetterDayNightManager.instance;
		if (manager == null)
		{
			return;
		}

		try
		{
			if (timeOverrideIndex >= 0)
			{
				manager.ClearTimeOfDay(true);
			}

			if (weatherOverridden)
			{
				manager.ClearFixedWeather(true);
			}
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Could not restore the world environment: " + exception.Message);
		}
	}

	private void ApplyTime(BetterDayNightManager manager, int index)
	{
		try
		{
			manager.SetTimeOfDay(index, true);
			timeOverrideIndex = index;
			LastMessage = "Time set to " + TimeName(manager, index) + " (local only).";
		}
		catch (Exception exception)
		{
			LastMessage = "Could not change time: " + exception.Message;
		}

		nextRefreshTime = 0f;
	}

	private string BuildStatus()
	{
		BetterDayNightManager manager = BetterDayNightManager.instance;
		if (manager == null)
		{
			return "Time of day: Not loaded\nWeather: Not loaded";
		}

		string time = ReadTimeName(manager);
		string weather = ReadWeatherName(manager);
		return "Time of day: " + Pretty(time) + (TimeOverridden ? " (custom)" : " (server)")
			   + "\nWeather: " + weather + (weatherOverridden ? " (custom)" : " (server)");
	}

	private string ReadTimeName(BetterDayNightManager manager)
	{
		string name;
		try
		{
			name = TimeOverridden ? TimeName(manager, timeOverrideIndex) : manager.GetTimeOfDayString();
		}
		catch (Exception)
		{
			name = "Unknown";
		}

		return string.IsNullOrEmpty(name) ? TimeName(manager, manager.currentTimeIndex) : name;
	}

	private static string ReadWeatherName(BetterDayNightManager manager)
	{
		try
		{
			return manager.CurrentWeather() == WeatherType.Raining ? "Raining" : "Clear";
		}
		catch (Exception)
		{
			return "Unknown";
		}
	}

	private static int TimeCount(BetterDayNightManager manager)
	{
		string[] names = manager.dayNightLightmapNames;
		return names != null && names.Length > 0 ? names.Length : FallbackTimeCount;
	}

	private static string TimeName(BetterDayNightManager manager, int index)
	{
		string[] names = manager.dayNightLightmapNames;
		if (names != null && index >= 0 && index < names.Length && !string.IsNullOrEmpty(names[index]))
		{
			return Pretty(names[index]);
		}

		return "Preset " + (index + 1);
	}

	private static string Pretty(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "Unknown";
		}

		string text = value.Replace("_", " ").Replace("-", " ").Trim();
		return text.Length == 1 ? text.ToUpperInvariant() : char.ToUpperInvariant(text[0]) + text.Substring(1);
	}
}

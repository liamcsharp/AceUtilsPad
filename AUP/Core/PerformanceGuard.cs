using UnityEngine;

namespace AUP;

public static class PerformanceGuard
{
	private const float DropRatio = 0.72f;
	private const float RecoverRatio = 0.9f;
	private const float DropSeconds = 2f;
	private const float RecoverSeconds = 6f;
	private const float PeakDecayPerSecond = 0.25f;
	private const float MinimumPeak = 45f;

	private static float peak = 72f;
	private static float lowClock;
	private static float highClock;

	public static int Level { get; private set; }

	public static string Status
	{
		get
		{
			switch (Level)
			{
				case 1:
					return "Saving frames: particles and moving colors paused";
				case 2:
					return "Saving frames: all menu effects paused";
				default:
					return "Running at full quality";
			}
		}
	}

	public static void Tick(int mode, float fps, float deltaTime)
	{
		if (mode <= 0)
		{
			Level = 0;
			lowClock = 0f;
			highClock = 0f;
			return;
		}

		int floor = mode >= 2 ? 1 : 0;
		if (fps <= 1f)
		{
			Level = Mathf.Max(Level, floor);
			return;
		}

		peak = Mathf.Max(MinimumPeak, Mathf.Max(fps, peak - PeakDecayPerSecond * deltaTime));
		if (fps < peak * DropRatio)
		{
			lowClock += deltaTime;
			highClock = 0f;
		}
		else if (fps > peak * RecoverRatio)
		{
			highClock += deltaTime;
			lowClock = 0f;
		}
		else
		{
			lowClock = 0f;
			highClock = 0f;
		}

		if (lowClock >= DropSeconds && Level < 2)
		{
			Level++;
			lowClock = 0f;
		}
		else if (highClock >= RecoverSeconds && Level > floor)
		{
			Level--;
			highClock = 0f;
		}

		Level = Mathf.Max(Level, floor);
	}

	public static float Interval(float seconds)
	{
		switch (Level)
		{
			case 1:
				return seconds * 1.6f;
			case 2:
				return seconds * 2.5f;
			default:
				return seconds;
		}
	}
}

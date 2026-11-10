using System;
using UnityEngine;

namespace AUP.UI;

public static class NotifySound
{
	public static readonly string[] Names = { "Off", "Chime", "Ding", "Pop", "Royal", "Blip", "Bell" };

	private const int SampleRate = 44100;

	private static readonly AudioClip[] Clips = new AudioClip[Names.Length];
	private static AudioSource source;

	public static int Clamp(int index)
	{
		return Mathf.Clamp(index, 0, Names.Length - 1);
	}

	public static string Name(int index)
	{
		return Names[Clamp(index)];
	}

	public static void Play(int index, float volume)
	{
		index = Clamp(index);
		if (index == 0 || volume <= 0.001f)
		{
			return;
		}

		try
		{
			if (Clips[index] == null)
			{
				Clips[index] = Build(index);
			}

			if (source == null)
			{
				GameObject host = new GameObject("AUPNotifySound");
				UnityEngine.Object.DontDestroyOnLoad(host);
				source = host.AddComponent<AudioSource>();
				source.spatialBlend = 0f;
				source.playOnAwake = false;
				source.ignoreListenerPause = true;
			}

			source.PlayOneShot(Clips[index], Mathf.Clamp01(volume));
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Notification sound failed: " + exception.Message);
		}
	}

	private static AudioClip Build(int index)
	{
		float[] samples;
		switch (index)
		{
			case 1:
				samples = Mix(0.9f, Note(880f, 0f, 0.5f, 5f, 0.3f), Note(1318.5f, 0.12f, 0.6f, 4.5f, 0.3f));
				break;
			case 2:
				samples = Mix(0.6f, Note(1568f, 0f, 0.55f, 7f, 0.15f));
				break;
			case 3:
				samples = Mix(0.25f, Sweep(520f, 180f, 0f, 0.14f, 22f));
				break;
			case 4:
				samples = Mix(1.3f, Note(523.25f, 0f, 0.3f, 6f, 0.4f), Note(659.25f, 0.14f, 0.3f, 6f, 0.4f),
					Note(783.99f, 0.28f, 0.3f, 6f, 0.4f), Note(1046.5f, 0.42f, 0.8f, 3.5f, 0.4f));
				break;
			case 5:
				samples = Mix(0.3f, Note(1200f, 0f, 0.08f, 30f, 0f), Note(1600f, 0.09f, 0.1f, 28f, 0f));
				break;
			default:
				samples = Mix(1.4f, Note(659.25f, 0f, 1.3f, 2.6f, 0.6f), Note(1318.5f, 0f, 1f, 4f, 0.2f), Note(1975.5f, 0f, 0.6f, 6f, 0.1f));
				break;
		}

		AudioClip clip = AudioClip.Create("AUPNotify" + Names[index], samples.Length, 1, SampleRate, false);
		clip.SetData(samples, 0);
		return clip;
	}

	private static Func<float, float> Note(float frequency, float start, float length, float decay, float harmonic)
	{
		return time =>
		{
			float local = time - start;
			if (local < 0f || local > length)
			{
				return 0f;
			}

			float attack = Mathf.Clamp01(local / 0.006f);
			float release = Mathf.Clamp01((length - local) / 0.03f);
			float envelope = attack * release * Mathf.Exp(-decay * local);
			float phase = 2f * Mathf.PI * frequency * local;
			return envelope * (Mathf.Sin(phase) + harmonic * Mathf.Sin(phase * 2f)) * 0.4f;
		};
	}

	private static Func<float, float> Sweep(float from, float to, float start, float length, float decay)
	{
		return time =>
		{
			float local = time - start;
			if (local < 0f || local > length)
			{
				return 0f;
			}

			float progress = local / length;
			float frequency = Mathf.Lerp(from, to, progress);
			float envelope = Mathf.Clamp01(local / 0.004f) * Mathf.Exp(-decay * local);
			return envelope * Mathf.Sin(2f * Mathf.PI * frequency * local) * 0.6f;
		};
	}

	private static float[] Mix(float seconds, params Func<float, float>[] voices)
	{
		int count = Mathf.CeilToInt(seconds * SampleRate);
		float[] samples = new float[count];
		for (int i = 0; i < count; i++)
		{
			float time = i / (float)SampleRate;
			float value = 0f;
			foreach (Func<float, float> voice in voices)
			{
				value += voice(time);
			}

			samples[i] = Mathf.Clamp(value, -1f, 1f);
		}

		return samples;
	}
}

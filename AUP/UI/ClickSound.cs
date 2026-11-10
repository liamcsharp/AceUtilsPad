using System;
using System.IO;
using UnityEngine;

namespace AUP.UI;

public static class ClickSound
{
	private const string FileName = "ButtonPressWood.wav";
	private const float Volume = 0.8f;
	private const int PcmFormat = 1;
	private const int SixteenBits = 16;

	private static AudioClip clip;
	private static AudioSource source;

	public static void Play()
	{
		try
		{
			if (clip == null)
			{
				clip = LoadClip();
				if (clip == null)
				{
					return;
				}
			}

			if (source == null)
			{
				GameObject host = new GameObject("AUPClickSound");
				UnityEngine.Object.DontDestroyOnLoad(host);
				source = host.AddComponent<AudioSource>();
				source.spatialBlend = 0f;
				source.playOnAwake = false;
				source.priority = 0;
				source.ignoreListenerPause = true;
			}

			source.PlayOneShot(clip, Volume);
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Click sound failed: " + exception.Message);
		}
	}

	private static AudioClip LoadClip()
	{
		byte[] found = null;
		try
		{
			found = ResourceLocator.ReadAny(FileName);
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Could not read the click sound file: " + exception.Message);
		}

		if (found != null)
		{
			try
			{
				return Decode(found);
			}
			catch (Exception exception)
			{
				Debug.LogWarning("[AceUtilsPad] Click sound file was unreadable: " + exception.Message);
			}
		}

		try
		{
			return Decode(ClickData.Wave());
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Could not load the click sound: " + exception.Message);
			return null;
		}
	}

	private static AudioClip Decode(byte[] data)
	{
		using (MemoryStream stream = new MemoryStream(data))
		{
			return DecodeWave(stream);
		}
	}

	private static AudioClip DecodeWave(Stream stream)
	{
		using (BinaryReader reader = new BinaryReader(stream))
		{
			reader.ReadBytes(12);
			int channels = 0;
			int sampleRate = 0;
			int bits = 0;
			int format = 0;
			while (stream.Position + 8 <= stream.Length)
			{
				string id = new string(reader.ReadChars(4));
				int size = reader.ReadInt32();
				if (id == "fmt ")
				{
					format = reader.ReadInt16();
					channels = reader.ReadInt16();
					sampleRate = reader.ReadInt32();
					reader.ReadInt32();
					reader.ReadInt16();
					bits = reader.ReadInt16();
					reader.ReadBytes(size - 16);
				}
				else if (id == "data")
				{
					if (format != PcmFormat || bits != SixteenBits || channels < 1)
					{
						throw new InvalidDataException("Only 16-bit PCM wave files are supported.");
					}

					int available = (int)Math.Min(size, stream.Length - stream.Position);
					return BuildClip(reader.ReadBytes(available), channels, sampleRate);
				}
				else
				{
					reader.ReadBytes(size + (size & 1));
				}
			}
		}

		throw new InvalidDataException("The wave file has no audio data.");
	}

	private static AudioClip BuildClip(byte[] data, int channels, int sampleRate)
	{
		int sampleCount = data.Length / 2 / channels * channels;
		float[] samples = new float[sampleCount];
		for (int i = 0; i < sampleCount; i++)
		{
			samples[i] = BitConverter.ToInt16(data, i * 2) / 32768f;
		}

		AudioClip created = AudioClip.Create("AUPClick", sampleCount / channels, channels, sampleRate, false);
		created.SetData(samples, 0);
		return created;
	}
}

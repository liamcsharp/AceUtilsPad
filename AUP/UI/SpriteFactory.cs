using System.Collections.Generic;
using UnityEngine;

namespace AUP.UI;

public static class SpriteFactory
{
	private const int RoundedSize = 128;
	private const float EdgeSoftness = 1.5f;

	private static readonly Dictionary<int, Sprite> RoundedCache = new Dictionary<int, Sprite>();
	private static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>();
	private static readonly Dictionary<SceneryStyle, Sprite> SceneryCache = new Dictionary<SceneryStyle, Sprite>();
	private static readonly Dictionary<SceneryStyle, SceneryJob> SceneryJobs = new Dictionary<SceneryStyle, SceneryJob>();

	private static Sprite ring;
	private static Sprite glow;
	private static Sprite petal;
	private static Sprite leaf;
	private static Sprite logo;

	public static Sprite Rounded(int cornerRadius)
	{
		cornerRadius = Mathf.Clamp(cornerRadius, 0, RoundedSize / 2);
		if (RoundedCache.TryGetValue(cornerRadius, out Sprite cached) && cached != null)
		{
			return cached;
		}

		Texture2D texture = new Texture2D(RoundedSize, RoundedSize, TextureFormat.RGBA32, false)
		{
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp
		};

		Color32[] pixels = new Color32[RoundedSize * RoundedSize];
		for (int y = 0; y < RoundedSize; y++)
		{
			for (int x = 0; x < RoundedSize; x++)
			{
				pixels[y * RoundedSize + x] = new Color32(255, 255, 255, (byte)(CornerCoverage(x, y, cornerRadius) * 255f));
			}
		}

		texture.SetPixels32(pixels);
		texture.Apply();

		Rect rect = new Rect(0f, 0f, RoundedSize, RoundedSize);
		Vector4 border = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);
		Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 1u, SpriteMeshType.FullRect, border);
		RoundedCache[cornerRadius] = sprite;
		return sprite;
	}

	public static Sprite Circle()
	{
		return Rounded(RoundedSize / 2);
	}

	public static Sprite Ring()
	{
		if (ring == null)
		{
			ring = FromField(128, (x, y) =>
			{
				float distance = Mathf.Sqrt(x * x + y * y);
				return Mathf.Clamp01(1f - Mathf.Abs(distance - 0.86f) / 0.1f);
			});
		}

		return ring;
	}

	public static Sprite Glow()
	{
		if (glow == null)
		{
			glow = FromField(128, (x, y) =>
			{
				float distance = Mathf.Clamp01(Mathf.Sqrt(x * x + y * y));
				float falloff = 1f - distance;
				return falloff * falloff * (3f - 2f * falloff);
			});
		}

		return glow;
	}

	public static Sprite Petal()
	{
		if (petal == null)
		{
			petal = FromField(64, (x, y) =>
			{
				float ellipse = Mathf.Sqrt(x * x / 0.36f + y * y / 0.9f) - 1f;
				float notch = 0.22f - Mathf.Sqrt(x * x + (y - 0.98f) * (y - 0.98f));
				return Mathf.Clamp01(0.5f - Mathf.Max(ellipse, notch) * 18f);
			});
		}

		return petal;
	}

	public static Sprite Leaf()
	{
		if (leaf == null)
		{
			leaf = FromField(64, (x, y) =>
			{
				float left = Mathf.Sqrt((x + 0.55f) * (x + 0.55f) + y * y) - 1f;
				float right = Mathf.Sqrt((x - 0.55f) * (x - 0.55f) + y * y) - 1f;
				float vein = 0.035f - Mathf.Abs(x);
				float shape = Mathf.Max(left, right);
				float coverage = Mathf.Clamp01(0.5f - shape * 18f);
				return vein > 0f && Mathf.Abs(y) < 0.8f ? coverage * 0.55f : coverage;
			});
		}

		return leaf;
	}

	public static Sprite Logo()
	{
		if (logo != null)
		{
			return logo;
		}

		try
		{
			Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true)
			{
				name = "AUPLogo",
				filterMode = FilterMode.Trilinear,
				wrapMode = TextureWrapMode.Clamp
			};

			if (!ImageConversion.LoadImage(texture, LogoData.Png(), false))
			{
				return Circle();
			}

			texture.filterMode = FilterMode.Trilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
			logo = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
			logo.name = "AUPLogo";
		}
		catch (System.Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Could not load the orb logo: " + exception.Message);
			return Circle();
		}

		return logo;
	}

	public static Sprite Scenery(SceneryStyle style)
	{
		if (style == SceneryStyle.None)
		{
			return null;
		}

		if (SceneryCache.TryGetValue(style, out Sprite cached) && cached != null)
		{
			return cached;
		}

		if (!SceneryJobs.TryGetValue(style, out SceneryJob job))
		{
			job = new SceneryJob();
			SceneryJobs[style] = job;
			SceneryStyle captured = style;
			SceneryJob target = job;
			System.Threading.Tasks.Task.Run(() =>
			{
				try
				{
					target.Pixels = SceneryPainter.PaintPixels(captured, out target.Width, out target.Height);
				}
				catch (System.Exception)
				{
					target.Failed = true;
				}

				target.Done = true;
			});
			return null;
		}

		if (!job.Done || job.Failed || job.Pixels == null)
		{
			return null;
		}

		Texture2D texture = new Texture2D(job.Width, job.Height, TextureFormat.RGBA32, true)
		{
			name = "AUPScenery_" + style,
			filterMode = FilterMode.Trilinear,
			wrapMode = TextureWrapMode.Clamp
		};
		texture.SetPixels(job.Pixels);
		texture.Apply(true);
		job.Pixels = null;
		Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
		sprite.name = "AUPScenery_" + style;
		SceneryCache[style] = sprite;
		return sprite;
	}

	public static bool SceneryFailed(SceneryStyle style)
	{
		return SceneryJobs.TryGetValue(style, out SceneryJob job) && job.Done && job.Failed;
	}

	private sealed class SceneryJob
	{
		public volatile bool Done;
		public volatile bool Failed;
		public Color[] Pixels;
		public int Width;
		public int Height;
	}

	private static Sprite FromField(int size, System.Func<float, float, float> coverage)
	{
		Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
		{
			filterMode = FilterMode.Trilinear,
			wrapMode = TextureWrapMode.Clamp
		};

		Color32[] pixels = new Color32[size * size];
		float step = 2f / size;
		for (int row = 0; row < size; row++)
		{
			float y = (row + 0.5f) * step - 1f;
			for (int column = 0; column < size; column++)
			{
				float x = (column + 0.5f) * step - 1f;
				pixels[row * size + column] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(coverage(x, y)) * 255f));
			}
		}

		texture.SetPixels32(pixels);
		texture.Apply(true);
		return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
	}

	public static Sprite Icon(string iconId)
	{
		if (IconCache.TryGetValue(iconId, out Sprite cached) && cached != null)
		{
			return cached;
		}

		Color32[] pixels = SideRailIcons.Render(iconId, SideRailIcons.TextureSize);
		if (pixels == null)
		{
			return null;
		}

		int size = SideRailIcons.TextureSize;
		Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
		{
			name = "AUPIcon_" + iconId,
			filterMode = FilterMode.Trilinear,
			wrapMode = TextureWrapMode.Clamp
		};

		texture.SetPixels32(pixels);
		texture.Apply(true);

		Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
		sprite.name = "AUPIcon_" + iconId;
		IconCache[iconId] = sprite;
		return sprite;
	}

	private static float CornerCoverage(int x, int y, int radius)
	{
		bool left = x < radius;
		bool right = x >= RoundedSize - radius;
		bool bottom = y < radius;
		bool top = y >= RoundedSize - radius;
		if (!(left || right) || !(bottom || top))
		{
			return 1f;
		}

		float centerX = left ? radius : RoundedSize - radius;
		float centerY = bottom ? radius : RoundedSize - radius;
		float dx = x - centerX;
		float dy = y - centerY;
		float distance = Mathf.Sqrt(dx * dx + dy * dy);
		return distance <= radius ? 1f : Mathf.Clamp01(1f - (distance - radius) / EdgeSoftness);
	}
}

using System;
using UnityEngine;

namespace AUP.UI;

internal static class SceneryPainter
{
	public const float Width = 360f;
	public const float Height = 150f;

	private const float Scale = 1.5f;
	private const float CornerRadius = 16f;
	private const float FadeStart = 78f;

	public static Color[] PaintPixels(SceneryStyle style, out int pixelWidth, out int pixelHeight)
	{
		Paper paper = new Paper(Mathf.RoundToInt(Width * Scale), Mathf.RoundToInt(Height * Scale), Scale);
		System.Random random = new System.Random(1000 + (int)style * 37);
		switch (style)
		{
			case SceneryStyle.Lava:
				PaintLava(paper, random);
				break;
			case SceneryStyle.Sakura:
				PaintSakura(paper, random);
				break;
			case SceneryStyle.Forest:
				PaintForest(paper, random);
				break;
			case SceneryStyle.Canyon:
				PaintCanyon(paper, random);
				break;
			case SceneryStyle.Cave:
				PaintCave(paper, random);
				break;
			case SceneryStyle.Beach:
				PaintBeach(paper, random);
				break;
			case SceneryStyle.City:
				PaintCity(paper, random);
				break;
			case SceneryStyle.Clouds:
				PaintClouds(paper, random);
				break;
		}

		paper.Finish(FadeStart, Height, CornerRadius);
		pixelWidth = paper.PixelWidth;
		pixelHeight = paper.PixelHeight;
		return paper.Pixels;
	}

	private static void PaintLava(Paper p, System.Random random)
	{
		p.Glow(250f, 40f, 150f, C(1f, 0.32f, 0.06f, 0.32f));
		p.Ridge(x => 70f + 16f * Mathf.Sin(x * 0.021f + 1.3f) + 9f * Mathf.Sin(x * 0.057f), C(0.13f, 0.035f, 0.03f));
		p.Polygon(new[] { 150f, 0f, 226f, 104f, 268f, 104f, 352f, 0f }, C(0.09f, 0.022f, 0.02f));
		p.Glow(247f, 106f, 34f, C(1f, 0.5f, 0.12f, 0.95f));
		p.Ellipse(247f, 104f, 19f, 4f, C(1f, 0.72f, 0.28f, 1f));
		p.Capsule(240f, 102f, 224f, 66f, 2.4f, C(1f, 0.48f, 0.1f, 1f));
		p.Capsule(224f, 66f, 216f, 34f, 2f, C(1f, 0.42f, 0.08f, 1f));
		p.Capsule(256f, 102f, 272f, 72f, 2f, C(1f, 0.5f, 0.12f, 1f));
		p.Capsule(272f, 72f, 270f, 42f, 1.7f, C(1f, 0.42f, 0.08f, 1f));
		p.Ridge(x => 30f + 8f * Mathf.Sin(x * 0.05f) + 5f * Mathf.Sin(x * 0.13f + 2f), C(0.055f, 0.015f, 0.015f));
		Func<float, float> river = x => 15f + 4f * Mathf.Sin(x * 0.03f) + 1.5f * Mathf.Sin(x * 0.11f);
		for (int i = 0; i < 8; i++)
		{
			float x = 20f + i * 46f;
			p.Glow(x, river(x) + 4f, 30f, C(1f, 0.4f, 0.06f, 0.28f));
		}

		p.Band(river, 5.5f, C(1f, 0.38f, 0.06f, 1f));
		p.Band(river, 2.2f, C(1f, 0.86f, 0.38f, 1f));
		for (int i = 0; i < 14; i++)
		{
			float x = Range(random, 0f, Width);
			p.Ellipse(x, Range(random, 2f, 8f), Range(random, 5f, 12f), Range(random, 3f, 6f), C(0.045f, 0.012f, 0.012f, 1f));
		}
	}

	private static void PaintSakura(Paper p, System.Random random)
	{
		p.Glow(300f, 114f, 52f, C(1f, 0.86f, 0.93f, 0.4f));
		p.Circle(300f, 114f, 15f, C(1f, 0.92f, 0.96f, 0.9f));
		p.Ridge(x => 58f + 14f * Mathf.Sin(x * 0.018f + 0.4f) + 6f * Mathf.Sin(x * 0.05f), C(0.3f, 0.12f, 0.22f));
		Tree(p, random, 186f, 50f, 0.55f, 0.55f);
		p.Ridge(x => 34f + 6f * Mathf.Sin(x * 0.025f + 2f) + 2f * Mathf.Sin(x * 0.09f), C(0.2f, 0.07f, 0.14f));
		Tree(p, random, 66f, 30f, 1f, 1f);
		Tree(p, random, 296f, 32f, 0.82f, 1f);
		for (int i = 0; i < 40; i++)
		{
			float x = Range(random, 0f, Width);
			float y = Range(random, 2f, 28f);
			p.Ellipse(x, y, Range(random, 1.4f, 2.6f), Range(random, 0.9f, 1.5f), Pick(random, C(1f, 0.72f, 0.85f, 0.95f), C(1f, 0.85f, 0.92f, 0.9f), C(0.98f, 0.6f, 0.78f, 0.9f)));
		}
	}

	private static void Tree(Paper p, System.Random random, float x, float y, float size, float alpha)
	{
		Color bark = C(0.24f, 0.11f, 0.12f, alpha);
		p.Polygon(new[] { x - 6f * size, y - 4f, x + 6f * size, y - 4f, x + 2.5f * size, y + 42f * size, x - 3f * size, y + 42f * size }, bark);
		p.Capsule(x - 1f * size, y + 30f * size, x - 28f * size, y + 58f * size, 2.8f * size, bark);
		p.Capsule(x + 1f * size, y + 34f * size, x + 30f * size, y + 60f * size, 2.5f * size, bark);
		p.Capsule(x, y + 38f * size, x - 2f * size, y + 70f * size, 2.3f * size, bark);
		float[] centersX = { -28f, 30f, -2f, -14f, 16f, -38f, 40f };
		float[] centersY = { 62f, 64f, 76f, 82f, 80f, 54f, 56f };
		Color[] shades =
		{
			C(0.9f, 0.48f, 0.68f, alpha), C(0.98f, 0.62f, 0.78f, alpha), C(1f, 0.74f, 0.86f, alpha), C(1f, 0.86f, 0.93f, alpha)
		};

		for (int shade = 0; shade < shades.Length; shade++)
		{
			for (int cluster = 0; cluster < centersX.Length; cluster++)
			{
				int blobs = 3;
				for (int i = 0; i < blobs; i++)
				{
					float cx = x + (centersX[cluster] + Range(random, -9f, 9f) + shade * 1.5f) * size;
					float cy = y + (centersY[cluster] + Range(random, -6f, 6f) + shade * 2f) * size;
					float radius = Range(random, 8f, 13f) * size * (1f - shade * 0.17f);
					p.Circle(cx, cy, radius, shades[shade]);
				}
			}
		}
	}

	private static void PaintForest(Paper p, System.Random random)
	{
		p.Glow(90f, 140f, 110f, C(0.85f, 1f, 0.7f, 0.12f));
		for (int i = 0; i < 16; i++)
		{
			float x = i * 24f + Range(random, -6f, 6f);
			Pine(p, x, 40f, Range(random, 62f, 98f), C(0.06f, 0.15f, 0.08f), C(0.1f, 0.08f, 0.05f));
		}

		p.Ridge(x => 42f + 4f * Mathf.Sin(x * 0.03f), C(0.07f, 0.15f, 0.08f));
		for (int i = 0; i < 8; i++)
		{
			float x = 14f + i * 48f + Range(random, -8f, 8f);
			Pine(p, x, 26f, Range(random, 72f, 104f), C(0.09f, 0.23f, 0.11f), C(0.22f, 0.14f, 0.08f));
		}

		p.Ridge(x => 24f + 3f * Mathf.Sin(x * 0.04f) + 1.5f * Mathf.Sin(x * 0.17f), C(0.12f, 0.21f, 0.09f));
		p.Band(x => 10f + 3f * Mathf.Sin(x * 0.02f + 1f), 3.2f, C(0.27f, 0.18f, 0.1f, 1f));
		for (int i = 0; i < 9; i++)
		{
			float x = Range(random, 0f, Width);
			float y = Range(random, 16f, 24f);
			p.Circle(x, y, Range(random, 6f, 10f), C(0.14f, 0.31f, 0.13f));
			p.Circle(x + 5f, y + 2f, Range(random, 4f, 7f), C(0.2f, 0.42f, 0.17f));
		}

		SmallTree(p, 120f, 18f, 1f);
		SmallTree(p, 250f, 20f, 0.85f);
		SmallTree(p, 330f, 16f, 0.75f);
		Mushroom(p, 46f, 14f, 1f);
		Mushroom(p, 58f, 12f, 0.7f);
		Mushroom(p, 212f, 13f, 0.85f);
		Mushroom(p, 300f, 10f, 0.65f);
	}

	private static void Pine(Paper p, float x, float baseY, float height, Color leaves, Color trunk)
	{
		p.Rect(x - 2.4f, baseY - 3f, x + 2.4f, baseY + height * 0.2f, 0f, trunk);
		for (int tier = 0; tier < 3; tier++)
		{
			float bottom = baseY + height * (0.12f + 0.26f * tier);
			float top = Mathf.Min(baseY + height, baseY + height * (0.56f + 0.22f * tier));
			float half = height * 0.3f * (1f - 0.24f * tier);
			p.Polygon(new[] { x - half, bottom, x + half, bottom, x, top }, leaves);
		}
	}

	private static void SmallTree(Paper p, float x, float y, float size)
	{
		p.Rect(x - 2f * size, y, x + 2f * size, y + 18f * size, 1f, C(0.3f, 0.19f, 0.1f));
		p.Circle(x, y + 24f * size, 11f * size, C(0.16f, 0.36f, 0.15f));
		p.Circle(x - 7f * size, y + 19f * size, 8f * size, C(0.14f, 0.32f, 0.13f));
		p.Circle(x + 7f * size, y + 20f * size, 8f * size, C(0.2f, 0.43f, 0.18f));
		p.Circle(x + 2f * size, y + 28f * size, 6f * size, C(0.26f, 0.5f, 0.2f));
	}

	private static void Mushroom(Paper p, float x, float y, float size)
	{
		p.Rect(x - 1.4f * size, y - 6f * size, x + 1.4f * size, y + 1f * size, 0.8f, C(0.95f, 0.9f, 0.78f));
		p.HalfDisc(x, y, 6f * size, C(0.86f, 0.18f, 0.14f));
		p.Circle(x - 2.5f * size, y + 2.6f * size, 1f * size, C(1f, 1f, 1f, 0.95f));
		p.Circle(x + 2f * size, y + 3.6f * size, 0.9f * size, C(1f, 1f, 1f, 0.95f));
	}

	private static void PaintCanyon(Paper p, System.Random random)
	{
		p.Glow(108f, 80f, 96f, C(1f, 0.68f, 0.32f, 0.45f));
		p.Circle(108f, 80f, 24f, C(1f, 0.82f, 0.52f, 1f));
		Color far = C(0.6f, 0.3f, 0.18f);
		Mesa(p, -12f, 74f, 86f, 40f, far);
		Mesa(p, 150f, 232f, 98f, 40f, far);
		Mesa(p, 262f, 372f, 80f, 40f, far);
		p.Ridge(x => 44f + 3f * Mathf.Sin(x * 0.04f), C(0.55f, 0.27f, 0.16f));
		Color near = C(0.45f, 0.19f, 0.11f);
		Mesa(p, 36f, 150f, 64f, 20f, near);
		Mesa(p, 206f, 304f, 72f, 20f, near);
		p.Rect(44f, 50f, 142f, 52f, 0f, C(0.36f, 0.14f, 0.08f, 0.8f));
		p.Rect(214f, 56f, 296f, 58f, 0f, C(0.36f, 0.14f, 0.08f, 0.8f));
		p.Rect(42f, 38f, 145f, 40f, 0f, C(0.36f, 0.14f, 0.08f, 0.7f));
		p.Ridge(x => 22f + 3f * Mathf.Sin(x * 0.03f) + Mathf.Sin(x * 0.2f), C(0.72f, 0.48f, 0.3f));
		p.Ridge(x => 10f + 2f * Mathf.Sin(x * 0.05f + 1f), C(0.62f, 0.4f, 0.24f));
		Cactus(p, 176f, 16f, 1f);
		Cactus(p, 326f, 14f, 0.8f);
		Cactus(p, 22f, 12f, 0.65f);
		for (int i = 0; i < 18; i++)
		{
			p.Ellipse(Range(random, 0f, Width), Range(random, 2f, 18f), Range(random, 1.5f, 4f), Range(random, 1f, 2f), C(0.5f, 0.3f, 0.18f, 0.8f));
		}
	}

	private static void Mesa(Paper p, float left, float right, float top, float bottom, Color color)
	{
		p.Polygon(new[] { left, bottom, left + 9f, top, right - 9f, top, right, bottom }, color);
	}

	private static void Cactus(Paper p, float x, float y, float size)
	{
		Color green = C(0.2f, 0.32f, 0.15f);
		p.Capsule(x, y, x, y + 38f * size, 4.2f * size, green);
		p.Capsule(x - 2f * size, y + 16f * size, x - 11f * size, y + 16f * size, 3f * size, green);
		p.Capsule(x - 11f * size, y + 16f * size, x - 11f * size, y + 28f * size, 3f * size, green);
		p.Capsule(x + 2f * size, y + 22f * size, x + 10f * size, y + 22f * size, 3f * size, green);
		p.Capsule(x + 10f * size, y + 22f * size, x + 10f * size, y + 32f * size, 3f * size, green);
	}

	private static void PaintCave(Paper p, System.Random random)
	{
		p.Glow(64f, 40f, 70f, C(0.45f, 0.9f, 1f, 0.3f));
		p.Glow(196f, 34f, 60f, C(0.7f, 0.45f, 1f, 0.3f));
		p.Glow(312f, 44f, 70f, C(0.45f, 0.9f, 1f, 0.28f));
		p.Ridge(x => 40f + 8f * Mathf.Sin(x * 0.03f) + 4f * Mathf.Sin(x * 0.09f), C(0.09f, 0.08f, 0.2f));
		Cluster(p, random, 64f, 22f, 1f, C(0.38f, 0.88f, 1f, 1f));
		Cluster(p, random, 196f, 20f, 0.75f, C(0.72f, 0.46f, 1f, 1f));
		Cluster(p, random, 312f, 24f, 0.95f, C(0.42f, 0.92f, 1f, 1f));
		p.Ridge(x => 20f + 5f * Mathf.Sin(x * 0.04f) + 3f * Mathf.Sin(x * 0.13f), C(0.07f, 0.06f, 0.15f));
		for (int i = 0; i < 12; i++)
		{
			float x = Range(random, 0f, Width);
			Crystal(p, x, Range(random, 10f, 18f), Range(random, 8f, 16f), Range(random, 2f, 3.5f), Range(random, -0.4f, 0.4f),
				Pick(random, C(0.38f, 0.88f, 1f, 0.9f), C(0.72f, 0.46f, 1f, 0.9f)));
		}

		for (int i = 0; i < 16; i++)
		{
			p.Ellipse(Range(random, 0f, Width), Range(random, 2f, 10f), Range(random, 2f, 5f), Range(random, 1.5f, 3f), C(0.05f, 0.045f, 0.12f, 1f));
		}
	}

	private static void Cluster(Paper p, System.Random random, float x, float y, float size, Color color)
	{
		Crystal(p, x - 14f * size, y, 34f * size, 6f * size, -0.42f, color);
		Crystal(p, x + 15f * size, y, 38f * size, 6.5f * size, 0.38f, color);
		Crystal(p, x - 4f * size, y, 50f * size, 8f * size, -0.08f, color);
		Crystal(p, x + 6f * size, y, 30f * size, 5f * size, 0.16f, color);
		Crystal(p, x - 24f * size, y, 20f * size, 4f * size, -0.7f, color);
		Crystal(p, x + 26f * size, y, 22f * size, 4f * size, 0.72f, color);
	}

	private static void Crystal(Paper p, float x, float y, float height, float width, float angle, Color color)
	{
		float[] local = { -width, 0f, -width, height * 0.76f, 0f, height, width, height * 0.76f, width, 0f };
		float[] facet = { -width, 0f, -width, height * 0.76f, 0f, height, 0f, 0f };
		p.Polygon(Rotate(local, x, y, angle), color);
		p.Polygon(Rotate(facet, x, y, angle), C(1f, 1f, 1f, 0.28f));
		p.Polygon(Rotate(new[] { width * 0.2f, height * 0.1f, width * 0.2f, height * 0.7f, width * 0.55f, height * 0.62f, width * 0.55f, height * 0.1f }, x, y, angle), C(0.05f, 0.05f, 0.2f, 0.18f));
	}

	private static float[] Rotate(float[] points, float x, float y, float angle)
	{
		float cos = Mathf.Cos(angle);
		float sin = Mathf.Sin(angle);
		float[] result = new float[points.Length];
		for (int i = 0; i < points.Length; i += 2)
		{
			float px = points[i];
			float py = points[i + 1];
			result[i] = x + px * cos + py * sin;
			result[i + 1] = y - px * sin + py * cos;
		}

		return result;
	}

	private static void PaintBeach(Paper p, System.Random random)
	{
		p.Glow(304f, 116f, 54f, C(1f, 0.86f, 0.5f, 0.5f));
		p.Circle(304f, 116f, 16f, C(1f, 0.87f, 0.52f, 1f));
		p.Ridge(x => 60f, C(0.3f, 0.63f, 0.86f));
		p.Ridge(x => 52f + 1.5f * Mathf.Sin(x * 0.06f), C(0.25f, 0.56f, 0.82f));
		p.Band(x => 54f + 1.5f * Mathf.Sin(x * 0.08f), 0.8f, C(1f, 1f, 1f, 0.55f));
		p.Band(x => 46f + 1.5f * Mathf.Sin(x * 0.1f + 1f), 0.8f, C(1f, 1f, 1f, 0.45f));
		p.Band(x => 37f + 3f * Mathf.Sin(x * 0.05f), 2.4f, C(1f, 1f, 1f, 0.9f));
		p.Ridge(x => 35f + 3f * Mathf.Sin(x * 0.05f), C(0.97f, 0.86f, 0.66f));
		p.Ridge(x => 14f + 2f * Mathf.Sin(x * 0.04f + 2f), C(0.95f, 0.81f, 0.6f));
		Palm(p, 46f, 18f);
		Umbrella(p, 268f, 18f);
		p.Polygon(Star(178f, 12f, 7f, 3f), C(1f, 0.5f, 0.42f, 1f));
		p.Polygon(Star(212f, 8f, 5f, 2.2f), C(1f, 0.62f, 0.5f, 1f));
		for (int i = 0; i < 12; i++)
		{
			p.Ellipse(Range(random, 0f, Width), Range(random, 3f, 28f), Range(random, 1f, 2.2f), Range(random, 0.7f, 1.3f), C(0.88f, 0.74f, 0.55f, 0.9f));
		}
	}

	private static void Palm(Paper p, float x, float y)
	{
		Color trunk = C(0.58f, 0.39f, 0.22f);
		Vector2 start = new Vector2(x, y);
		Vector2 control = new Vector2(x + 2f, y + 50f);
		Vector2 end = new Vector2(x + 28f, y + 86f);
		Vector2 previous = start;
		for (int i = 1; i <= 16; i++)
		{
			float t = i / 16f;
			Vector2 point = Bezier(start, control, end, t);
			p.Capsule(previous.x, previous.y, point.x, point.y, Mathf.Lerp(5f, 3f, t), trunk);
			if (i % 2 == 0)
			{
				p.Capsule(point.x - 4f, point.y, point.x + 4f, point.y, 0.6f, C(0.44f, 0.28f, 0.15f, 0.9f));
			}

			previous = point;
		}

		float[] angles = { 160f, 130f, 95f, 55f, 20f, -10f };
		for (int i = 0; i < angles.Length; i++)
		{
			float radians = angles[i] * Mathf.Deg2Rad;
			Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
			Vector2 tip = end + direction * 40f + Vector2.down * 18f;
			Vector2 middle = end + direction * 24f + Vector2.up * 6f;
			Color leaf = i % 2 == 0 ? C(0.18f, 0.58f, 0.32f) : C(0.25f, 0.68f, 0.38f);
			for (int step = 0; step <= 14; step++)
			{
				float t = step / 14f;
				Vector2 point = Bezier(end, middle, tip, t);
				p.Circle(point.x, point.y, Mathf.Lerp(7.5f, 1.8f, t), leaf);
			}
		}

		p.Circle(end.x - 3f, end.y - 4f, 3.2f, C(0.42f, 0.27f, 0.14f));
		p.Circle(end.x + 3f, end.y - 5f, 3.2f, C(0.38f, 0.24f, 0.12f));
	}

	private static void Umbrella(Paper p, float x, float y)
	{
		p.Capsule(x, y - 6f, x - 4f, y + 52f, 1.2f, C(0.95f, 0.95f, 0.95f));
		float cx = x - 4f;
		float cy = y + 50f;
		p.HalfDisc(cx, cy, 30f, C(1f, 0.45f, 0.39f));
		for (int i = 0; i < 5; i += 2)
		{
			float a0 = Mathf.PI * i / 5f;
			float a1 = Mathf.PI * (i + 1) / 5f;
			p.Polygon(new[] { cx, cy, cx + Mathf.Cos(a0) * 30f, cy + Mathf.Sin(a0) * 30f, cx + Mathf.Cos((a0 + a1) * 0.5f) * 30f, cy + Mathf.Sin((a0 + a1) * 0.5f) * 30f, cx + Mathf.Cos(a1) * 30f, cy + Mathf.Sin(a1) * 30f }, C(1f, 0.97f, 0.92f, 1f));
		}
	}

	private static void PaintCity(Paper p, System.Random random)
	{
		p.Glow(80f, 70f, 100f, C(1f, 0.28f, 0.7f, 0.2f));
		p.Glow(290f, 70f, 100f, C(0.3f, 0.9f, 1f, 0.2f));
		float x = -6f;
		while (x < Width)
		{
			float width = Range(random, 18f, 30f);
			p.Rect(x, 18f, x + width - 2f, 18f + Range(random, 48f, 112f), 0f, C(0.13f, 0.12f, 0.17f));
			x += width;
		}

		x = -4f;
		Color[] lights = { C(0.35f, 0.95f, 1f, 0.85f), C(1f, 0.35f, 0.75f, 0.85f), C(1f, 0.85f, 0.5f, 0.7f) };
		int building = 0;
		while (x < Width)
		{
			float width = Range(random, 26f, 46f);
			float height = Range(random, 34f, 86f);
			p.Rect(x, 16f, x + width - 3f, 16f + height, 0f, C(0.065f, 0.065f, 0.09f));
			for (float wy = 24f; wy < 10f + height; wy += 9f)
			{
				for (float wx = x + 4f; wx < x + width - 8f; wx += 7f)
				{
					if (random.NextDouble() < 0.38)
					{
						p.Rect(wx, wy, wx + 3.4f, wy + 4.4f, 0.5f, lights[random.Next(lights.Length)]);
					}
				}
			}

			if (building % 2 == 0 && height > 50f)
			{
				Color neon = building % 4 == 0 ? C(1f, 0.3f, 0.72f, 1f) : C(0.3f, 0.95f, 1f, 1f);
				float signY = 16f + height - 18f;
				p.Glow(x + width * 0.5f, signY + 4f, 26f, new Color(neon.r, neon.g, neon.b, 0.35f));
				p.Rect(x + 5f, signY, x + width - 8f, signY + 9f, 3f, neon);
				p.Rect(x + 6.6f, signY + 1.6f, x + width - 9.6f, signY + 7.4f, 2f, C(0.08f, 0.06f, 0.1f, 1f));
			}

			x += width;
			building++;
		}

		p.Rect(0f, 0f, Width, 17f, 0f, C(0.05f, 0.05f, 0.07f));
		p.Band(x2 => 15.5f, 0.8f, C(1f, 0.3f, 0.72f, 0.75f));
		for (float dash = 6f; dash < Width; dash += 26f)
		{
			p.Rect(dash, 7f, dash + 12f, 8.4f, 0.6f, C(0.9f, 0.9f, 0.7f, 0.6f));
		}
	}

	private static void PaintClouds(Paper p, System.Random random)
	{
		p.Glow(292f, 118f, 64f, C(1f, 1f, 0.88f, 0.55f));
		for (int i = 0; i < 14; i++)
		{
			p.Circle(i * 28f + Range(random, -6f, 6f), Range(random, 48f, 66f), Range(random, 16f, 26f), C(0.8f, 0.88f, 1f));
		}

		for (int i = 0; i < 14; i++)
		{
			p.Circle(i * 28f + Range(random, -6f, 6f) + 12f, Range(random, 30f, 46f), Range(random, 16f, 26f), C(0.89f, 0.94f, 1f));
		}

		for (int i = 0; i < 12; i++)
		{
			p.Circle(i * 33f + Range(random, -6f, 6f), Range(random, 10f, 26f), Range(random, 18f, 30f), C(1f, 1f, 1f));
		}

		p.Rect(0f, 0f, Width, 16f, 0f, C(1f, 1f, 1f));
	}

	private static float[] Star(float x, float y, float outer, float inner)
	{
		float[] points = new float[20];
		for (int i = 0; i < 10; i++)
		{
			float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
			float radius = i % 2 == 0 ? outer : inner;
			points[i * 2] = x + Mathf.Cos(angle) * radius;
			points[i * 2 + 1] = y + Mathf.Sin(angle) * radius;
		}

		return points;
	}

	private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
	{
		float u = 1f - t;
		return u * u * a + 2f * u * t * b + t * t * c;
	}

	private static float Range(System.Random random, float min, float max)
	{
		return min + (float)random.NextDouble() * (max - min);
	}

	private static Color Pick(System.Random random, params Color[] colors)
	{
		return colors[random.Next(colors.Length)];
	}

	private static Color C(float r, float g, float b, float a = 1f)
	{
		return new Color(r, g, b, a);
	}

	private sealed class Paper
	{
		private readonly Color[] pixels;
		private readonly int width;
		private readonly int height;
		private readonly float scale;

		public Paper(int width, int height, float scale)
		{
			this.width = width;
			this.height = height;
			this.scale = scale;
			pixels = new Color[width * height];
		}

		public void Circle(float cx, float cy, float radius, Color color)
		{
			Fill(cx - radius, cy - radius, cx + radius, cy + radius, (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - radius, color);
		}

		public void Ellipse(float cx, float cy, float rx, float ry, Color color)
		{
			float minimum = Mathf.Min(rx, ry);
			Fill(cx - rx, cy - ry, cx + rx, cy + ry, (x, y) =>
			{
				float dx = (x - cx) / rx;
				float dy = (y - cy) / ry;
				return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * minimum;
			}, color);
		}

		public void HalfDisc(float cx, float cy, float radius, Color color)
		{
			Fill(cx - radius, cy, cx + radius, cy + radius, (x, y) =>
			{
				float circle = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - radius;
				return Mathf.Max(circle, cy - y);
			}, color);
		}

		public void Rect(float x0, float y0, float x1, float y1, float radius, Color color)
		{
			float cx = (x0 + x1) * 0.5f;
			float cy = (y0 + y1) * 0.5f;
			float hx = (x1 - x0) * 0.5f;
			float hy = (y1 - y0) * 0.5f;
			Fill(x0, y0, x1, y1, (x, y) =>
			{
				float dx = Mathf.Abs(x - cx) - (hx - radius);
				float dy = Mathf.Abs(y - cy) - (hy - radius);
				float ox = Mathf.Max(dx, 0f);
				float oy = Mathf.Max(dy, 0f);
				return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
			}, color);
		}

		public void Capsule(float ax, float ay, float bx, float by, float radius, Color color)
		{
			float lx = bx - ax;
			float ly = by - ay;
			float length = lx * lx + ly * ly;
			Fill(Mathf.Min(ax, bx) - radius, Mathf.Min(ay, by) - radius, Mathf.Max(ax, bx) + radius, Mathf.Max(ay, by) + radius, (x, y) =>
			{
				float px = x - ax;
				float py = y - ay;
				float t = length > 0f ? Mathf.Clamp01((px * lx + py * ly) / length) : 0f;
				float dx = px - lx * t;
				float dy = py - ly * t;
				return Mathf.Sqrt(dx * dx + dy * dy) - radius;
			}, color);
		}

		public void Polygon(float[] points, Color color)
		{
			float minX = float.MaxValue;
			float minY = float.MaxValue;
			float maxX = float.MinValue;
			float maxY = float.MinValue;
			for (int i = 0; i < points.Length; i += 2)
			{
				minX = Mathf.Min(minX, points[i]);
				maxX = Mathf.Max(maxX, points[i]);
				minY = Mathf.Min(minY, points[i + 1]);
				maxY = Mathf.Max(maxY, points[i + 1]);
			}

			Fill(minX, minY, maxX, maxY, (x, y) => SdPolygon(x, y, points), color);
		}

		public void Glow(float cx, float cy, float radius, Color color)
		{
			int x0 = Mathf.Max(0, Mathf.FloorToInt((cx - radius) * scale));
			int x1 = Mathf.Min(width - 1, Mathf.CeilToInt((cx + radius) * scale));
			int y0 = Mathf.Max(0, Mathf.FloorToInt((cy - radius) * scale));
			int y1 = Mathf.Min(height - 1, Mathf.CeilToInt((cy + radius) * scale));
			for (int row = y0; row <= y1; row++)
			{
				float y = (row + 0.5f) / scale;
				for (int column = x0; column <= x1; column++)
				{
					float x = (column + 0.5f) / scale;
					float distance = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / radius;
					if (distance >= 1f)
					{
						continue;
					}

					float falloff = 1f - distance;
					Blend(row * width + column, color, falloff * falloff);
				}
			}
		}

		public void Ridge(Func<float, float> top, Color color)
		{
			for (int column = 0; column < width; column++)
			{
				float x = (column + 0.5f) / scale;
				float surface = top(x);
				int rows = Mathf.Min(height - 1, Mathf.CeilToInt(surface * scale) + 1);
				for (int row = 0; row <= rows; row++)
				{
					float y = (row + 0.5f) / scale;
					float coverage = Mathf.Clamp01(0.5f - (y - surface) * scale);
					if (coverage > 0f)
					{
						Blend(row * width + column, color, coverage);
					}
				}
			}
		}

		public void Band(Func<float, float> center, float halfWidth, Color color)
		{
			for (int column = 0; column < width; column++)
			{
				float x = (column + 0.5f) / scale;
				float middle = center(x);
				int r0 = Mathf.Max(0, Mathf.FloorToInt((middle - halfWidth) * scale) - 1);
				int r1 = Mathf.Min(height - 1, Mathf.CeilToInt((middle + halfWidth) * scale) + 1);
				for (int row = r0; row <= r1; row++)
				{
					float y = (row + 0.5f) / scale;
					float coverage = Mathf.Clamp01(0.5f - (Mathf.Abs(y - middle) - halfWidth) * scale);
					if (coverage > 0f)
					{
						Blend(row * width + column, color, coverage);
					}
				}
			}
		}

		public void Finish(float fadeStart, float fadeEnd, float corner)
		{
			for (int row = 0; row < height; row++)
			{
				float y = (row + 0.5f) / scale;
				float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeStart, fadeEnd, y));
				for (int column = 0; column < width; column++)
				{
					float x = (column + 0.5f) / scale;
					float mask = fade * CornerMask(x, y, corner);
					int index = row * width + column;
					Color pixel = pixels[index];
					pixel.a *= mask;
					pixels[index] = pixel;
				}
			}
		}

		public int PixelWidth => width;

		public int PixelHeight => height;

		public Color[] Pixels => pixels;

		private float CornerMask(float x, float y, float radius)
		{
			if (y >= radius)
			{
				return 1f;
			}

			float right = width / scale;
			float cx;
			if (x < radius)
			{
				cx = radius;
			}
			else if (x > right - radius)
			{
				cx = right - radius;
			}
			else
			{
				return 1f;
			}

			float distance = Mathf.Sqrt((x - cx) * (x - cx) + (y - radius) * (y - radius));
			return Mathf.Clamp01(radius - distance + 0.5f);
		}

		private void Fill(float minX, float minY, float maxX, float maxY, Func<float, float, float> distance, Color color)
		{
			int x0 = Mathf.Max(0, Mathf.FloorToInt(minX * scale) - 1);
			int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(maxX * scale) + 1);
			int y0 = Mathf.Max(0, Mathf.FloorToInt(minY * scale) - 1);
			int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(maxY * scale) + 1);
			bool erase = color.a <= 0f;
			for (int row = y0; row <= y1; row++)
			{
				float y = (row + 0.5f) / scale;
				for (int column = x0; column <= x1; column++)
				{
					float x = (column + 0.5f) / scale;
					float coverage = Mathf.Clamp01(0.5f - distance(x, y) * scale);
					if (coverage <= 0f)
					{
						continue;
					}

					int index = row * width + column;
					if (erase)
					{
						Color pixel = pixels[index];
						pixel.a *= 1f - coverage;
						pixels[index] = pixel;
					}
					else
					{
						Blend(index, color, coverage);
					}
				}
			}
		}

		private void Blend(int index, Color color, float coverage)
		{
			float sourceAlpha = color.a * coverage;
			if (sourceAlpha <= 0f)
			{
				return;
			}

			Color destination = pixels[index];
			float outAlpha = sourceAlpha + destination.a * (1f - sourceAlpha);
			if (outAlpha <= 0.0001f)
			{
				return;
			}

			float keep = destination.a * (1f - sourceAlpha);
			pixels[index] = new Color(
				(color.r * sourceAlpha + destination.r * keep) / outAlpha,
				(color.g * sourceAlpha + destination.g * keep) / outAlpha,
				(color.b * sourceAlpha + destination.b * keep) / outAlpha,
				outAlpha);
		}

		private static float SdPolygon(float x, float y, float[] points)
		{
			int count = points.Length / 2;
			float dx0 = x - points[0];
			float dy0 = y - points[1];
			float squaredDistance = dx0 * dx0 + dy0 * dy0;
			float sign = 1f;
			int previous = count - 1;
			for (int i = 0; i < count; i++)
			{
				float ex = points[previous * 2] - points[i * 2];
				float ey = points[previous * 2 + 1] - points[i * 2 + 1];
				float wx = x - points[i * 2];
				float wy = y - points[i * 2 + 1];
				float lengthSquared = ex * ex + ey * ey;
				float t = lengthSquared > 0f ? Mathf.Clamp01((wx * ex + wy * ey) / lengthSquared) : 0f;
				float bx = wx - ex * t;
				float by = wy - ey * t;
				squaredDistance = Mathf.Min(squaredDistance, bx * bx + by * by);

				bool above = y >= points[i * 2 + 1];
				bool belowPrevious = y < points[previous * 2 + 1];
				bool left = ex * wy > ey * wx;
				if ((above && belowPrevious && left) || (!above && !belowPrevious && !left))
				{
					sign = -sign;
				}

				previous = i;
			}

			return sign * Mathf.Sqrt(squaredDistance);
		}
	}
}

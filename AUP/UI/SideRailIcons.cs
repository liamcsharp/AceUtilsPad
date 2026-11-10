using System;
using UnityEngine;

namespace AUP.UI;

internal static class SideRailIcons
{
	internal const int TextureSize = 128;

	internal static readonly string[] Ids =
	{
		"info", "bolt", "group", "bell", "play", "tag", "shirt",
		"sun", "palette", "gear", "wrench", "heart", "up", "down", "keys", "pc", "quest", "star", "crown", "phone", "camera", "grid", "home"
	};

	private static readonly float[] BoltPoints =
	{
		0.24f, 0.87f, -0.49f, -0.09f, -0.06f, -0.09f, -0.24f, -0.87f, 0.49f, 0.11f, 0.07f, 0.11f
	};

	private static readonly float[] PlayPoints = { -0.2f, 0.42f, -0.2f, -0.42f, 0.44f, 0f };

	private static readonly float[] TagPoints = { 0.84f, 0.5f, 0.84f, -0.5f, -0.18f, -0.5f, -0.84f, 0f, -0.18f, 0.5f };

	private static readonly float[] ShirtPoints =
	{
		-0.30f, 0.78f, -0.88f, 0.48f, -0.66f, 0.08f, -0.46f, 0.22f, -0.46f, -0.80f, 0.46f, -0.80f,
		0.46f, 0.22f, 0.66f, 0.08f, 0.88f, 0.48f, 0.30f, 0.78f, 0f, 0.50f
	};

	private static readonly float[] BellBodyPoints = { -0.5f, 0.18f, 0.5f, 0.18f, 0.74f, -0.42f, -0.74f, -0.42f };

	private static readonly float[] StarPoints = BuildStar();

	private static readonly float[] UpPoints = { -0.7f, -0.35f, 0.7f, -0.35f, 0f, 0.4f };

	private static readonly float[] DownPoints = { -0.7f, 0.35f, 0.7f, 0.35f, 0f, -0.4f };

	internal static Color32[] Render(string id, int size)
	{
		if (Array.IndexOf(Ids, id) < 0)
		{
			return null;
		}

		Color32[] pixels = new Color32[size * size];
		float step = 2f / size;
		for (int row = 0; row < size; row++)
		{
			float y = (row + 0.5f) * step - 1f;
			for (int column = 0; column < size; column++)
			{
				float x = (column + 0.5f) * step - 1f;
				float coverage = Mathf.Clamp01(0.5f - Distance(id, x, y) / step);
				pixels[row * size + column] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
			}
		}

		return pixels;
	}

	private static float Distance(string id, float x, float y)
	{
		switch (id)
		{
			case "info": return IconInfo(x, y);
			case "bolt": return SdPolygon(x, y, BoltPoints) - 0.04f;
			case "group": return IconGroup(x, y);
			case "bell": return IconBell(x, y);
			case "play": return IconPlay(x, y);
			case "tag": return IconTag(x, y);
			case "shirt": return SdPolygon(x, y, ShirtPoints) - 0.04f;
			case "sun": return IconSun(x, y);
			case "palette": return IconPalette(x, y);
			case "gear": return IconGear(x, y);
			case "wrench": return IconWrench(x, y);
			case "heart": return IconHeart(x, y);
			case "keys": return IconKeys(x, y);
			case "pc": return IconPc(x, y);
			case "quest": return IconQuest(x, y);
			case "star": return SdPolygon(x, y, StarPoints) - 0.05f;
			case "crown": return IconCrown(x, y);
			case "phone": return IconPhone(x, y);
			case "camera": return IconCamera(x, y);
			case "grid": return IconGrid(x, y);
			case "home": return IconHome(x, y);
			case "up": return SdPolygon(x, y, UpPoints) - 0.08f;
			case "down": return SdPolygon(x, y, DownPoints) - 0.08f;
			default: return 1f;
		}
	}

	private static float Subtract(float shape, float cut)
	{
		return Mathf.Max(shape, -cut);
	}

	private static float RotatedX(float x, float y, float angle)
	{
		return Mathf.Cos(angle) * x + Mathf.Sin(angle) * y;
	}

	private static float RotatedY(float x, float y, float angle)
	{
		return -Mathf.Sin(angle) * x + Mathf.Cos(angle) * y;
	}

	private static float SdCircle(float x, float y, float centerX, float centerY, float radius)
	{
		float dx = x - centerX;
		float dy = y - centerY;
		return Mathf.Sqrt(dx * dx + dy * dy) - radius;
	}

	private static float SdBox(float x, float y, float centerX, float centerY, float halfWidth, float halfHeight, float rounding)
	{
		float dx = Mathf.Abs(x - centerX) - (halfWidth - rounding);
		float dy = Mathf.Abs(y - centerY) - (halfHeight - rounding);
		float outsideX = Mathf.Max(dx, 0f);
		float outsideY = Mathf.Max(dy, 0f);
		return Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY) + Mathf.Min(Mathf.Max(dx, dy), 0f) - rounding;
	}

	private static float SdCapsule(float x, float y, float startX, float startY, float endX, float endY, float radius)
	{
		float px = x - startX;
		float py = y - startY;
		float lx = endX - startX;
		float ly = endY - startY;
		float t = Mathf.Clamp01((px * lx + py * ly) / (lx * lx + ly * ly));
		float dx = px - lx * t;
		float dy = py - ly * t;
		return Mathf.Sqrt(dx * dx + dy * dy) - radius;
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
			float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey));
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

	private static float IconInfo(float x, float y)
	{
		float shape = SdCircle(x, y, 0f, 0f, 0.88f);
		shape = Subtract(shape, SdCircle(x, y, 0f, 0.46f, 0.13f));
		return Subtract(shape, SdBox(x, y, 0f, -0.12f, 0.12f, 0.34f, 0.05f));
	}

	private static float IconGroup(float x, float y)
	{
		float front = SdCircle(x, y, -0.26f, 0.42f, 0.28f);
		front = Mathf.Min(front, Subtract(SdCircle(x, y, -0.26f, -0.84f, 0.66f), SdBox(x, y, 0f, -1.24f, 2f, 0.5f, 0f)));
		float back = SdCircle(x, y, 0.44f, 0.46f, 0.24f);
		back = Mathf.Min(back, Subtract(SdCircle(x, y, 0.44f, -0.84f, 0.48f), SdBox(x, y, 0f, -1.24f, 2f, 0.5f, 0f)));
		back = Subtract(back, front - 0.1f);
		return Mathf.Min(front, back);
	}

	private static float IconBell(float x, float y)
	{
		float shape = Mathf.Min(SdCircle(x, y, 0f, 0.18f, 0.5f), SdPolygon(x, y, BellBodyPoints));
		shape = Mathf.Min(shape, SdCircle(x, y, 0f, 0.74f, 0.1f));
		return Mathf.Min(shape, SdCircle(x, y, 0f, -0.62f, 0.15f));
	}

	private static float IconPlay(float x, float y)
	{
		float ring = Subtract(SdCircle(x, y, 0f, 0f, 0.88f), SdCircle(x, y, 0f, 0f, 0.72f));
		return Mathf.Min(ring, SdPolygon(x, y, PlayPoints) - 0.06f);
	}

	private static float IconTag(float x, float y)
	{
		float shape = SdPolygon(x, y, TagPoints) - 0.08f;
		shape = Subtract(shape, SdCircle(x, y, -0.34f, 0f, 0.12f));
		return Subtract(shape, SdCapsule(x, y, 0.02f, 0f, 0.5f, 0f, 0.07f));
	}

	private static float IconSun(float x, float y)
	{
		float shape = SdCircle(x, y, 0f, 0f, 0.36f);
		for (int i = 0; i < 8; i++)
		{
			float angle = i * Mathf.PI / 4f;
			float cos = Mathf.Cos(angle);
			float sin = Mathf.Sin(angle);
			shape = Mathf.Min(shape, SdCapsule(x, y, cos * 0.58f, sin * 0.58f, cos * 0.82f, sin * 0.82f, 0.08f));
		}

		return shape;
	}

	private static float IconPalette(float x, float y)
	{
		float shape = SdCircle(x, y, 0f, 0f, 0.88f);
		shape = Subtract(shape, SdCircle(x, y, -0.3f, 0.32f, 0.14f));
		shape = Subtract(shape, SdCircle(x, y, 0.12f, 0.46f, 0.14f));
		shape = Subtract(shape, SdCircle(x, y, 0.5f, 0.12f, 0.14f));
		shape = Subtract(shape, SdCircle(x, y, -0.5f, -0.1f, 0.14f));
		return Subtract(shape, SdCircle(x, y, 0.2f, -0.52f, 0.24f));
	}

	private static float IconGear(float x, float y)
	{
		float angle = Mathf.Atan2(y, x);
		float radius = Mathf.Sqrt(x * x + y * y);
		float toothStep = Mathf.PI / 4f;
		float local = Mathf.Repeat(angle + toothStep * 0.5f, toothStep) - toothStep * 0.5f;
		float toothX = radius * Mathf.Cos(local);
		float toothY = radius * Mathf.Sin(local);
		float shape = Mathf.Min(SdCircle(x, y, 0f, 0f, 0.62f), SdBox(toothX, toothY, 0.7f, 0f, 0.2f, 0.17f, 0.05f));
		return Subtract(shape, SdCircle(x, y, 0f, 0f, 0.27f));
	}

	private static float IconWrench(float x, float y)
	{
		const float scale = 0.94f;
		float sx = (x + 0.04f) / scale;
		float sy = (y + 0.04f) / scale;
		float rx = RotatedX(sx, sy, -Mathf.PI / 4f);
		float ry = RotatedY(sx, sy, -Mathf.PI / 4f);
		float handle = SdCapsule(rx, ry, 0f, -1.02f, 0f, 0.25f, 0.2f);
		float head = SdCircle(rx, ry, 0f, 0.72f, 0.5f);
		head = Subtract(head, SdBox(rx, ry, 0f, 1.15f, 0.19f, 0.55f, 0f));
		return Mathf.Min(handle, head) * scale;
	}

	private static float IconKeys(float x, float y)
	{
		float shape = Subtract(SdBox(x, y, 0f, 0f, 0.92f, 0.6f, 0.16f), SdBox(x, y, 0f, 0f, 0.76f, 0.44f, 0.08f));
		for (int row = 0; row < 2; row++)
		{
			for (int column = 0; column < 4; column++)
			{
				shape = Mathf.Min(shape, SdBox(x, y, -0.48f + column * 0.32f, 0.2f - row * 0.26f, 0.09f, 0.08f, 0.03f));
			}
		}

		return Mathf.Min(shape, SdBox(x, y, 0f, -0.28f, 0.42f, 0.07f, 0.03f));
	}

	private static float IconPc(float x, float y)
	{
		float screen = Subtract(SdBox(x, y, 0f, 0.26f, 0.9f, 0.6f, 0.12f), SdBox(x, y, 0f, 0.3f, 0.7f, 0.42f, 0.04f));
		float neck = SdBox(x, y, 0f, -0.46f, 0.12f, 0.16f, 0f);
		float stand = SdCapsule(x, y, -0.42f, -0.66f, 0.42f, -0.66f, 0.1f);
		return Mathf.Min(screen, Mathf.Min(neck, stand));
	}

	private static float IconQuest(float x, float y)
	{
		float visor = SdBox(x, y, 0f, 0.02f, 0.84f, 0.46f, 0.3f);
		float strap = SdBox(x, y, 0f, 0.08f, 0.98f, 0.1f, 0.05f);
		float shape = Mathf.Min(visor, strap);
		shape = Subtract(shape, SdCircle(x, y, -0.5f, 0.22f, 0.09f));
		shape = Subtract(shape, SdCircle(x, y, 0.5f, 0.22f, 0.09f));
		shape = Subtract(shape, SdCircle(x, y, -0.5f, -0.2f, 0.09f));
		shape = Subtract(shape, SdCircle(x, y, 0.5f, -0.2f, 0.09f));
		return Subtract(shape, SdBox(x, y, 0f, -0.46f, 0.18f, 0.12f, 0.08f));
	}

	private static readonly float[] CrownPoints =
	{
		-0.82f, -0.34f, -0.9f, 0.46f, -0.42f, 0.02f, 0f, 0.7f, 0.42f, 0.02f, 0.9f, 0.46f, 0.82f, -0.34f
	};

	private static readonly float[] RoofPoints = { -0.92f, 0.02f, 0f, 0.86f, 0.92f, 0.02f };

	private static float IconCrown(float x, float y)
	{
		float shape = SdPolygon(x, y, CrownPoints) - 0.04f;
		shape = Mathf.Min(shape, SdBox(x, y, 0f, -0.6f, 0.82f, 0.12f, 0.05f));
		shape = Mathf.Min(shape, SdCircle(x, y, -0.9f, 0.54f, 0.1f));
		shape = Mathf.Min(shape, SdCircle(x, y, 0.9f, 0.54f, 0.1f));
		return Mathf.Min(shape, SdCircle(x, y, 0f, 0.8f, 0.1f));
	}

	private static float IconPhone(float x, float y)
	{
		float shape = Subtract(SdBox(x, y, 0f, 0f, 0.52f, 0.92f, 0.18f), SdBox(x, y, 0f, 0.08f, 0.38f, 0.62f, 0.06f));
		return Mathf.Min(shape, SdBox(x, y, 0f, -0.72f, 0.16f, 0.05f, 0.04f));
	}

	private static float IconCamera(float x, float y)
	{
		float shape = Mathf.Min(SdBox(x, y, 0f, -0.08f, 0.9f, 0.58f, 0.16f), SdBox(x, y, -0.3f, 0.56f, 0.28f, 0.14f, 0.06f));
		shape = Subtract(shape, SdCircle(x, y, 0f, -0.1f, 0.36f));
		return Mathf.Min(shape, SdCircle(x, y, 0f, -0.1f, 0.2f));
	}

	private static float IconGrid(float x, float y)
	{
		float shape = SdBox(x, y, -0.46f, 0.46f, 0.36f, 0.36f, 0.1f);
		shape = Mathf.Min(shape, SdBox(x, y, 0.46f, 0.46f, 0.36f, 0.36f, 0.1f));
		shape = Mathf.Min(shape, SdBox(x, y, -0.46f, -0.46f, 0.36f, 0.36f, 0.1f));
		return Mathf.Min(shape, SdCircle(x, y, 0.46f, -0.46f, 0.36f));
	}

	private static float IconHome(float x, float y)
	{
		float shape = Mathf.Min(SdPolygon(x, y, RoofPoints) - 0.05f, SdBox(x, y, 0f, -0.42f, 0.62f, 0.46f, 0.06f));
		return Subtract(shape, SdBox(x, y, 0f, -0.6f, 0.17f, 0.28f, 0.03f));
	}

	private static float[] BuildStar()
	{
		float[] points = new float[20];
		for (int i = 0; i < 10; i++)
		{
			float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
			float radius = i % 2 == 0 ? 0.92f : 0.4f;
			points[i * 2] = Mathf.Cos(angle) * radius;
			points[i * 2 + 1] = Mathf.Sin(angle) * radius - 0.06f;
		}

		return points;
	}

	private static float IconHeart(float x, float y)
	{
		float rx = RotatedX(x, y + 0.1f, Mathf.PI / 4f);
		float ry = RotatedY(x, y + 0.1f, Mathf.PI / 4f);
		float shape = SdBox(rx, ry, 0f, 0f, 0.5f, 0.5f, 0f);
		shape = Mathf.Min(shape, SdCircle(x, y, -0.354f, 0.254f, 0.5f));
		return Mathf.Min(shape, SdCircle(x, y, 0.354f, 0.254f, 0.5f));
	}
}

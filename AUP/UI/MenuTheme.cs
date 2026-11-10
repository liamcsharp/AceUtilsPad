using System;
using System.Collections.Generic;
using UnityEngine;

namespace AUP.UI;

public enum ParticleStyle
{
	None,
	Embers,
	Petals,
	Leaves,
	Dust,
	Crystals,
	Glints,
	Rain,
	Fluff
}

public enum SceneryStyle
{
	None,
	Lava,
	Sakura,
	Forest,
	Canyon,
	Cave,
	Beach,
	City,
	Clouds
}

public delegate Color ColorPick(MenuTheme theme);

public static class Pick
{
	public static readonly ColorPick Panel = theme => theme.Panel;
	public static readonly ColorPick Rail = theme => theme.Rail;
	public static readonly ColorPick Element = theme => theme.Element;
	public static readonly ColorPick Accent = theme => theme.Accent;
	public static readonly ColorPick Text = theme => theme.Text;
	public static readonly ColorPick Muted = theme => theme.Muted;
	public static readonly ColorPick Separator = theme => theme.Separator;
}

public sealed class MenuTheme
{
	public string Name;
	public string Description;
	public Color Panel;
	public Color Rail;
	public Color Element;
	public Color Accent;
	public Color Glow;
	public Color Text = Color.white;
	public Color ElementText = Color.white;
	public Color Muted = new Color(0.78f, 0.8f, 0.92f, 1f);
	public Color Separator;
	public Color ParticleColor;
	public Color[] Gradient;
	public float GradientSpeed = 0.035f;
	public float SceneryAlpha = 0.65f;
	public ParticleStyle Particles;
	public SceneryStyle Scenery;
	public bool IsLight;

	public bool IsAnimated => Gradient != null && Gradient.Length > 1;

	public Color OnAccent => Luminance(Accent) > 0.62f ? new Color(0.06f, 0.06f, 0.12f, 1f) : Color.white;

	private static float Luminance(Color color)
	{
		return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
	}
}

public static class ThemeLibrary
{
	public static readonly IReadOnlyList<MenuTheme> All = new List<MenuTheme>
	{
		Lava(),
		Sakura(),
		Forest(),
		Canyon(),
		Cave(),
		Beach(),
		City(),
		Clouds()
	};

	public static MenuTheme Find(string name)
	{
		foreach (MenuTheme theme in All)
		{
			if (string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return theme;
			}
		}

		return All[0];
	}

	private static MenuTheme Lava()
	{
		MenuTheme theme = Dark("Lava", "Deep black-red with molten orange glow.",
			Rgb(0.075f, 0.022f, 0.02f), Rgb(0.055f, 0.016f, 0.016f), Rgb(0.17f, 0.055f, 0.04f), Rgb(1f, 0.47f, 0.13f));
		theme.Muted = Rgb(0.93f, 0.72f, 0.62f);
		theme.Gradient = new[] { Rgb(0.09f, 0.02f, 0.02f), Rgb(0.2f, 0.045f, 0.02f), Rgb(0.07f, 0.015f, 0.02f) };
		theme.Particles = ParticleStyle.Embers;
		theme.ParticleColor = Rgb(1f, 0.55f, 0.15f);
		theme.Scenery = SceneryStyle.Lava;
		return theme;
	}

	private static MenuTheme Sakura()
	{
		MenuTheme theme = Dark("Sakura", "Soft rose dusk with blossoming cherry trees.",
			Rgb(0.2f, 0.075f, 0.13f), Rgb(0.15f, 0.05f, 0.1f), Rgb(0.32f, 0.12f, 0.21f), Rgb(1f, 0.64f, 0.8f));
		theme.Muted = Rgb(0.98f, 0.8f, 0.88f);
		theme.Gradient = new[] { Rgb(0.23f, 0.08f, 0.16f), Rgb(0.33f, 0.13f, 0.24f), Rgb(0.17f, 0.06f, 0.14f) };
		theme.Particles = ParticleStyle.Petals;
		theme.ParticleColor = Rgb(1f, 0.74f, 0.86f);
		theme.Scenery = SceneryStyle.Sakura;
		return theme;
	}

	private static MenuTheme Forest()
	{
		MenuTheme theme = Dark("Forest", "Mossy greens and bark browns under tall pines.",
			Rgb(0.065f, 0.12f, 0.075f), Rgb(0.05f, 0.085f, 0.055f), Rgb(0.2f, 0.14f, 0.085f), Rgb(0.58f, 0.86f, 0.36f));
		theme.Muted = Rgb(0.78f, 0.88f, 0.72f);
		theme.Gradient = new[] { Rgb(0.07f, 0.14f, 0.08f), Rgb(0.11f, 0.19f, 0.1f), Rgb(0.1f, 0.09f, 0.055f) };
		theme.Particles = ParticleStyle.Leaves;
		theme.ParticleColor = Rgb(0.62f, 0.84f, 0.3f);
		theme.Scenery = SceneryStyle.Forest;
		return theme;
	}

	private static MenuTheme Canyon()
	{
		MenuTheme theme = Dark("Canyon", "Dusty reds and tan in a warm sunset.",
			Rgb(0.27f, 0.12f, 0.075f), Rgb(0.2f, 0.085f, 0.055f), Rgb(0.42f, 0.21f, 0.12f), Rgb(1f, 0.77f, 0.45f));
		theme.Muted = Rgb(0.98f, 0.85f, 0.72f);
		theme.Gradient = new[] { Rgb(0.42f, 0.17f, 0.1f), Rgb(0.28f, 0.11f, 0.075f), Rgb(0.5f, 0.27f, 0.14f) };
		theme.Particles = ParticleStyle.Dust;
		theme.ParticleColor = Rgb(1f, 0.85f, 0.62f);
		theme.Scenery = SceneryStyle.Canyon;
		return theme;
	}

	private static MenuTheme Cave()
	{
		MenuTheme theme = Dark("Cave", "Dark purple-blue with glowing crystals.",
			Rgb(0.06f, 0.055f, 0.14f), Rgb(0.04f, 0.04f, 0.1f), Rgb(0.12f, 0.1f, 0.25f), Rgb(0.45f, 0.95f, 1f));
		theme.Muted = Rgb(0.72f, 0.76f, 0.95f);
		theme.Gradient = new[] { Rgb(0.07f, 0.06f, 0.18f), Rgb(0.12f, 0.065f, 0.24f), Rgb(0.04f, 0.08f, 0.18f) };
		theme.Particles = ParticleStyle.Crystals;
		theme.ParticleColor = Rgb(0.55f, 0.95f, 1f);
		theme.Scenery = SceneryStyle.Cave;
		return theme;
	}

	private static MenuTheme Beach()
	{
		MenuTheme theme = Light("Beach", "Sandy cream, sky blue and coral.",
			Rgb(0.98f, 0.93f, 0.82f), Rgb(0.93f, 0.85f, 0.7f), Rgb(0.33f, 0.66f, 0.87f), Rgb(1f, 0.45f, 0.39f));
		theme.Text = Rgb(0.17f, 0.19f, 0.25f);
		theme.Muted = Rgb(0.4f, 0.42f, 0.48f);
		theme.Gradient = new[] { Rgb(0.74f, 0.89f, 0.98f), Rgb(0.98f, 0.93f, 0.82f), Rgb(0.99f, 0.87f, 0.73f) };
		theme.Particles = ParticleStyle.Glints;
		theme.ParticleColor = Color.white;
		theme.Scenery = SceneryStyle.Beach;
		return theme;
	}

	private static MenuTheme City()
	{
		MenuTheme theme = Dark("City", "Dark gray streets lit by pink and cyan neon.",
			Rgb(0.08f, 0.08f, 0.1f), Rgb(0.055f, 0.055f, 0.075f), Rgb(0.15f, 0.14f, 0.19f), Rgb(1f, 0.27f, 0.68f));
		theme.Muted = Rgb(0.6f, 0.92f, 1f);
		theme.Gradient = new[] { Rgb(0.09f, 0.08f, 0.12f), Rgb(0.14f, 0.07f, 0.16f), Rgb(0.06f, 0.1f, 0.14f) };
		theme.Particles = ParticleStyle.Rain;
		theme.ParticleColor = Rgb(0.35f, 0.95f, 1f);
		theme.Scenery = SceneryStyle.City;
		return theme;
	}

	private static MenuTheme Clouds()
	{
		MenuTheme theme = Light("Clouds", "Soft white and sky blue for a clean look.",
			Rgb(0.93f, 0.96f, 1f), Rgb(0.83f, 0.9f, 0.99f), Rgb(0.42f, 0.62f, 0.93f), Rgb(0.2f, 0.5f, 0.95f));
		theme.Text = Rgb(0.12f, 0.18f, 0.3f);
		theme.Muted = Rgb(0.36f, 0.44f, 0.58f);
		theme.Gradient = new[] { Rgb(0.78f, 0.88f, 1f), Rgb(0.95f, 0.97f, 1f), Rgb(0.7f, 0.84f, 0.99f) };
		theme.Particles = ParticleStyle.Fluff;
		theme.ParticleColor = Color.white;
		theme.Scenery = SceneryStyle.Clouds;
		return theme;
	}

	private static MenuTheme Dark(string name, string description, Color panel, Color rail, Color element, Color accent)
	{
		return new MenuTheme
		{
			Name = name,
			Description = description,
			Panel = panel,
			Rail = rail,
			Element = element,
			Accent = accent,
			Glow = WithAlpha(accent, 0.5f),
			Separator = WithAlpha(accent, 0.45f),
			ParticleColor = accent
		};
	}

	private static MenuTheme Light(string name, string description, Color panel, Color rail, Color element, Color accent)
	{
		MenuTheme theme = Dark(name, description, panel, rail, element, accent);
		theme.IsLight = true;
		theme.SceneryAlpha = 0.8f;
		theme.Separator = WithAlpha(accent, 0.55f);
		return theme;
	}

	private static Color Rgb(float r, float g, float b)
	{
		return new Color(r, g, b, 1f);
	}

	private static Color WithAlpha(Color color, float alpha)
	{
		return new Color(color.r, color.g, color.b, alpha);
	}
}

using UnityEngine;

namespace AUP.UI;

public sealed class ThemeController
{
	private readonly MenuSettings settings;

	public ThemeController(MenuSettings settings)
	{
		this.settings = settings;
	}

	public MenuTheme Current => ThemeLibrary.Find(settings.ThemeName.Value);

	public float Opacity => Mathf.Clamp(settings.Opacity.Value, MenuSettings.MinOpacity, MenuSettings.MaxOpacity);

	public bool AnimationEnabled => settings.AnimatedThemes.Value && PerformanceGuard.Level == 0;

	public bool ParticlesEnabled => settings.ThemeParticles.Value && PerformanceGuard.Level == 0;

	public bool SceneryEnabled => settings.ThemeScenery.Value;

	public bool MotionEnabled => settings.MenuAnimations.Value && PerformanceGuard.Level < 2;

	public void Select(string themeName)
	{
		settings.ThemeName.Value = themeName;
	}

	public bool IsSelected(MenuTheme theme)
	{
		return ReferenceEquals(Current, theme);
	}
}

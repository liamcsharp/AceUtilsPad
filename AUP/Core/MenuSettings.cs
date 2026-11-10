using System;
using BepInEx.Configuration;

namespace AUP;

public enum NameColorMode
{
	Plain,
	Platform,
	Fps,
	Ping,
	AccountAge
}

public sealed class TagSettings
{
	public static readonly int[] NewAccountDayPresets = { 3, 7, 14, 30, 60, 90, 180, 365 };

	public const float MinFontSize = 10f;
	public const float MaxFontSize = 40f;
	public const float MinDistance = 5f;
	public const float MaxDistanceLimit = 60f;

	public readonly ConfigEntry<bool> Enabled;
	public readonly ConfigEntry<bool> ShowFps;
	public readonly ConfigEntry<bool> ShowPlatform;
	public readonly ConfigEntry<bool> ShowAccountAge;
	public readonly ConfigEntry<bool> ShowPing;
	public readonly ConfigEntry<bool> ShowNewBadge;
	public readonly ConfigEntry<bool> ColorValues;
	public readonly ConfigEntry<bool> FadeEnabled;
	public readonly ConfigEntry<float> FontSize;
	public readonly ConfigEntry<float> MaxDistance;
	public readonly ConfigEntry<int> NewAccountDays;
	public readonly ConfigEntry<int> NameTint;
	public readonly ConfigEntry<int> NameColor;
	public readonly ConfigEntry<bool> ShowFriendStar;
	public readonly ConfigEntry<bool> ShowNotes;
	public readonly ConfigEntry<bool> ShowCustomTags;
	public readonly ConfigEntry<bool> Background;
	public readonly ConfigEntry<bool> ShowOwnTag;
	public readonly ConfigEntry<string> SelfTag;
	public readonly ConfigEntry<int> SelfTagColor;

	public TagSettings(ConfigFile file)
	{
		Enabled = file.Bind("NameTags", "Enabled", false, "Show name tags above players.");
		ShowFps = file.Bind("NameTags", "ShowFps", true, "Show the player's FPS.");
		ShowPlatform = file.Bind("NameTags", "ShowPlatform", true, "Show the player's platform.");
		ShowAccountAge = file.Bind("NameTags", "ShowAccountAge", true, "Show how old the account is.");
		ShowPing = file.Bind("NameTags", "ShowPing", true, "Show the estimated player ping.");
		ShowNewBadge = file.Bind("NameTags", "ShowNewBadge", true, "Flag young accounts with a NEW badge.");
		ColorValues = file.Bind("NameTags", "ColorValues", true, "Color FPS, ping, platform and age values by quality.");
		FadeEnabled = file.Bind("NameTags", "FadeEnabled", true, "Fade tags out before the distance limit.");
		FontSize = file.Bind("NameTags", "FontSize", 20f, "Tag font size.");
		MaxDistance = file.Bind("NameTags", "MaxDistance", 25f, "Tags farther than this are hidden.");
		NewAccountDays = file.Bind("NameTags", "NewAccountDays", 30, "Accounts younger than this many days get the NEW badge.");
		NameTint = file.Bind("NameTags", "NameTint", 0, "Name tint index used by the Plain color rule.");
		NameColor = file.Bind("NameTags", "NameColorRule", (int)NameColorMode.Plain, "What decides the name color.");
		ShowFriendStar = file.Bind("NameTags", "ShowFriendStar", true, "Show a star on friends and starred players.");
		ShowNotes = file.Bind("NameTags", "ShowNotes", true, "Show your own notes under player names.");
		ShowCustomTags = file.Bind("NameTags", "ShowCustomTags", true, "Show the custom tags other AceUtilsPad users set for themselves.");
		Background = file.Bind("NameTags", "Background", true, "Draw a dark card behind each tag so names stay readable.");
		ShowOwnTag = file.Bind("NameTags", "ShowOwnTag", false, "Show a tag above your own player too.");
		SelfTag = file.Bind("NameTags", "SelfTag", string.Empty, "Your custom tag. Other AceUtilsPad users see it on your name tag.");
		SelfTagColor = file.Bind("NameTags", "SelfTagColor", 0, "Color index for your custom tag.");

		Enabled.SettingChanged += OnAnySettingChanged;
		ShowFps.SettingChanged += OnAnySettingChanged;
		ShowPlatform.SettingChanged += OnAnySettingChanged;
		ShowAccountAge.SettingChanged += OnAnySettingChanged;
		ShowPing.SettingChanged += OnAnySettingChanged;
		ShowNewBadge.SettingChanged += OnAnySettingChanged;
		ColorValues.SettingChanged += OnAnySettingChanged;
		FadeEnabled.SettingChanged += OnAnySettingChanged;
		FontSize.SettingChanged += OnAnySettingChanged;
		MaxDistance.SettingChanged += OnAnySettingChanged;
		NewAccountDays.SettingChanged += OnAnySettingChanged;
		NameTint.SettingChanged += OnAnySettingChanged;
		NameColor.SettingChanged += OnAnySettingChanged;
		ShowFriendStar.SettingChanged += OnAnySettingChanged;
		ShowNotes.SettingChanged += OnAnySettingChanged;
		ShowCustomTags.SettingChanged += OnAnySettingChanged;
		Background.SettingChanged += OnAnySettingChanged;
		ShowOwnTag.SettingChanged += OnAnySettingChanged;
		SelfTag.SettingChanged += OnAnySettingChanged;
		SelfTagColor.SettingChanged += OnAnySettingChanged;
	}

	public void BumpVersion()
	{
		Version++;
	}

	public int Version { get; private set; }

	public NameColorMode ColorMode
	{
		get => (NameColorMode)Math.Max(0, Math.Min(NameColor.Value, (int)NameColorMode.AccountAge));
		set => NameColor.Value = (int)value;
	}

	public void CycleColorMode()
	{
		int modeCount = Enum.GetValues(typeof(NameColorMode)).Length;
		ColorMode = (NameColorMode)(((int)ColorMode + 1) % modeCount);
	}

	public void StepFontSize(float delta)
	{
		FontSize.Value = Clamp(FontSize.Value + delta, MinFontSize, MaxFontSize);
	}

	public void StepDistance(float delta)
	{
		MaxDistance.Value = Clamp(MaxDistance.Value + delta, MinDistance, MaxDistanceLimit);
	}

	public void StepNewAccountDays(int direction)
	{
		int index = Array.IndexOf(NewAccountDayPresets, NewAccountDays.Value);
		if (index < 0)
		{
			index = Array.FindIndex(NewAccountDayPresets, preset => preset >= NewAccountDays.Value);
			index = index < 0 ? NewAccountDayPresets.Length - 1 : index;
		}

		index = Math.Max(0, Math.Min(NewAccountDayPresets.Length - 1, index + direction));
		NewAccountDays.Value = NewAccountDayPresets[index];
	}

	public void Reset()
	{
		Enabled.Value = false;
		ShowFps.Value = true;
		ShowPlatform.Value = true;
		ShowAccountAge.Value = true;
		ShowPing.Value = true;
		ShowNewBadge.Value = true;
		ColorValues.Value = true;
		FadeEnabled.Value = true;
		FontSize.Value = 20f;
		MaxDistance.Value = 25f;
		NewAccountDays.Value = 30;
		NameTint.Value = 0;
		ColorMode = NameColorMode.Plain;
		ShowFriendStar.Value = true;
		ShowNotes.Value = true;
		ShowCustomTags.Value = true;
		Background.Value = true;
		ShowOwnTag.Value = false;
		SelfTagColor.Value = 0;
	}

	private void OnAnySettingChanged(object sender, EventArgs args)
	{
		Version++;
	}

	private static float Clamp(float value, float min, float max)
	{
		return Math.Max(min, Math.Min(max, value));
	}
}

public sealed class WindSettings
{
	public const int MaxStrength = 3;
	public const int DirectionCount = 8;

	public readonly ConfigEntry<int> Strength;
	public readonly ConfigEntry<int> Direction;
	public readonly ConfigEntry<bool> Gusts;

	public int StrengthLevel => Math.Max(0, Math.Min(MaxStrength, Strength.Value));

	public int DirectionIndex => (Direction.Value % DirectionCount + DirectionCount) % DirectionCount;

	public WindSettings(ConfigFile file)
	{
		Strength = file.Bind("Wind", "Strength", 0, "Local wind strength from 0 (off) to 3.");
		Direction = file.Bind("Wind", "Direction", 0, "Wind direction in 45 degree steps.");
		Gusts = file.Bind("Wind", "Gusts", true, "Vary the wind strength over time.");
	}

	public void StepStrength(int direction)
	{
		Strength.Value = Math.Max(0, Math.Min(MaxStrength, StrengthLevel + direction));
	}

	public void StepDirection(int direction)
	{
		Direction.Value = ((DirectionIndex + direction) % DirectionCount + DirectionCount) % DirectionCount;
	}

	public void Reset()
	{
		Strength.Value = 0;
		Direction.Value = 0;
		Gusts.Value = true;
	}
}

public sealed class MenuSettings
{
	public const float MinOpacity = 0.4f;
	public const float MaxOpacity = 1f;
	public const float DefaultOpacity = 0.95f;
	public const float MinPointerRange = 3f;
	public const float MaxPointerRange = 30f;
	public const string DefaultThemeName = "Lava";
	public const string DashboardUrl = "https://mod-kill-gate.base44.app";
	public const string DefaultKillSwitchUrl = DashboardUrl + "/api/apps/6ad28635efea27673ff79c9e/entities/ModStatus";

	public readonly ConfigEntry<bool> GunRay;
	public readonly ConfigEntry<int> MenuSize;
	public readonly ConfigEntry<float> WristOffset;
	public readonly ConfigEntry<bool> PhoneMode;
	public readonly ConfigEntry<int> AntiLag;
	public readonly ConfigEntry<bool> Splash;
	public readonly ConfigEntry<bool> OwnerAlerts;
	public readonly ConfigEntry<bool> FriendJoinAlerts;
	public readonly ConfigEntry<int> OwnerSound;
	public readonly ConfigEntry<int> FriendSound;
	public readonly ConfigEntry<int> AlertSound;
	public readonly ConfigEntry<float> SoundVolume;
	public readonly ConfigEntry<float> PointerRange;
	public readonly ConfigEntry<bool> AutoLock;
	public readonly ConfigEntry<bool> OrbMode;
	public readonly ConfigEntry<bool> MenuAnimations;
	public readonly ConfigEntry<bool> ThemeScenery;
	public readonly ConfigEntry<bool> DetectionAlerts;
	public readonly ConfigEntry<bool> FriendAlerts;
	public readonly ConfigEntry<float> Opacity;
	public readonly ConfigEntry<string> ThemeName;
	public readonly ConfigEntry<bool> AnimatedThemes;
	public readonly ConfigEntry<bool> ThemeParticles;
	public readonly ConfigEntry<string> KillSwitchUrl;
	public readonly ConfigEntry<int> KillSwitchIntervalSeconds;
	public readonly TagSettings Tags;
	public readonly WindSettings Wind;

	public MenuSettings(ConfigFile file)
	{
		GunRay = file.Bind("Menu", "GunRay", true, "Show the gun pointer while holding the right grip.");
		MenuSize = file.Bind("Menu", "SizeLevel", 3, "Menu size: 0 tiny, 1 mini, 2 small, 3 medium, 4 large.");
		WristOffset = file.Bind("Menu", "WristOffset", 0f, "Slides the wrist menu sideways away from your hand, in meters.");
		PhoneMode = file.Bind("Menu", "PhoneMode", false, "Use the AceOne smartphone shape instead of the pad.");
		AntiLag = file.Bind("Menu", "AntiLag", 1, "Anti-lag: 0 off, 1 automatic, 2 always on.");
		Splash = file.Bind("Menu", "WelcomeScreen", true, "Show the welcome screen with the orb logo when the game starts.");
		OwnerAlerts = file.Bind("Alerts", "OwnerJoinAlerts", true, "Alert when the AceUtilsPad owner is in your lobby.");
		FriendJoinAlerts = file.Bind("Alerts", "FriendJoinAlerts", true, "Alert when a friend or starred player joins your lobby.");
		OwnerSound = file.Bind("Alerts", "OwnerSound", 4, "Sound index for owner alerts.");
		FriendSound = file.Bind("Alerts", "FriendSound", 1, "Sound index for friend alerts.");
		AlertSound = file.Bind("Alerts", "AlertSound", 2, "Sound index for every other alert.");
		SoundVolume = file.Bind("Alerts", "SoundVolume", 0.7f, "Notification sound volume from 0 to 1.");
		PointerRange = file.Bind("Menu", "PointerRange", 10f, "How far the gun pointer reaches, in meters.");
		AutoLock = file.Bind("Menu", "AutoLock", false, "Lock onto the pointed player without holding the trigger.");
		OrbMode = file.Bind("Menu", "OrbMode", false, "Use a throwable orb that unfolds the menu where it lands.");
		MenuAnimations = file.Bind("Menu", "Animations", true, "Play open, close, press and page animations.");
		DetectionAlerts = file.Bind("Alerts", "DetectionAlerts", true, "Alert when a player with mods or cheats is detected.");
		FriendAlerts = file.Bind("Alerts", "FriendAlerts", true, "Alert when an in-game friend joins a lobby.");
		Opacity = file.Bind("Theme", "Opacity", DefaultOpacity, "Menu background opacity.");
		ThemeName = file.Bind("Theme", "Name", DefaultThemeName, "Selected theme.");
		AnimatedThemes = file.Bind("Theme", "AnimatedBackgrounds", true, "Animate gradient backgrounds.");
		ThemeParticles = file.Bind("Theme", "Particles", true, "Show floating particles on themed backgrounds.");
		ThemeScenery = file.Bind("Theme", "Scenery", true, "Draw the theme scenery along the bottom of the menu.");
		KillSwitchUrl = file.Bind("KillSwitch", "Url", DefaultKillSwitchUrl, "Status link that can remotely shut the menu down.");
		KillSwitchIntervalSeconds = file.Bind("KillSwitch", "IntervalSeconds", 30, "Seconds between kill switch checks.");
		Tags = new TagSettings(file);
		Wind = new WindSettings(file);
	}

	public const float MinWristOffset = -0.1f;
	public const float MaxWristOffset = 0.25f;

	public static readonly string[] SizeNames = { "Tiny", "Mini", "Small", "Medium", "Large" };
	public static readonly float[] SizeScales = { 0.5f, 0.65f, 0.8f, 1f, 1.25f };
	public static readonly string[] AntiLagNames = { "Off", "Auto", "Always" };

	public void StepWristOffset(float delta)
	{
		WristOffset.Value = Math.Max(MinWristOffset, Math.Min(MaxWristOffset, (float)Math.Round(WristOffset.Value + delta, 2)));
	}

	public void StepVolume(float delta)
	{
		SoundVolume.Value = Math.Max(0f, Math.Min(1f, (float)Math.Round(SoundVolume.Value + delta, 1)));
	}

	public void CycleAntiLag()
	{
		AntiLag.Value = (Math.Max(0, AntiLag.Value) + 1) % AntiLagNames.Length;
	}

	public int SizeIndex => Math.Max(0, Math.Min(SizeScales.Length - 1, MenuSize.Value));

	public float SizeScale => SizeScales[SizeIndex];

	public void StepPointerRange(float delta)
	{
		PointerRange.Value = Math.Max(MinPointerRange, Math.Min(MaxPointerRange, PointerRange.Value + delta));
	}

	public void StepOpacity(float delta)
	{
		Opacity.Value = Math.Max(MinOpacity, Math.Min(MaxOpacity, (float)Math.Round(Opacity.Value + delta, 2)));
	}

	public void ResetMenu()
	{
		GunRay.Value = true;
		MenuSize.Value = 3;
		WristOffset.Value = 0f;
		PhoneMode.Value = false;
		AntiLag.Value = 1;
		Splash.Value = true;
		OwnerAlerts.Value = true;
		FriendJoinAlerts.Value = true;
		OwnerSound.Value = 4;
		FriendSound.Value = 1;
		AlertSound.Value = 2;
		SoundVolume.Value = 0.7f;
		PointerRange.Value = 10f;
		AutoLock.Value = false;
		OrbMode.Value = false;
		MenuAnimations.Value = true;
		DetectionAlerts.Value = true;
		FriendAlerts.Value = true;
		Opacity.Value = DefaultOpacity;
		ThemeName.Value = DefaultThemeName;
		AnimatedThemes.Value = true;
		ThemeParticles.Value = true;
		ThemeScenery.Value = true;
		Tags.Reset();
		Wind.Reset();
	}
}

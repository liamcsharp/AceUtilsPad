using System;
using System.Collections.Generic;
using AUP.UI;

namespace AUP;

public sealed class MenuNavigation
{
	private readonly int pageCount;

	public MenuNavigation(int pageCount)
	{
		this.pageCount = pageCount;
	}

	public int Page { get; private set; }
	public int Section { get; private set; }
	public int PageCount => pageCount;

	public event Action Changed;

	public void GoToPage(int page)
	{
		int clamped = Math.Max(0, Math.Min(pageCount - 1, page));
		if (clamped == Page && Section == 0)
		{
			return;
		}

		Page = clamped;
		Section = 0;
		Changed?.Invoke();
	}

	public void GoToSection(int section)
	{
		if (section == Section)
		{
			return;
		}

		Section = section;
		Changed?.Invoke();
	}
}

public sealed class MenuContext
{
	private const float FpsSmoothing = 0.05f;

	public MenuSettings Settings;
	public ThemeController Theme;
	public PlayerSelection Selection;
	public MenuNavigation Nav;
	public LobbyService Lobby;
	public FriendAlertService Friends;
	public AudioHandler Audio;
	public NameTagController Tags;
	public WorldEnvironmentController World;
	public WindEffectController Wind;
	public PlayerNotes Notes;
	public AceCam Cam;
	public AUP.Widgets.WidgetManager Widgets;
	public Func<bool> MenuOpen;
	public Action ToggleMenu;
	public AlertHub Alerts;
	public TextEntry Text;
	public IReadOnlyList<MenuPage> Pages;
	public CosmeticsService Cosmetics;
	public LobbyKeyboardOverlay Keyboard;
	public DetectionAlertService DetectionAlerts;
	public KillSwitch KillSwitch;
	public Action HideMenu;
	public Action SummonOrb;
	public Func<string> OrbStatus;

	public float Fps { get; private set; }

	public bool Busy => DetectionService.Loading || (Audio != null && Audio.Connecting);

	public void ShowPage<T>() where T : MenuPage
	{
		if (Pages == null || Nav == null)
		{
			return;
		}

		for (int i = 0; i < Pages.Count; i++)
		{
			if (Pages[i] is T)
			{
				Nav.GoToPage(i);
				return;
			}
		}
	}

	public event Action RefreshRequested;

	public void TickFps(float deltaTime)
	{
		if (deltaTime <= 0f)
		{
			return;
		}

		float instant = 1f / deltaTime;
		Fps = Fps <= 0f ? instant : Fps + (instant - Fps) * FpsSmoothing;
	}

	public void RequestRefresh()
	{
		RefreshRequested?.Invoke();
	}
}

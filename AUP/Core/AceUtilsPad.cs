using System.Collections.Generic;
using AUP.Pages;
using AUP.UI;
using BepInEx;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace AUP;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class AceUtilsPad : BaseUnityPlugin
{
	private const float PresenceInterval = 5f;
	private const float QuitDelaySeconds = 1f;

	private readonly List<Component> components = new List<Component>();

	private MenuContext context;
	private MenuController menu;
	private AlertHub alerts;
	private DiscordRPC discord;
	private Harmony harmony;
	private float nextPresenceTime;
	private bool shutDown;

	private void Awake()
	{
		MenuSettings settings = new MenuSettings(Config);
		context = CreateContext(settings);
		alerts = new AlertHub { Settings = settings };
		context.Alerts = alerts;
		context.Cam.Initialize(context, new CamSettings(Config));
		context.Widgets.Initialize(context);

		IReadOnlyList<MenuPage> pages = CreatePages(context);
		context.Pages = pages;
		context.Nav = new MenuNavigation(pages.Count);

		MenuNetworkDisplay networkDisplay = Add<MenuNetworkDisplay>();
		networkDisplay.Initialize(index => pages[Mathf.Clamp(index, 0, pages.Count - 1)].Title, pages.Count);

		alerts.Register(AlertBanner.Create(SurfaceMode.World, context));
		alerts.Register(AlertBanner.Create(SurfaceMode.Screen, context));

		OrbMenu orb = Add<OrbMenu>();
		orb.Initialize(settings);
		menu = Add<MenuController>();
		menu.Initialize(context, pages, alerts, networkDisplay, orb);

		context.DetectionAlerts.Initialize(settings, alerts);
		context.Friends.FriendJoined += OnFriendJoined;
		context.KillSwitch.Killed += Shutdown;

		discord = Add<DiscordRPC>();
		Add<PresenceService>().Initialize(settings.Tags);
		Add<OwnerCrownController>();
		Add<LobbyJoinAlerts>().Initialize(settings, alerts, context.Friends, context.Notes);
		if (settings.Splash.Value)
		{
			SplashScreen.Create();
		}

		InstallPatches();
	}

	private void Start()
	{
		DetectionService.RefreshLists();
	}

	private void Update()
	{
		if (shutDown)
		{
			return;
		}

		context.TickFps(Time.unscaledDeltaTime);
		PerformanceGuard.Tick(context.Settings.AntiLag.Value, context.Fps, Time.unscaledDeltaTime);
		context.Selection.Validate();
		UpdateDiscordActivity();
	}

	private void OnDestroy()
	{
		if (harmony != null)
		{
			harmony.UnpatchSelf();
		}
	}

	private MenuContext CreateContext(MenuSettings settings)
	{
		MenuContext created = new MenuContext
		{
			Settings = settings,
			Theme = new ThemeController(settings),
			Selection = new PlayerSelection(),
			Notes = new PlayerNotes(),
			Text = new TextEntry(),
			Cosmetics = new CosmeticsService(),
			Lobby = Add<LobbyService>(),
			Friends = Add<FriendAlertService>(),
			Audio = Add<AudioHandler>(),
			Cam = Add<AceCam>(),
			Widgets = Add<AUP.Widgets.WidgetManager>(),
			Tags = Add<NameTagController>(),
			World = Add<WorldEnvironmentController>(),
			Wind = Add<WindEffectController>(),
			Keyboard = Add<LobbyKeyboardOverlay>(),
			DetectionAlerts = Add<DetectionAlertService>(),
			KillSwitch = Add<KillSwitch>()
		};

		created.Friends.Initialize(settings);
		created.Tags.Initialize(settings.Tags, created.Notes, created.Friends);
		created.Wind.Initialize(settings.Wind);
		created.Keyboard.Initialize(created.Lobby);
		created.KillSwitch.Initialize(settings);

		GunPointer pointer = Add<GunPointer>();
		pointer.Initialize(settings, created.Selection, () => menu != null && menu.IsWorldMenuOpen, () => created.Text.Active);
		pointer.LockedOn += _ => created.Nav?.GoToPage(0);
		return created;
	}

	private static IReadOnlyList<MenuPage> CreatePages(MenuContext menuContext)
	{
		return new List<MenuPage>
		{
			new PlayerInfoPage(menuContext),
			new ModsPage(menuContext),
			new LobbyPage(menuContext),
			new FriendsPage(menuContext),
			new MediaPage(menuContext),
			new CamPage(menuContext),
			new WidgetsPage(menuContext),
			new WidgetHubPage(menuContext),
			new TagsPage(menuContext),
			new CosmeticsPage(menuContext),
			new WorldPage(menuContext),
			new ThemesPage(menuContext),
			new SettingsPage(menuContext),
			new UtilitiesPage(menuContext),
			new KeyboardPage(menuContext),
			new CreditsPage(menuContext)
		};
	}

	private T Add<T>() where T : Component
	{
		T component = gameObject.AddComponent<T>();
		components.Add(component);
		return component;
	}

	private void InstallPatches()
	{
		harmony = new Harmony(PluginInfo.Guid);
		if (!LatencyTracker.Install(harmony))
		{
			Logger.LogWarning("Could not hook player latency. Ping will show N/A.");
		}
	}

	private void OnFriendJoined(FriendEntry friend)
	{
		string zone = string.IsNullOrEmpty(friend.Zone) ? string.Empty : " in " + Rich.Escape(friend.Zone);
		alerts.Post(new AlertMessage
		{
			Text = "<b>" + Rich.Escape(friend.Name) + "</b> joined a lobby" + zone + ".",
			ActionText = "Join",
			Action = () => context.Lobby.JoinFriend(friend.Room, friend.IsPublic),
			Seconds = 12f
		});
	}

	private void UpdateDiscordActivity()
	{
		if (Time.unscaledTime < nextPresenceTime)
		{
			return;
		}

		nextPresenceTime = Time.unscaledTime + PresenceInterval;
		string details = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
			? "In a lobby (" + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers + ")"
			: "Using Menu";
		discord.SetActivity(details, "In Game");
	}

	private System.Collections.IEnumerator QuitGameSoon()
	{
		yield return new WaitForSecondsRealtime(QuitDelaySeconds);
		Application.Quit();
	}

	private void Shutdown()
	{
		if (shutDown)
		{
			return;
		}

		shutDown = true;
		Logger.LogWarning("Shut down remotely by the kill switch.");
		menu.Shutdown();
		PresenceService.Withdraw();
		foreach (Component component in components)
		{
			Destroy(component);
		}

		components.Clear();
		if (harmony != null)
		{
			harmony.UnpatchSelf();
			harmony = null;
		}

		StartCoroutine(QuitGameSoon());
	}
}

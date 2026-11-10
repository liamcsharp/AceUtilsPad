using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AUP.UI;

namespace AUP;

public sealed class MenuNetworkDisplay : MonoBehaviour
{
	private const string VisibleKey = "AceUtilsPad.MenuVisible";
	private const string PageKey = "AceUtilsPad.MenuPage";
	private const string ContentKey = "AceUtilsPad.MenuContent";
	private const float RefreshInterval = 0.25f;
	private const float PublishInterval = 1f;

	private readonly Dictionary<VRRig, RemoteDisplay> displays = new Dictionary<VRRig, RemoteDisplay>();
	private readonly List<VRRig> staleRigs = new List<VRRig>();
	private readonly HashSet<VRRig> seenRigs = new HashSet<VRRig>();

	private Func<int, string> pageNames;
	private int pageCount;
	private float nextRefreshTime;
	private float nextPublishTime;

	public void Initialize(Func<int, string> pageNameLookup, int totalPages)
	{
		pageNames = pageNameLookup;
		pageCount = totalPages;
	}

	public void PublishLocalState(bool visible, int page, string content)
	{
		Player local = PhotonNetwork.LocalPlayer;
		if (!PhotonNetwork.InRoom || local == null)
		{
			return;
		}

		content = content ?? string.Empty;
		int wirePage = page + 1;
		bool visibleSame = Matches(local, VisibleKey, visible);
		bool pageSame = Matches(local, PageKey, wirePage);
		bool contentSame = Matches(local, ContentKey, content);

		bool navigationChanged = !visibleSame || !pageSame;
		if (!navigationChanged && contentSame)
		{
			return;
		}

		if (!navigationChanged && Time.unscaledTime < nextPublishTime)
		{
			return;
		}

		nextPublishTime = Time.unscaledTime + PublishInterval;
		Hashtable properties = new Hashtable
		{
			{ VisibleKey, visible },
			{ PageKey, wirePage },
			{ ContentKey, content }
		};
		local.SetCustomProperties(properties);
	}

	private static bool Matches<T>(Player player, string key, T expected)
	{
		return player.CustomProperties != null
			   && player.CustomProperties.TryGetValue(key, out object value)
			   && value is T typed
			   && EqualityComparer<T>.Default.Equals(typed, expected);
	}

	private void Update()
	{
		if (Time.unscaledTime < nextRefreshTime)
		{
			return;
		}

		nextRefreshTime = Time.unscaledTime + RefreshInterval;
		RefreshDisplays();
	}

	private void OnDestroy()
	{
		foreach (RemoteDisplay display in displays.Values)
		{
			display.Destroy();
		}

		displays.Clear();
	}

	private void RefreshDisplays()
	{
		seenRigs.Clear();
		foreach (VRRig rig in PlayerInfo.RemoteRigs())
		{
			seenRigs.Add(rig);
			if (!TryReadRemoteMenu(rig, out int page, out string content))
			{
				RemoveDisplay(rig);
				continue;
			}

			Transform hand = Hands.Find(rig.transform, HandSide.Left);
			if (hand == null)
			{
				continue;
			}

			if (!displays.TryGetValue(rig, out RemoteDisplay display) || display.Root == null)
			{
				display = RemoteDisplay.Create(hand);
				displays[rig] = display;
			}

			display.Attach(hand);
			display.Show(page, pageCount, pageNames(page), content);
		}

		staleRigs.Clear();
		foreach (VRRig rig in displays.Keys)
		{
			if (rig == null || !seenRigs.Contains(rig))
			{
				staleRigs.Add(rig);
			}
		}

		foreach (VRRig rig in staleRigs)
		{
			RemoveDisplay(rig);
		}
	}

	private bool TryReadRemoteMenu(VRRig rig, out int page, out string content)
	{
		page = 0;
		content = string.Empty;
		Dictionary<object, object> properties = PlayerInfo.Properties(rig);
		if (properties == null || !properties.TryGetValue(VisibleKey, out object visible) || !(visible is bool shown) || !shown)
		{
			return false;
		}

		if (properties.TryGetValue(PageKey, out object pageValue) && pageValue is int index)
		{
			page = Mathf.Clamp(index - 1, 0, Mathf.Max(0, pageCount - 1));
		}

		if (properties.TryGetValue(ContentKey, out object contentValue))
		{
			content = contentValue as string ?? string.Empty;
		}

		return true;
	}

	private void RemoveDisplay(VRRig rig)
	{
		if (rig != null && displays.TryGetValue(rig, out RemoteDisplay display))
		{
			display.Destroy();
		}

		displays.Remove(rig);
	}

	private sealed class RemoteDisplay
	{
		private static readonly Color BackgroundColor = new Color(0.04f, 0.08f, 0.16f, 0.94f);
		private static readonly Color PageColor = new Color(0.6f, 0.6f, 0.6f, 1f);
		private static readonly Color PageNameColor = new Color(0.45f, 0.72f, 1f, 1f);

		private TextMeshProUGUI pageText;
		private TextMeshProUGUI pageNameText;
		private TextMeshProUGUI contentText;
		private Transform hand;

		public GameObject Root { get; private set; }

		public static RemoteDisplay Create(Transform hand)
		{
			GameObject root = new GameObject("AUPRemoteMenu");
			root.transform.SetParent(hand, false);
			root.transform.localPosition = new Vector3(0.08f, 0.04f, 0.04f);
			root.transform.localRotation = Quaternion.Euler(125f, 0f, 10f);
			root.transform.localScale = Vector3.one * 0.00115f;

			Canvas canvas = root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = Camera.main;
			root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1000f;
			((RectTransform)root.transform).sizeDelta = new Vector2(280f, 320f);

			RemoteDisplay display = new RemoteDisplay { Root = root, hand = hand };
			display.Build(root.transform);
			return display;
		}

		public void Attach(Transform newHand)
		{
			if (hand == newHand)
			{
				return;
			}

			Root.transform.SetParent(newHand, false);
			hand = newHand;
		}

		public void Show(int page, int pageCount, string pageName, string content)
		{
			pageText.text = "Page " + (page + 1) + "/" + pageCount;
			pageNameText.text = Rich.Escape(pageName);
			contentText.text = string.IsNullOrWhiteSpace(content) ? "Loading menu..." : Rich.Sanitize(content);
		}

		public void Destroy()
		{
			if (Root != null)
			{
				UnityEngine.Object.Destroy(Root);
			}
		}

		private void Build(Transform root)
		{
			Image background = UiKit.NewImage("Background", root, null, false);
			UiKit.Stretch(background.rectTransform);
			background.color = BackgroundColor;

			TextMeshProUGUI title = AddText(root, "Title", PluginInfo.Name, new Vector2(0f, 125f), 11f, new Vector2(250f, 20f));
			title.fontStyle = FontStyles.Bold;
			pageText = AddText(root, "Page", string.Empty, new Vector2(0f, 108f), 8f, new Vector2(250f, 16f));
			pageText.color = PageColor;
			pageNameText = AddText(root, "PageName", string.Empty, new Vector2(0f, 94f), 10f, new Vector2(250f, 18f));
			pageNameText.color = PageNameColor;
			contentText = AddText(root, "Content", "Loading menu...", new Vector2(0f, 10f), 7.5f, new Vector2(245f, 155f));
		}

		private static TextMeshProUGUI AddText(Transform parent, string name, string value, Vector2 position, float size, Vector2 area)
		{
			TextMeshProUGUI text = UiKit.NewText(name, parent, size, TextAlignmentOptions.Center, FontStyles.Normal);
			text.text = value;
			text.color = Color.white;
			RectTransform rect = text.rectTransform;
			rect.sizeDelta = area;
			rect.anchoredPosition = position;
			return text;
		}
	}
}

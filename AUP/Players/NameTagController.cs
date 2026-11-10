using System;
using System.Collections.Generic;
using System.Text;
using AUP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP;

public sealed class NameTagController : MonoBehaviour
{
	private const float ScanInterval = 1f;
	private const float ContentInterval = 0.25f;
	private const float TagHeight = 0.55f;
	private const float FadeStartRatio = 0.7f;
	private const float BaseScale = 0.0021f;
	private const float ReferenceFont = 20f;
	private const float NameSize = 22f;
	private const float StatSize = 13f;
	private const float NoteSize = 12f;
	private const float ChipSize = 12f;
	private const float IconSize = 20f;
	private const float StarSize = 17f;
	private const float DotSize = 8f;
	private const float Gap = 5f;
	private const float RowGap = 2f;
	private const float Padding = 9f;
	private const int PingRounding = 5;

	private static readonly Color CardColor = new Color(0.04f, 0.04f, 0.07f, 0.6f);
	private static readonly Color NewChipColor = new Color(1f, 0.83f, 0.25f, 1f);

	private readonly Dictionary<VRRig, TagView> tags = new Dictionary<VRRig, TagView>();
	private readonly HashSet<VRRig> seenRigs = new HashSet<VRRig>();
	private readonly List<VRRig> staleRigs = new List<VRRig>();
	private readonly StringBuilder builder = new StringBuilder(160);

	private TagSettings settings;
	private PlayerNotes notes;
	private FriendAlertService friends;
	private Camera viewCamera;
	private float nextScanTime;

	public void Initialize(TagSettings tagSettings, PlayerNotes playerNotes, FriendAlertService friendService)
	{
		settings = tagSettings;
		notes = playerNotes;
		friends = friendService;
	}

	private void Update()
	{
		if (settings == null)
		{
			return;
		}

		if (!settings.Enabled.Value)
		{
			if (tags.Count > 0)
			{
				DestroyAllTags();
			}

			return;
		}

		if (GorillaTagger.Instance == null)
		{
			return;
		}

		if (viewCamera == null || !viewCamera.isActiveAndEnabled)
		{
			viewCamera = PhysicsLayers.ViewCamera();
		}

		if (Time.unscaledTime >= nextScanTime)
		{
			nextScanTime = Time.unscaledTime + ScanInterval;
			SyncTagsWithRigs();
		}

		foreach (KeyValuePair<VRRig, TagView> pair in tags)
		{
			UpdateTag(pair.Key, pair.Value);
		}
	}

	private void OnDestroy()
	{
		DestroyAllTags();
	}

	private void SyncTagsWithRigs()
	{
		seenRigs.Clear();
		foreach (VRRig rig in PlayerInfo.RemoteRigs())
		{
			Track(rig, false);
		}

		if (settings.ShowOwnTag.Value)
		{
			VRRig local = VRRig.LocalRig;
			if (local == null && GorillaTagger.Instance != null)
			{
				local = GorillaTagger.Instance.offlineVRRig;
			}

			if (local != null)
			{
				Track(local, true);
			}
		}

		staleRigs.Clear();
		foreach (KeyValuePair<VRRig, TagView> pair in tags)
		{
			if (pair.Key == null || !seenRigs.Contains(pair.Key) || pair.Value.Root == null)
			{
				staleRigs.Add(pair.Key);
			}
		}

		foreach (VRRig rig in staleRigs)
		{
			RemoveTag(rig);
		}
	}

	private void Track(VRRig rig, bool isLocal)
	{
		seenRigs.Add(rig);
		if (!tags.ContainsKey(rig))
		{
			tags[rig] = TagView.Create(transform, isLocal);
		}
	}

	private void UpdateTag(VRRig rig, TagView view)
	{
		if (view.Root == null)
		{
			return;
		}

		if (rig == null)
		{
			view.Root.gameObject.SetActive(false);
			return;
		}

		Transform tagTransform = view.Root;
		tagTransform.position = rig.transform.position + Vector3.up * TagHeight;

		float alpha = ComputeAlpha(tagTransform.position);
		bool visible = alpha > 0.01f;
		if (view.Root.gameObject.activeSelf != visible)
		{
			view.Root.gameObject.SetActive(visible);
		}

		if (!visible)
		{
			return;
		}

		if (Time.unscaledTime >= view.NextContentTime)
		{
			view.NextContentTime = Time.unscaledTime + ContentInterval;
			RefreshContent(rig, view);
		}

		view.Group.alpha = alpha;
		float scale = BaseScale * settings.FontSize.Value / ReferenceFont;
		tagTransform.localScale = new Vector3(scale, scale, scale);
		FaceCamera(tagTransform);
	}

	private float ComputeAlpha(Vector3 tagPosition)
	{
		if (viewCamera == null)
		{
			return 1f;
		}

		float distance = Vector3.Distance(viewCamera.transform.position, tagPosition);
		float limit = settings.MaxDistance.Value;
		if (distance > limit)
		{
			return 0f;
		}

		if (!settings.FadeEnabled.Value)
		{
			return 1f;
		}

		return 1f - Mathf.InverseLerp(limit * FadeStartRatio, limit, distance);
	}

	private void FaceCamera(Transform tagTransform)
	{
		if (viewCamera == null)
		{
			return;
		}

		Vector3 away = tagTransform.position - viewCamera.transform.position;
		if (away.sqrMagnitude > 0.0001f)
		{
			tagTransform.rotation = Quaternion.LookRotation(away);
		}
	}

	private void RefreshContent(VRRig rig, TagView view)
	{
		string playerName = PlayerInfo.DisplayName(rig);
		string platform = PlayerInfo.Platform(rig);
		int fps = view.IsLocal ? 0 : PlayerInfo.Fps(rig);
		int? rawPing = view.IsLocal ? null : LatencyTracker.GetPingMs(rig);
		int ping = rawPing.HasValue ? Mathf.RoundToInt(rawPing.Value / (float)PingRounding) * PingRounding : -1;
		AccountAge age = NeedsAccountAge() ? PlayerInfo.GetAccountAge(rig) : new AccountAge(AccountStatus.Unknown, DateTime.MinValue);
		bool friend = settings.ShowFriendStar.Value && !view.IsLocal
					  && ((friends != null && friends.IsFriend(rig)) || (notes != null && notes.IsStarred(rig)));
		string note = settings.ShowNotes.Value && notes != null && !view.IsLocal ? notes.NoteFor(rig) : string.Empty;

		string customText = null;
		int customColor = 0;
		if (view.IsLocal)
		{
			customText = PlayerTags.Clean(settings.SelfTag.Value);
			customColor = settings.SelfTagColor.Value;
		}
		else if (settings.ShowCustomTags.Value)
		{
			PlayerTags.TryRead(rig, out customText, out customColor);
		}

		bool isNew = !view.IsLocal && settings.ShowNewBadge.Value && age.IsReady && age.Days < settings.NewAccountDays.Value;
		string nameMarkup = "<color=" + Hex(NameColor(fps, ping, platform, age)) + ">" + Rich.Escape(playerName) + "</color>";
		string stats = view.IsLocal ? string.Empty : BuildStats(fps, ping, platform, age);
		string iconId = settings.ShowPlatform.Value ? PlatformNames.IconId(platform) : null;
		bool showDot = !view.IsLocal && settings.ShowPing.Value;

		string signature = settings.Version + "|" + (notes != null ? notes.Version : 0) + "|" + nameMarkup + "|" + stats + "|" + iconId + "|" + friend
						   + "|" + note + "|" + customText + "|" + customColor + "|" + isNew + "|" + ping + "|" + showDot;
		if (signature == view.Signature)
		{
			return;
		}

		view.Signature = signature;
		view.Layout(nameMarkup, iconId, TagPalette.PlatformIcon(platform), friend, isNew, stats, showDot, TagPalette.PingDot(ping),
			note, customText, TagPalette.SelfTag(customColor), settings.Background.Value);
	}

	private bool NeedsAccountAge()
	{
		return settings.ShowAccountAge.Value || settings.ShowNewBadge.Value || settings.ColorMode == NameColorMode.AccountAge;
	}

	private string BuildStats(int fps, int ping, string platform, AccountAge age)
	{
		builder.Length = 0;
		int fieldCount = 0;
		if (settings.ShowPing.Value)
		{
			AppendField(fieldCount++, ping >= 0 ? ping + "ms" : "?ms", TagPalette.Ping(ping), ping >= 0);
		}

		if (settings.ShowFps.Value)
		{
			AppendField(fieldCount++, fps > 0 ? fps + " fps" : "? fps", TagPalette.Fps(fps), fps > 0);
		}

		if (settings.ShowPlatform.Value && PlatformNames.IconId(platform) == null)
		{
			AppendField(fieldCount++, PlatformNames.Friendly(platform), TagPalette.Platform(platform), true);
		}

		if (settings.ShowAccountAge.Value)
		{
			AppendField(fieldCount++, age.ShortText + " old", TagPalette.Age(age, settings.NewAccountDays.Value), age.IsReady);
		}

		return builder.ToString();
	}

	private void AppendField(int index, string value, Color color, bool hasData)
	{
		if (index > 0)
		{
			builder.Append(" <color=").Append(Hex(TagPalette.Separator)).Append(">|</color> ");
		}

		Color shown = hasData && settings.ColorValues.Value ? color : TagPalette.Neutral;
		builder.Append("<color=").Append(Hex(shown)).Append('>').Append(value).Append("</color>");
	}

	private Color NameColor(int fps, int ping, string platform, AccountAge age)
	{
		Color tint = TagPalette.Tint(settings.NameTint.Value);
		switch (settings.ColorMode)
		{
			case NameColorMode.Platform:
				return TagPalette.Platform(platform);
			case NameColorMode.Fps:
				return fps > 0 ? TagPalette.Fps(fps) : tint;
			case NameColorMode.Ping:
				return ping >= 0 ? TagPalette.Ping(ping) : tint;
			case NameColorMode.AccountAge:
				return age.IsReady ? TagPalette.Age(age, settings.NewAccountDays.Value) : tint;
			default:
				return tint;
		}
	}

	private static string Hex(Color color)
	{
		return "#" + ColorUtility.ToHtmlStringRGB(color);
	}

	private void RemoveTag(VRRig rig)
	{
		if (tags.TryGetValue(rig, out TagView view) && view.Root != null)
		{
			Destroy(view.Root.gameObject);
		}

		tags.Remove(rig);
	}

	private void DestroyAllTags()
	{
		foreach (TagView view in tags.Values)
		{
			if (view.Root != null)
			{
				Destroy(view.Root.gameObject);
			}
		}

		tags.Clear();
		seenRigs.Clear();
		staleRigs.Clear();
	}

	private sealed class TagView
	{
		public RectTransform Root;
		public CanvasGroup Group;
		public bool IsLocal;
		public float NextContentTime;
		public string Signature;

		private Image card;
		private Image chip;
		private TextMeshProUGUI chipText;
		private Image icon;
		private TextMeshProUGUI nameText;
		private Image star;
		private Image newChip;
		private TextMeshProUGUI newText;
		private Image dot;
		private TextMeshProUGUI statsText;
		private TextMeshProUGUI noteText;

		public static TagView Create(Transform parent, bool isLocal)
		{
			GameObject root = new GameObject("AceUtilsPadNameTag", typeof(RectTransform));
			root.transform.SetParent(parent, false);
			Canvas canvas = root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1000f;
			RectTransform rect = (RectTransform)root.transform;
			rect.sizeDelta = new Vector2(10f, 10f);
			rect.pivot = new Vector2(0.5f, 0f);

			TagView view = new TagView
			{
				Root = rect,
				Group = root.AddComponent<CanvasGroup>(),
				IsLocal = isLocal
			};

			view.Group.interactable = false;
			view.Group.blocksRaycasts = false;
			view.card = view.NewImage("Card", SpriteFactory.Rounded(18), true);
			view.chip = view.NewImage("Chip", SpriteFactory.Rounded(10), true);
			view.chipText = view.NewText("ChipText", ChipSize, FontStyles.Bold);
			view.icon = view.NewImage("Platform", null, false);
			view.nameText = view.NewText("Name", NameSize, FontStyles.Bold);
			view.star = view.NewImage("Star", SpriteFactory.Icon("star"), false);
			view.newChip = view.NewImage("NewChip", SpriteFactory.Rounded(8), true);
			view.newText = view.NewText("NewText", ChipSize - 1f, FontStyles.Bold);
			view.dot = view.NewImage("PingDot", SpriteFactory.Circle(), false);
			view.statsText = view.NewText("Stats", StatSize, FontStyles.Normal);
			view.noteText = view.NewText("Note", NoteSize, FontStyles.Italic);
			view.star.color = TagPalette.Star;
			view.newChip.color = NewChipColor;
			view.newText.text = "NEW";
			view.newText.color = new Color(0.12f, 0.08f, 0.02f, 1f);
			return view;
		}

		public void Layout(string nameMarkup, string iconId, Color iconColor, bool friend, bool isNew, string stats, bool showDot, Color dotColor,
			string note, string customText, Color customColor, bool background)
		{
			float y = 0f;
			float widest = 0f;
			bool hasNote = !string.IsNullOrEmpty(note);
			bool hasStats = !string.IsNullOrEmpty(stats);
			bool hasCustom = !string.IsNullOrEmpty(customText);

			y += Padding;
			noteText.gameObject.SetActive(hasNote);
			if (hasNote)
			{
				SetText(noteText, "\"" + Rich.Escape(note) + "\"");
				noteText.color = TagPalette.NoteText;
				float width = Measure(noteText, NoteSize);
				Place(noteText.rectTransform, -width * 0.5f, y, width, NoteSize + 4f);
				widest = Mathf.Max(widest, width);
				y += NoteSize + 4f + RowGap;
			}

			statsText.gameObject.SetActive(hasStats);
			dot.gameObject.SetActive(hasStats && showDot);
			if (hasStats)
			{
				SetText(statsText, stats);
				float textWidth = Measure(statsText, StatSize);
				float rowWidth = textWidth + (showDot ? DotSize + Gap : 0f);
				float x = -rowWidth * 0.5f;
				if (showDot)
				{
					dot.color = dotColor;
					Place(dot.rectTransform, x, y + (StatSize + 4f - DotSize) * 0.5f, DotSize, DotSize);
					x += DotSize + Gap;
				}

				Place(statsText.rectTransform, x, y, textWidth, StatSize + 4f);
				widest = Mathf.Max(widest, rowWidth);
				y += StatSize + 4f + RowGap;
			}

			SetText(nameText, nameMarkup);
			float nameWidth = Measure(nameText, NameSize);
			bool hasIcon = !string.IsNullOrEmpty(iconId);
			float chipWidth = isNew ? Measure(newText, ChipSize - 1f) + 10f : 0f;
			float nameRow = nameWidth + (hasIcon ? IconSize + Gap : 0f) + (friend ? StarSize + Gap : 0f) + (isNew ? chipWidth + Gap : 0f);
			float nameHeight = NameSize + 6f;
			float cursor = -nameRow * 0.5f;
			icon.gameObject.SetActive(hasIcon);
			if (hasIcon)
			{
				icon.sprite = SpriteFactory.Icon(iconId);
				icon.color = iconColor;
				Place(icon.rectTransform, cursor, y + (nameHeight - IconSize) * 0.5f, IconSize, IconSize);
				cursor += IconSize + Gap;
			}

			Place(nameText.rectTransform, cursor, y, nameWidth, nameHeight);
			cursor += nameWidth + Gap;
			star.gameObject.SetActive(friend);
			if (friend)
			{
				Place(star.rectTransform, cursor, y + (nameHeight - StarSize) * 0.5f, StarSize, StarSize);
				cursor += StarSize + Gap;
			}

			newChip.gameObject.SetActive(isNew);
			if (isNew)
			{
				float chipHeight = ChipSize + 5f;
				Place(newChip.rectTransform, cursor, y + (nameHeight - chipHeight) * 0.5f, chipWidth, chipHeight);
				Place(newText.rectTransform, cursor, y + (nameHeight - chipHeight) * 0.5f, chipWidth, chipHeight);
			}

			widest = Mathf.Max(widest, nameRow);
			y += nameHeight;

			chip.gameObject.SetActive(hasCustom);
			chipText.gameObject.SetActive(hasCustom);
			if (hasCustom)
			{
				y += RowGap + 2f;
				SetText(chipText, Rich.Escape(customText));
				chipText.color = Luminance(customColor) > 0.6f ? new Color(0.08f, 0.08f, 0.12f, 1f) : Color.white;
				chip.color = customColor;
				float width = Measure(chipText, ChipSize) + 16f;
				float height = ChipSize + 7f;
				Place(chip.rectTransform, -width * 0.5f, y, width, height);
				Place(chipText.rectTransform, -width * 0.5f, y, width, height);
				widest = Mathf.Max(widest, width);
				y += height;
			}

			y += Padding;
			card.gameObject.SetActive(background);
			card.color = CardColor;
			float cardWidth = widest + Padding * 2f;
			Place(card.rectTransform, -cardWidth * 0.5f, 0f, cardWidth, y);
			Root.sizeDelta = new Vector2(cardWidth, y);
		}

		private Image NewImage(string name, Sprite sprite, bool sliced)
		{
			Image image = UiKit.NewImage(name, Root, sprite, false);
			image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
			image.preserveAspect = !sliced;
			return image;
		}

		private TextMeshProUGUI NewText(string name, float size, FontStyles style)
		{
			TextMeshProUGUI text = UiKit.NewText(name, Root, size, TextAlignmentOptions.Center, style);
			text.textWrappingMode = TextWrappingModes.NoWrap;
			text.overflowMode = TextOverflowModes.Overflow;
			return text;
		}

		private static void SetText(TextMeshProUGUI text, string value)
		{
			if (!string.Equals(text.text, value, StringComparison.Ordinal))
			{
				text.text = value;
			}
		}

		private static float Measure(TextMeshProUGUI text, float size)
		{
			try
			{
				float width = text.GetPreferredValues(text.text, 2000f, size * 2f).x;
				if (width > 0f && !float.IsNaN(width) && !float.IsInfinity(width))
				{
					return width + 2f;
				}
			}
			catch (Exception)
			{
			}

			return (text.text ?? string.Empty).Length * size * 0.42f;
		}

		private static void Place(RectTransform rect, float x, float y, float width, float height)
		{
			Vector2 anchor = new Vector2(0.5f, 0f);
			rect.anchorMin = anchor;
			rect.anchorMax = anchor;
			rect.pivot = Vector2.zero;
			rect.anchoredPosition = new Vector2(x, y);
			rect.sizeDelta = new Vector2(width, height);
		}

		private static float Luminance(Color color)
		{
			return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
		}
	}
}

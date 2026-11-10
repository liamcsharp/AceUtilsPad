using System.Collections.Generic;
using AUP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP;

internal sealed class CamTablet
{
	private const float Width = 560f;
	private const float Height = 340f;
	private const float UnitScale = 0.0009f;
	private const float RefreshInterval = 0.15f;
	private const float PreviewX = 20f;
	private const float PreviewY = 20f;
	private const float PreviewWidth = 320f;
	private const float PreviewHeight = 180f;
	private const float GrabMarginX = 70f;
	private const float GrabMarginY = 40f;
	private const float GrabDepth = 110f;

	private readonly MenuTouchInput touch = new MenuTouchInput();
	private readonly List<UiButton> targets = new List<UiButton>();
	private readonly List<UiButton> actions = new List<UiButton>();

	private MenuContext context;
	private AceCam cam;
	private GameObject root;
	private RectTransform rect;
	private CanvasGroup group;
	private MenuBody body;
	private MiniPanel panel;
	private Image glass;
	private Image frame;
	private RawImage preview;
	private RawImage gallery;
	private Image flash;
	private Image barTop;
	private Image barBottom;
	private Image recDot;
	private TextMeshProUGUI badge;
	private TextMeshProUGUI status;
	private TextMeshProUGUI presetLabel;
	private Transform holder;
	private Vector3 holdPosition;
	private Quaternion holdRotation;
	private bool holderIsRight;
	private bool leftHeld;
	private bool rightHeld;
	private float refreshClock;
	private bool behind;

	public bool PreviewVisible => root != null && root.activeSelf && !behind;

	public static CamTablet Create(MenuContext context, AceCam cam)
	{
		CamTablet tablet = new CamTablet { context = context, cam = cam };
		tablet.Build();
		WorldUi.Register(tablet.Collect);
		return tablet;
	}

	public void Dispose()
	{
		WorldUi.Unregister(Collect);
		body?.Destroy();
		if (root != null)
		{
			Object.Destroy(root);
		}

		root = null;
	}

	public void PlaceInFront()
	{
		Transform head = PhysicsLayers.Head();
		if (head == null || root == null)
		{
			return;
		}

		Vector3 forward = PhysicsLayers.FlatForward(head);
		root.transform.SetPositionAndRotation(head.position + forward * 0.6f + Vector3.down * 0.12f, Quaternion.LookRotation(forward, Vector3.up));
		holder = null;
	}

	public void LensPose(bool selfie, out Vector3 position, out Quaternion rotation)
	{
		Transform transform = root.transform;
		float scale = transform.lossyScale.x;
		Vector3 top = transform.position + transform.up * ((Height * 0.5f - 10f) * scale);
		if (selfie)
		{
			position = top - transform.forward * 0.02f;
			rotation = Quaternion.LookRotation(-transform.forward, transform.up);
		}
		else
		{
			position = top + transform.forward * 0.04f;
			rotation = transform.rotation;
		}
	}

	public void Collect(List<UiButton> list)
	{
		if (root == null || !root.activeSelf || behind)
		{
			return;
		}

		panel.Collect(list);
	}

	public void Tick(float deltaTime)
	{
		if (root == null)
		{
			return;
		}

		float scale = UnitScale * Mathf.Clamp(cam.Settings.Size.Value, CamSettings.MinSize, CamSettings.MaxSize);
		if (!Mathf.Approximately(root.transform.localScale.x, scale))
		{
			root.transform.localScale = Vector3.one * scale;
		}

		HandleGrab();
		behind = body.UpdateFacing(rect);
		float alpha = behind ? 0f : 1f;
		if (!Mathf.Approximately(group.alpha, alpha))
		{
			group.alpha = alpha;
		}

		if (behind)
		{
			touch.Reset();
			return;
		}

		if (SimpleInputs.Available && !(holder != null && holderIsRight))
		{
			targets.Clear();
			panel.Collect(targets);
			touch.Update(rect, targets);
		}
		else
		{
			touch.Reset();
		}

		panel.Tick(deltaTime);
		Texture texture = cam.PreviewTexture;
		if (!ReferenceEquals(preview.texture, texture))
		{
			preview.texture = texture;
		}

		Rect uv = cam.Mirror ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f);
		if (preview.uvRect != uv)
		{
			preview.uvRect = uv;
		}

		float flashAlpha = cam.Flash * 0.85f;
		if (!Mathf.Approximately(flash.color.a, flashAlpha))
		{
			flash.color = new Color(1f, 1f, 1f, flashAlpha);
		}

		refreshClock -= deltaTime;
		if (refreshClock <= 0f || panel.Dirty)
		{
			refreshClock = PerformanceGuard.Interval(RefreshInterval);
			Refresh();
		}
	}

	private void Refresh()
	{
		MenuTheme theme = context.Theme.Current;
		Color shell = theme.IsAnimated ? theme.Gradient[0] : theme.Panel;
		glass.color = new Color(shell.r, shell.g, shell.b, 1f);
		frame.color = theme.Accent;
		body.Tint(Color.Lerp(theme.Rail, Color.black, theme.IsLight ? 0.05f : 0.3f), theme.Accent);
		panel.Refresh(theme);
		bool cinematic = cam.Settings.Cinematic.Value;
		barTop.gameObject.SetActive(cinematic);
		barBottom.gameObject.SetActive(cinematic);
		bool blink = Time.unscaledTime % 1f < 0.6f;
		recDot.gameObject.SetActive(cam.Recording && blink);
		badge.text = cam.StatusLine;
		status.text = cam.Status;
		status.color = theme.Muted;
		presetLabel.text = AceCam.PresetNames[cam.PresetIndex];
		presetLabel.color = theme.Text;
		Texture shot = gallery.gameObject.activeInHierarchy ? cam.GalleryTexture() : null;
		if (gallery.gameObject.activeInHierarchy)
		{
			gallery.texture = shot;
			gallery.color = shot != null ? Color.white : new Color(0f, 0f, 0f, 0.35f);
		}
	}

	private void HandleGrab()
	{
		bool left = SimpleInputs.LeftGrab;
		bool right = SimpleInputs.RightGrab;
		bool leftPressed = left && !leftHeld;
		bool rightPressed = right && !rightHeld;
		leftHeld = left;
		rightHeld = right;

		if (holder != null)
		{
			bool stillHeld = holderIsRight ? right : left;
			if (!stillHeld)
			{
				holder = null;
				return;
			}

			root.transform.SetPositionAndRotation(holder.TransformPoint(holdPosition), holder.rotation * holdRotation);
			return;
		}

		if (rightPressed && TryGrab(HandSide.Right))
		{
			holderIsRight = true;
		}
		else if (leftPressed && TryGrab(HandSide.Left))
		{
			holderIsRight = false;
		}
	}

	private bool TryGrab(HandSide side)
	{
		Transform hand = Hands.Local(side);
		if (hand == null)
		{
			return false;
		}

		Vector3 local = rect.InverseTransformPoint(hand.position);
		if (Mathf.Abs(local.x) > Width * 0.5f + GrabMarginX || Mathf.Abs(local.y) > Height * 0.5f + GrabMarginY || Mathf.Abs(local.z) > GrabDepth)
		{
			return false;
		}

		holder = hand;
		holdPosition = hand.InverseTransformPoint(root.transform.position);
		holdRotation = Quaternion.Inverse(hand.rotation) * root.transform.rotation;
		GorillaTagger tagger = GorillaTagger.Instance;
		if (tagger != null)
		{
			tagger.StartVibration(side == HandSide.Left, 0.3f, 0.05f);
		}

		return true;
	}

	private void Build()
	{
		root = new GameObject("AceCamTablet", typeof(RectTransform));
		Object.DontDestroyOnLoad(root);
		Canvas canvas = root.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1000f;
		rect = (RectTransform)root.transform;
		rect.sizeDelta = new Vector2(Width, Height);
		rect.localScale = Vector3.one * UnitScale;
		group = root.AddComponent<CanvasGroup>();
		group.interactable = false;
		group.blocksRaycasts = false;

		body = new MenuBody(rect, "Body");
		body.Add(new Vector3(0f, 0f, 1f + 9f), Width + 12f, Height + 12f, 18f, 30f, 5f, 0);
		body.Add(new Vector3(-Width * 0.5f - 20f, 0f, 1f + 12f), 40f, 230f, 24f, 16f, 6f, 1);
		body.Add(new Vector3(Width * 0.5f + 20f, 0f, 1f + 12f), 40f, 230f, 24f, 16f, 6f, 1);
		body.Add(new Vector3(0f, Height * 0.5f - 12f, 1f + 18f + 2.5f), 44f, 44f, 5f, 22f, 2f, 2);
		body.Add(new Vector3(0f, Height * 0.5f - 12f, 1f + 18f + 5.5f), 24f, 24f, 2f, 12f, 0.8f, 3);

		glass = UiKit.NewImage("Glass", rect, SpriteFactory.Rounded(16), false);
		UiKit.Stretch(glass.rectTransform);
		Image selfieLens = UiKit.NewImage("SelfieLens", rect, SpriteFactory.Circle(), false);
		UiKit.PlaceTopLeft(selfieLens.rectTransform, Width * 0.5f - 5f, 5f, 10f, 10f);
		selfieLens.color = new Color(0.02f, 0.02f, 0.03f, 1f);

		frame = UiKit.NewImage("PreviewFrame", rect, SpriteFactory.Rounded(8), false);
		UiKit.PlaceTopLeft(frame.rectTransform, PreviewX - 3f, PreviewY - 3f, PreviewWidth + 6f, PreviewHeight + 6f);
		RectTransform previewRect = UiKit.NewRect("Preview", rect);
		UiKit.PlaceTopLeft(previewRect, PreviewX, PreviewY, PreviewWidth, PreviewHeight);
		preview = previewRect.gameObject.AddComponent<RawImage>();
		preview.raycastTarget = false;
		if (previewRect.GetComponent<Canvas>() == null)
		{
			previewRect.gameObject.AddComponent<Canvas>();
		}

		barTop = Bar(previewRect, 0f);
		barBottom = Bar(previewRect, PreviewHeight - 22f);
		flash = UiKit.NewImage("Flash", previewRect, null, false);
		UiKit.Stretch(flash.rectTransform);
		flash.color = new Color(1f, 1f, 1f, 0f);
		recDot = UiKit.NewImage("Rec", previewRect, SpriteFactory.Circle(), false);
		UiKit.PlaceTopLeft(recDot.rectTransform, PreviewWidth - 20f, 8f, 12f, 12f);
		recDot.color = new Color(1f, 0.2f, 0.2f, 1f);
		Image badgePlate = UiKit.NewImage("BadgePlate", previewRect, SpriteFactory.Rounded(8), false);
		UiKit.PlaceTopLeft(badgePlate.rectTransform, 6f, PreviewHeight - 24f, 150f, 18f);
		badgePlate.color = new Color(0f, 0f, 0f, 0.55f);
		badge = UiKit.NewText("Badge", badgePlate.rectTransform, 8f, TextAlignmentOptions.Center, FontStyles.Bold);
		UiKit.Stretch(badge.rectTransform);
		badge.color = Color.white;

		float y = PreviewY + PreviewHeight + 14f;
		Action("Photo", cam.TakePhoto, null, 17f, y, 100f);
		ActionDynamic(() => cam.Recording ? "Stop" : "Record", cam.ToggleRecording, () => cam.Recording, 121f, y, 100f);
		Action("Selfie", () => cam.SetMode(cam.Settings.CurrentMode == CamMode.Selfie ? CamMode.Tablet : CamMode.Selfie),
			() => cam.Settings.CurrentMode == CamMode.Selfie, 225f, y, 58f);
		Action("Reset", cam.ResetCamera, null, 287f, y, 56f);

		status = UiKit.NewText("Status", rect, 8.5f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
		UiKit.PlaceTopLeft(status.rectTransform, 18f, y + 40f, PreviewWidth + 6f, 16f);

		float presetY = y + 60f;
		Action("<", () => cam.StepPreset(-1), null, 17f, presetY, 36f);
		presetLabel = UiKit.NewText("Preset", rect, 10f, TextAlignmentOptions.Center, FontStyles.Bold);
		UiKit.PlaceTopLeft(presetLabel.rectTransform, 57f, presetY, 126f, 36f);
		Action(">", () => cam.StepPreset(1), null, 187f, presetY, 36f);
		Action("Use Preset", cam.ApplyPreset, null, 227f, presetY, 116f);

		List<MenuPage> pages = new List<MenuPage>
		{
			new DelegatePage(context, "Mode", "camera", ui =>
			{
				CamUi.BuildModes(ui, cam, 2);
				ui.Toggle("Cinematic", () => cam.Settings.Cinematic.Value, () => cam.Settings.Cinematic.Value = !cam.Settings.Cinematic.Value);
				ui.Label("Selfie uses the little lens above the screen.", 2, 7.5f, FontStyles.Normal, Pick.Muted);
			}),
			new DelegatePage(context, "Lens", "camera", ui => CamUi.BuildLens(ui, cam)),
			new DelegatePage(context, "Move", "camera", ui => CamUi.BuildMove(ui, cam)),
			new DelegatePage(context, "Shots", "camera", ui =>
			{
				RectTransform box = ui.Box(106f);
				gallery = box.gameObject.AddComponent<RawImage>();
				gallery.raycastTarget = false;
				CamUi.BuildShotControls(ui, cam);
			})
		};

		panel = new MiniPanel(context, rect, 356f, 16f, 188f, Height - 32f, pages);
		foreach (UiButton action in actions)
		{
			panel.AddExtra(action);
		}
	}

	private static Image Bar(RectTransform parent, float y)
	{
		Image bar = UiKit.NewImage("Letterbox", parent, null, false);
		UiKit.PlaceTopLeft(bar.rectTransform, 0f, y, PreviewWidth, 22f);
		bar.color = Color.black;
		bar.gameObject.SetActive(false);
		return bar;
	}

	private void Action(string text, System.Action click, System.Func<bool> active, float x, float y, float width)
	{
		ActionDynamic(() => text, click, active, x, y, width);
	}

	private void ActionDynamic(System.Func<string> text, System.Action click, System.Func<bool> active, float x, float y, float width)
	{
		UiButton button = ButtonFactory.Create(rect, "Action", new ButtonSpec(text, click, active), width, 36f, 10, 10f, context.RequestRefresh);
		UiKit.PlaceTopLeft(button.Rect, x, y, width, 36f);
		actions.Add(button);
	}
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

public sealed class SplashScreen : MonoBehaviour
{
	private const float Distance = 1.15f;
	private const float PixelScale = 0.0016f;
	private const float FadeIn = 0.5f;
	private const float Hold = 3.2f;
	private const float FadeOut = 0.7f;
	private const float EmblemSize = 190f;

	private CanvasGroup group;
	private OrbEmblem emblem;
	private RectTransform spinner;
	private TextMeshProUGUI welcome;
	private TextMeshProUGUI status;
	private Image bar;
	private float clock = -1f;
	private bool placed;

	public static SplashScreen Create()
	{
		GameObject root = new GameObject("AUPSplash", typeof(RectTransform));
		DontDestroyOnLoad(root);
		Canvas canvas = root.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		root.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1000f;
		SplashScreen splash = root.AddComponent<SplashScreen>();
		splash.Build((RectTransform)root.transform);
		return splash;
	}

	private void Build(RectTransform rect)
	{
		rect.sizeDelta = new Vector2(520f, 360f);
		rect.localScale = Vector3.one * PixelScale;
		group = gameObject.AddComponent<CanvasGroup>();
		group.alpha = 0f;
		group.interactable = false;
		group.blocksRaycasts = false;

		Image card = UiKit.NewImage("Card", rect, SpriteFactory.Rounded(16), false);
		UiKit.Stretch(card.rectTransform);
		card.color = new Color(0.03f, 0.012f, 0.014f, 0.94f);
		Image halo = UiKit.NewImage("Halo", rect, SpriteFactory.Glow(), false);
		halo.type = Image.Type.Simple;
		Center(halo.rectTransform, 0f, 40f, 420f, 420f);
		halo.color = new Color(1f, 0.4f, 0.1f, 0.22f);

		spinner = UiKit.NewRect("Spinner", rect);
		Center(spinner, 0f, 46f, EmblemSize, EmblemSize);
		emblem = OrbEmblem.Create(spinner, EmblemSize);
		Center(emblem.Rect, 0f, 0f, EmblemSize, EmblemSize);
		emblem.Busy = true;

		welcome = UiKit.NewText("Welcome", rect, 26f, TextAlignmentOptions.Center, FontStyles.Bold);
		Center(welcome.rectTransform, 0f, -92f, 500f, 36f);
		welcome.text = "Welcome to " + PluginInfo.Name;
		welcome.color = Color.white;
		status = UiKit.NewText("Status", rect, 12f, TextAlignmentOptions.Center, FontStyles.Normal);
		Center(status.rectTransform, 0f, -122f, 500f, 20f);
		status.color = new Color(1f, 0.72f, 0.55f, 1f);

		Image track = UiKit.NewImage("Track", rect, SpriteFactory.Rounded(8), false);
		Center(track.rectTransform, 0f, -148f, 260f, 6f);
		track.color = new Color(1f, 1f, 1f, 0.12f);
		bar = UiKit.NewImage("Bar", track.rectTransform, SpriteFactory.Rounded(8), false);
		bar.rectTransform.anchorMin = Vector2.zero;
		bar.rectTransform.anchorMax = new Vector2(0f, 1f);
		bar.rectTransform.offsetMin = Vector2.zero;
		bar.rectTransform.offsetMax = Vector2.zero;
		bar.color = new Color(1f, 0.47f, 0.13f, 1f);
	}

	private static void Center(RectTransform rect, float x, float y, float width, float height)
	{
		Vector2 middle = new Vector2(0.5f, 0.5f);
		rect.anchorMin = middle;
		rect.anchorMax = middle;
		rect.pivot = middle;
		rect.anchoredPosition = new Vector2(x, y);
		rect.sizeDelta = new Vector2(width, height);
	}

	private void LateUpdate()
	{
		Camera camera = PhysicsLayers.ViewCamera();
		if (camera == null || GorillaTagger.Instance == null)
		{
			return;
		}

		if (clock < 0f)
		{
			clock = 0f;
			emblem.Burst();
		}

		float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
		clock += deltaTime;
		Transform head = camera.transform;
		Vector3 target = head.position + head.forward * Distance;
		Quaternion facing = Quaternion.LookRotation(target - head.position, Vector3.up);
		if (!placed)
		{
			placed = true;
			transform.SetPositionAndRotation(target, facing);
		}
		else
		{
			float blend = 1f - Mathf.Exp(-6f * deltaTime);
			transform.SetPositionAndRotation(Vector3.Lerp(transform.position, target, blend), Quaternion.Slerp(transform.rotation, facing, blend));
		}

		float total = FadeIn + Hold + FadeOut;
		float alpha = clock < FadeIn ? clock / FadeIn : clock > FadeIn + Hold ? 1f - (clock - FadeIn - Hold) / FadeOut : 1f;
		group.alpha = Mathf.Clamp01(alpha);
		float progress = Mathf.Clamp01(clock / (FadeIn + Hold));
		bar.rectTransform.anchorMax = new Vector2(UiMotion.EaseOutCubic(progress), 1f);
		status.text = progress < 0.35f ? "Warming up the orb..." : progress < 0.7f ? "Loading themes and widgets..." : progress < 1f ? "Almost there..." : "Ready";
		float spin = (1f - UiMotion.EaseOutCubic(clock / 1.6f)) * 720f + Mathf.Sin(clock * 1.6f) * 22f;
		spinner.localRotation = Quaternion.Euler(0f, spin, 0f);
		float pulse = 1f + 0.04f * Mathf.Sin(clock * 3f);
		spinner.localScale = new Vector3(pulse, pulse, 1f);
		if (clock >= total)
		{
			Destroy(gameObject);
		}
	}
}

using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

public sealed class OrbEmblem : MonoBehaviour
{
	private const float RingMajor = 0.36987f;
	private const float RingMinor = 0.11244f;
	private const float DotDiameter = 0.052f;
	private const float StartAngle = -0.116f;
	private const float BreathPeriod = 3.4f;
	private const float BusySpeed = 6f;

	private static readonly Vector2 MajorAxis = new Vector2(0.9395f, 0.3425f);
	private static readonly Vector2 MinorAxis = new Vector2(0.3425f, -0.9395f);
	private static readonly Color GlowColor = new Color(1f, 0.42f, 0.12f, 1f);

	private RectTransform root;
	private Image glow;
	private Image logo;
	private Image dotBack;
	private Image dotFront;
	private float size;
	private float angle = StartAngle;
	private float burst;

	public float Speed { get; set; } = 1.3f;

	public float GlowStrength { get; set; } = 1f;

	public bool Busy { get; set; }

	public static OrbEmblem Create(Transform parent, float size)
	{
		RectTransform rect = UiKit.NewRect("OrbEmblem", parent);
		rect.sizeDelta = new Vector2(size, size);
		OrbEmblem emblem = rect.gameObject.AddComponent<OrbEmblem>();
		emblem.Build(rect, size);
		return emblem;
	}

	public RectTransform Rect => root;

	public void Burst()
	{
		burst = 1f;
	}

	private void Build(RectTransform rect, float emblemSize)
	{
		root = rect;
		size = emblemSize;
		glow = Centered("Glow", SpriteFactory.Glow(), size * 1.3f);
		dotBack = Centered("DotBack", SpriteFactory.Circle(), size * DotDiameter);
		logo = Centered("Logo", SpriteFactory.Logo(), size);
		logo.preserveAspect = true;
		dotFront = Centered("DotFront", SpriteFactory.Circle(), size * DotDiameter);
		dotBack.color = new Color(0.86f, 0.86f, 0.88f, 1f);
		dotFront.color = Color.white;
		Animate(0f);
	}

	private Image Centered(string name, Sprite sprite, float extent)
	{
		Image image = UiKit.NewImage(name, root, sprite, false);
		image.type = Image.Type.Simple;
		RectTransform rect = image.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.sizeDelta = new Vector2(extent, extent);
		rect.anchoredPosition = Vector2.zero;
		return image;
	}

	private void Update()
	{
		Animate(Time.unscaledDeltaTime);
	}

	private void Animate(float deltaTime)
	{
		burst = Mathf.MoveTowards(burst, 0f, deltaTime * 1.4f);
		float speed = (Busy ? BusySpeed : Speed) + burst * burst * 14f;
		angle = Mathf.Repeat(angle + deltaTime * speed, Mathf.PI * 2f);

		float sin = Mathf.Sin(angle);
		Vector2 position = (MajorAxis * (Mathf.Cos(angle) * RingMajor) + MinorAxis * (sin * RingMinor)) * size;
		bool front = sin > 0f;
		float depth = 1f + 0.14f * sin;
		dotFront.enabled = front;
		dotBack.enabled = !front;
		Image dot = front ? dotFront : dotBack;
		dot.rectTransform.anchoredPosition = position;
		dot.rectTransform.localScale = new Vector3(depth, depth, 1f);

		float breath = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / BreathPeriod));
		float strength = Mathf.Clamp01(GlowStrength);
		glow.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, (0.22f + 0.36f * breath + 0.3f * burst) * strength);
		float glowScale = 1f + 0.1f * breath + 0.25f * burst;
		glow.rectTransform.localScale = new Vector3(glowScale, glowScale, 1f);
	}
}

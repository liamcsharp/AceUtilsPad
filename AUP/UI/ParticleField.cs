using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

public sealed class ParticleField : MonoBehaviour
{
	private const int Count = 22;
	private const float StepInterval = 0.033f;

	private readonly Particle[] particles = new Particle[Count];

	private RectTransform container;
	private ParticleStyle style;
	private ParticleStyle builtStyle = (ParticleStyle)(-1);
	private Color baseColor = Color.white;
	private float stepClock;

	public static ParticleField Create(Transform parent)
	{
		RectTransform rect = UiKit.NewRect("Particles", parent);
		UiKit.Stretch(rect);
		ParticleField field = rect.gameObject.AddComponent<ParticleField>();
		field.container = rect;
		field.Spawn();
		return field;
	}

	public void Configure(ParticleStyle particleStyle, bool visible, Color color)
	{
		style = visible ? particleStyle : ParticleStyle.None;
		baseColor = color;
		container.gameObject.SetActive(style != ParticleStyle.None);
		if (style != ParticleStyle.None && style != builtStyle)
		{
			Rebuild();
		}
	}

	private void Spawn()
	{
		for (int i = 0; i < Count; i++)
		{
			Image image = UiKit.NewImage("Particle", container, SpriteFactory.Circle(), false);
			image.type = Image.Type.Simple;
			particles[i] = new Particle { Image = image };
		}
	}

	private void Rebuild()
	{
		builtStyle = style;
		Sprite sprite = SpriteFor(style);
		Rect bounds = container.rect;
		for (int i = 0; i < Count; i++)
		{
			Particle particle = particles[i];
			particle.Position = new Vector2(Random.value, Random.value);
			particle.Phase = Random.value * Mathf.PI * 2f;
			particle.Rotation = Random.Range(0f, 360f);
			particle.Spin = Random.Range(-1f, 1f);
			particle.Shade = Random.value;
			particle.Speed = SpeedFor(style);
			particle.Drift = Random.Range(0.01f, 0.05f);
			particle.Size = SizeFor(style);
			particle.Image.sprite = sprite;
			particle.Image.rectTransform.sizeDelta = style == ParticleStyle.Rain
				? new Vector2(1.2f, particle.Size)
				: style == ParticleStyle.Petals || style == ParticleStyle.Leaves
					? new Vector2(particle.Size * 0.7f, particle.Size)
					: new Vector2(particle.Size, particle.Size);
			particle.Image.rectTransform.localRotation = Quaternion.identity;
			particles[i] = particle;
			Place(particles[i], bounds);
		}
	}

	private static Sprite SpriteFor(ParticleStyle particleStyle)
	{
		switch (particleStyle)
		{
			case ParticleStyle.Petals:
				return SpriteFactory.Petal();
			case ParticleStyle.Leaves:
				return SpriteFactory.Leaf();
			case ParticleStyle.Fluff:
			case ParticleStyle.Crystals:
				return SpriteFactory.Glow();
			default:
				return SpriteFactory.Circle();
		}
	}

	private static float SpeedFor(ParticleStyle particleStyle)
	{
		switch (particleStyle)
		{
			case ParticleStyle.Embers:
				return Random.Range(0.05f, 0.14f);
			case ParticleStyle.Petals:
				return Random.Range(0.04f, 0.09f);
			case ParticleStyle.Leaves:
				return Random.Range(0.03f, 0.07f);
			case ParticleStyle.Rain:
				return Random.Range(0.6f, 0.95f);
			case ParticleStyle.Dust:
				return Random.Range(0.015f, 0.04f);
			case ParticleStyle.Fluff:
				return Random.Range(0.008f, 0.02f);
			default:
				return Random.Range(0.005f, 0.015f);
		}
	}

	private static float SizeFor(ParticleStyle particleStyle)
	{
		switch (particleStyle)
		{
			case ParticleStyle.Embers:
				return Random.Range(2f, 5f);
			case ParticleStyle.Petals:
				return Random.Range(6f, 10f);
			case ParticleStyle.Leaves:
				return Random.Range(7f, 11f);
			case ParticleStyle.Rain:
				return Random.Range(9f, 15f);
			case ParticleStyle.Dust:
				return Random.Range(1.5f, 3.5f);
			case ParticleStyle.Crystals:
				return Random.Range(5f, 10f);
			case ParticleStyle.Fluff:
				return Random.Range(16f, 30f);
			default:
				return Random.Range(2f, 4.5f);
		}
	}

	private void Update()
	{
		if (style == ParticleStyle.None)
		{
			return;
		}

		Rect bounds = container.rect;
		if (bounds.width < 1f)
		{
			return;
		}

		stepClock += Time.unscaledDeltaTime;
		if (stepClock < StepInterval)
		{
			return;
		}

		float deltaTime = Mathf.Min(stepClock, 0.25f);
		stepClock = 0f;
		float time = Time.unscaledTime;
		for (int i = 0; i < Count; i++)
		{
			Advance(ref particles[i], deltaTime, time);
			Place(particles[i], bounds);
			Tint(particles[i], time);
		}
	}

	private void Advance(ref Particle particle, float deltaTime, float time)
	{
		float sway = Mathf.Sin(time * 0.8f + particle.Phase) * particle.Drift * deltaTime;
		Vector2 position = particle.Position;

		switch (style)
		{
			case ParticleStyle.Embers:
				position.x += sway;
				position.y += particle.Speed * deltaTime;
				break;
			case ParticleStyle.Petals:
			case ParticleStyle.Leaves:
				position.x += sway * 3f + 0.02f * deltaTime;
				position.y -= particle.Speed * deltaTime;
				particle.Rotation += particle.Spin * 70f * deltaTime;
				break;
			case ParticleStyle.Rain:
				position.x -= particle.Speed * 0.08f * deltaTime;
				position.y -= particle.Speed * deltaTime;
				break;
			case ParticleStyle.Dust:
			case ParticleStyle.Fluff:
				position.x += particle.Speed * deltaTime;
				position.y += Mathf.Sin(time * 0.5f + particle.Phase) * particle.Drift * 0.5f * deltaTime;
				break;
			default:
				position.x += sway * 0.4f;
				position.y += particle.Speed * deltaTime;
				break;
		}

		position.x = Mathf.Repeat(position.x, 1f);
		position.y = Mathf.Repeat(position.y, 1f);
		particle.Position = position;
	}

	private void Place(Particle particle, Rect bounds)
	{
		RectTransform rect = particle.Image.rectTransform;
		rect.anchoredPosition = new Vector2(
			Mathf.Lerp(bounds.xMin, bounds.xMax, particle.Position.x),
			Mathf.Lerp(bounds.yMin, bounds.yMax, particle.Position.y));
		if (style == ParticleStyle.Petals || style == ParticleStyle.Leaves)
		{
			rect.localRotation = Quaternion.Euler(0f, 0f, particle.Rotation);
		}
		else if (style == ParticleStyle.Rain)
		{
			rect.localRotation = Quaternion.Euler(0f, 0f, -5f);
		}
	}

	private void Tint(Particle particle, float time)
	{
		float twinkle = 0.5f + 0.5f * Mathf.Sin(time * 2.2f + particle.Phase);
		float edge = Mathf.Clamp01(Mathf.Min(particle.Position.y, 1f - particle.Position.y) * 8f);
		float alpha;
		Color color = baseColor;
		switch (style)
		{
			case ParticleStyle.Embers:
				alpha = 0.35f + 0.55f * twinkle;
				color = Color.Lerp(baseColor, new Color(1f, 0.9f, 0.4f, 1f), twinkle * 0.6f);
				break;
			case ParticleStyle.Petals:
				alpha = 0.85f;
				color = Color.Lerp(baseColor, Color.white, particle.Shade * 0.4f);
				break;
			case ParticleStyle.Leaves:
				alpha = 0.8f;
				color = Color.Lerp(baseColor, new Color(0.85f, 0.65f, 0.25f, 1f), particle.Shade * 0.5f);
				break;
			case ParticleStyle.Rain:
				alpha = 0.35f;
				color = particle.Shade > 0.65f ? new Color(1f, 0.35f, 0.75f, 1f) : baseColor;
				break;
			case ParticleStyle.Dust:
				alpha = 0.22f + 0.2f * twinkle;
				break;
			case ParticleStyle.Crystals:
				alpha = 0.1f + 0.7f * twinkle * twinkle;
				color = particle.Shade > 0.6f ? new Color(0.75f, 0.5f, 1f, 1f) : baseColor;
				break;
			case ParticleStyle.Fluff:
				alpha = 0.45f;
				break;
			default:
				alpha = 0.15f + 0.65f * twinkle * twinkle;
				break;
		}

		particle.Image.color = new Color(color.r, color.g, color.b, alpha * edge);
	}

	private struct Particle
	{
		public Image Image;
		public Vector2 Position;
		public float Speed;
		public float Phase;
		public float Size;
		public float Drift;
		public float Rotation;
		public float Spin;
		public float Shade;
	}
}

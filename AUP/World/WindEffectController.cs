using UnityEngine;

namespace AUP;

public sealed class WindEffectController : MonoBehaviour
{
	private static readonly string[] StrengthNames = { "Off", "Breeze", "Gale", "Storm" };
	private static readonly string[] CompassNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
	private static readonly float[] Speeds = { 0f, 4f, 9f, 16f };
	private static readonly float[] Rates = { 0f, 45f, 120f, 240f };

	private const float VolumeWidth = 14f;
	private const float VolumeHeight = 7f;
	private const float StreakAlpha = 0.4f;
	private const float GustFrequency = 0.25f;
	private const float DegreesPerStep = 360f / WindSettings.DirectionCount;
	private const float BlowAwayFromSourceDegrees = 180f;
	private const int MaxParticles = 700;

	private WindSettings settings;
	private ParticleSystem particles;
	private Material material;

	public void Initialize(WindSettings windSettings)
	{
		settings = windSettings;
	}

	public string Describe()
	{
		int strength = settings.StrengthLevel;
		if (strength == 0)
		{
			return "Wind is off.";
		}

		string gusts = settings.Gusts.Value ? ", gusting" : string.Empty;
		return StrengthNames[strength] + " from the " + CompassNames[settings.DirectionIndex] + gusts;
	}

	private void Update()
	{
		if (settings == null)
		{
			return;
		}

		int strength = settings.StrengthLevel;
		Camera view = Camera.main;
		if (strength == 0 || view == null)
		{
			StopEffect();
			return;
		}

		EnsureParticles();
		float gust = settings.Gusts.Value ? Mathf.Lerp(0.6f, 1.3f, Mathf.PerlinNoise(Time.time * GustFrequency, 0.5f)) : 1f;
		Aim(view.transform.position, settings.DirectionIndex * DegreesPerStep + BlowAwayFromSourceDegrees);
		Apply(Speeds[strength] * gust, Rates[strength] * gust);

		if (!particles.isPlaying)
		{
			particles.Play();
		}
	}

	private void OnDestroy()
	{
		if (particles != null)
		{
			Destroy(particles.gameObject);
		}

		if (material != null)
		{
			Destroy(material);
		}
	}

	private void StopEffect()
	{
		if (particles != null && particles.isPlaying)
		{
			particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		}
	}

	private void Aim(Vector3 center, float yawDegrees)
	{
		Transform emitter = particles.transform;
		emitter.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
		emitter.position = center - emitter.forward * (VolumeWidth * 0.5f);
	}

	private void Apply(float speed, float rate)
	{
		ParticleSystem.MainModule main = particles.main;
		main.startSpeed = speed;
		main.startLifetime = Mathf.Clamp(VolumeWidth / Mathf.Max(speed, 0.1f), 0.6f, 3.5f);

		ParticleSystem.EmissionModule emission = particles.emission;
		emission.rateOverTime = rate;
	}

	private void EnsureParticles()
	{
		if (particles != null)
		{
			return;
		}

		GameObject host = new GameObject("AUPWindEffect");
		particles = host.AddComponent<ParticleSystem>();
		particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		ParticleSystem.MainModule main = particles.main;
		main.loop = true;
		main.playOnAwake = false;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.maxParticles = MaxParticles;
		main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
		main.startColor = new Color(1f, 1f, 1f, StreakAlpha);

		ParticleSystem.ShapeModule shape = particles.shape;
		shape.enabled = true;
		shape.shapeType = ParticleSystemShapeType.Box;
		shape.scale = new Vector3(VolumeWidth, VolumeHeight, 1f);

		ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
		colorOverLifetime.enabled = true;
		colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildFadeGradient());

		ParticleSystemRenderer particleRenderer = host.GetComponent<ParticleSystemRenderer>();
		particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
		particleRenderer.velocityScale = 0.05f;
		particleRenderer.lengthScale = 2f;
		material = Materials.CreateUnlit(Color.white);
		particleRenderer.sharedMaterial = material;
	}

	private static Gradient BuildFadeGradient()
	{
		Gradient gradient = new Gradient();
		gradient.SetKeys(
			new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
			new[]
			{
				new GradientAlphaKey(0f, 0f),
				new GradientAlphaKey(1f, 0.2f),
				new GradientAlphaKey(1f, 0.8f),
				new GradientAlphaKey(0f, 1f)
			});
		return gradient;
	}
}

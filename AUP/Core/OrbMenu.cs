using System;
using AUP.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AUP;

public enum OrbState
{
	Hidden,
	Floating,
	Held,
	Flying,
	Deployed
}

internal sealed class OrbBody : MonoBehaviour
{
	public bool Touched;

	private void OnCollisionEnter(Collision collision)
	{
		Touched = true;
	}
}

public sealed class OrbMenu : MonoBehaviour
{
	public const float DeployScale = 2.4f;

	private const float OrbRadius = 0.065f;
	private const float EmblemPixels = 256f;
	private const float EmblemWorldSize = 0.19f;
	private const float GrabRadius = 0.2f;
	private const float MouseGrabRadius = 0.22f;
	private const float SummonDistance = 0.5f;
	private const float SummonDrop = 0.12f;
	private const float ThrowBoost = 1.25f;
	private const float MaxThrowSpeed = 18f;
	private const float PlaceSpeed = 0.9f;
	private const float SettleSpeed = 0.45f;
	private const float SettleTime = 0.15f;
	private const float MaxFlightTime = 2.6f;
	private const float FallDepth = 25f;
	private const float HoverGap = 0.13f;
	private const float MinMenuDistance = 0.75f;
	private const float MouseMinDistance = 0.6f;
	private const float MouseMaxDistance = 10f;
	private const float MouseStep = 0.35f;
	private const float BurstDuration = 0.55f;
	private const int VelocitySamples = 6;

	private readonly Vector3[] samplePositions = new Vector3[VelocitySamples];
	private readonly float[] sampleTimes = new float[VelocitySamples];
	private readonly BurstRing[] bursts = new BurstRing[2];

	private MenuSettings settings;
	private GameObject root;
	private Rigidbody body;
	private SphereCollider sphere;
	private OrbBody contact;
	private RectTransform visual;
	private OrbEmblem emblem;
	private Grabber holder = Grabber.None;
	private int sampleCount;
	private int sampleIndex;
	private Vector3 floatAnchor;
	private Vector3 launchPoint;
	private Vector3 menuCenter;
	private Quaternion menuRotation = Quaternion.identity;
	private float flightClock;
	private float settleClock;
	private float mouseDistance = 2f;
	private bool leftGripHeld;
	private bool rightGripHeld;
	private bool mouseHeld;

	public event Action Deployed;

	public event Action Recalled;

	private enum Grabber
	{
		None,
		Left,
		Right,
		Mouse
	}

	public OrbState State { get; private set; } = OrbState.Hidden;

	public bool IsDeployed => State == OrbState.Deployed;

	public Vector3 MenuCenter => menuCenter;

	public Quaternion MenuRotation => menuRotation;

	public Vector3 OrbPosition => root != null ? root.transform.position : menuCenter;

	public string StatusText
	{
		get
		{
			if (settings == null || !settings.OrbMode.Value)
			{
				return "Orb Menu is off";
			}

			switch (State)
			{
				case OrbState.Floating:
					return "The orb is floating in front of you";
				case OrbState.Held:
					return "Holding the orb. Let go to throw it";
				case OrbState.Flying:
					return "Orb in flight...";
				case OrbState.Deployed:
					return "Menu is open where the orb landed";
				default:
					return "Calling the orb...";
			}
		}
	}

	public void Initialize(MenuSettings menuSettings)
	{
		settings = menuSettings;
	}

	public void Summon()
	{
		if (settings == null || !settings.OrbMode.Value)
		{
			return;
		}

		Transform head = PhysicsLayers.Head();
		if (head == null)
		{
			return;
		}

		EnsureBuilt(head.position);
		if (root == null)
		{
			return;
		}

		bool wasDeployed = State == OrbState.Deployed;
		holder = Grabber.None;
		floatAnchor = head.position + PhysicsLayers.FlatForward(head) * SummonDistance + Vector3.down * SummonDrop;
		Freeze();
		root.transform.position = floatAnchor;
		State = OrbState.Floating;
		emblem.Burst();
		SpawnBurst(floatAnchor, Quaternion.LookRotation(PhysicsLayers.FlatForward(head)), 0.35f);
		if (wasDeployed)
		{
			Recalled?.Invoke();
		}
	}

	private void Update()
	{
		bool wanted = settings != null && settings.OrbMode.Value;
		if (!wanted)
		{
			if (root != null || State != OrbState.Hidden)
			{
				Despawn();
			}

			return;
		}

		if (root == null)
		{
			Summon();
			if (root == null)
			{
				return;
			}
		}

		float deltaTime = Time.deltaTime;
		HandleGrabInput();
		switch (State)
		{
			case OrbState.Floating:
				Bob(floatAnchor, deltaTime);
				break;
			case OrbState.Held:
				FollowHolder();
				break;
			case OrbState.Flying:
				UpdateFlight(deltaTime);
				break;
			case OrbState.Deployed:
				Bob(HoverPoint(), deltaTime);
				break;
		}

		AdvanceBursts(Time.unscaledDeltaTime);
	}

	private void LateUpdate()
	{
		if (root == null || visual == null)
		{
			return;
		}

		if (State == OrbState.Held)
		{
			FollowHolder();
		}

		Camera camera = PhysicsLayers.ViewCamera();
		if (camera != null)
		{
			Vector3 away = visual.position - camera.transform.position;
			if (away.sqrMagnitude > 0.0001f)
			{
				visual.rotation = Quaternion.LookRotation(away);
			}
		}
	}

	private void OnDestroy()
	{
		Despawn();
		foreach (BurstRing burst in bursts)
		{
			if (burst != null && burst.Root != null)
			{
				Destroy(burst.Root.gameObject);
			}
		}
	}

	private void Despawn()
	{
		bool wasDeployed = State == OrbState.Deployed;
		State = OrbState.Hidden;
		holder = Grabber.None;
		if (root != null)
		{
			Destroy(root);
		}

		root = null;
		body = null;
		sphere = null;
		contact = null;
		visual = null;
		emblem = null;
		if (wasDeployed)
		{
			Recalled?.Invoke();
		}
	}

	private void EnsureBuilt(Vector3 near)
	{
		if (root != null)
		{
			return;
		}

		try
		{
			root = new GameObject("AceUtilsPadOrb");
			DontDestroyOnLoad(root);
			sphere = root.AddComponent<SphereCollider>();
			sphere.radius = OrbRadius;
			body = root.AddComponent<Rigidbody>();
			body.mass = 0.3f;
			body.linearDamping = 0.15f;
			body.angularDamping = 0.6f;
			body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			body.isKinematic = true;
			sphere.sharedMaterial = new PhysicsMaterial("AUPOrb")
			{
				bounciness = 0.35f,
				dynamicFriction = 0.6f,
				staticFriction = 0.6f,
				bounceCombine = PhysicsMaterialCombine.Maximum
			};
			contact = root.AddComponent<OrbBody>();

			GameObject canvasObject = new GameObject("OrbVisual", typeof(RectTransform));
			canvasObject.transform.SetParent(root.transform, false);
			Canvas canvas = canvasObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			canvasObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 1000f;
			visual = (RectTransform)canvasObject.transform;
			visual.sizeDelta = new Vector2(EmblemPixels, EmblemPixels);
			visual.localScale = Vector3.one * (EmblemWorldSize / EmblemPixels);
			emblem = OrbEmblem.Create(visual, EmblemPixels);
			RectTransform emblemRect = emblem.Rect;
			emblemRect.anchorMin = new Vector2(0.5f, 0.5f);
			emblemRect.anchorMax = new Vector2(0.5f, 0.5f);
			emblemRect.pivot = new Vector2(0.5f, 0.5f);
			emblemRect.anchoredPosition = Vector2.zero;

			PhysicsLayers.SetLayer(root, PhysicsLayers.PickLayer(near));
			PhysicsLayers.IgnorePlayer(root);
		}
		catch (Exception exception)
		{
			Debug.LogError("[AceUtilsPad] Could not build the orb: " + exception);
			if (root != null)
			{
				Destroy(root);
			}

			root = null;
		}
	}

	private void HandleGrabInput()
	{
		bool leftGrip = SimpleInputs.LeftGrab;
		bool rightGrip = SimpleInputs.RightGrab;
		Mouse mouse = Mouse.current;
		bool mouseGrip = mouse != null && mouse.rightButton.isPressed;

		bool leftPressed = leftGrip && !leftGripHeld;
		bool rightPressed = rightGrip && !rightGripHeld;
		bool mousePressed = mouseGrip && !mouseHeld;
		leftGripHeld = leftGrip;
		rightGripHeld = rightGrip;
		mouseHeld = mouseGrip;

		if (State == OrbState.Held)
		{
			bool released = holder == Grabber.Left && !leftGrip || holder == Grabber.Right && !rightGrip || holder == Grabber.Mouse && !mouseGrip;
			if (holder == Grabber.Mouse && mouse != null)
			{
				float scroll = mouse.scroll.ReadValue().y;
				if (Mathf.Abs(scroll) > 0.01f)
				{
					mouseDistance = Mathf.Clamp(mouseDistance + Mathf.Sign(scroll) * MouseStep, MouseMinDistance, MouseMaxDistance);
				}
			}

			if (released)
			{
				Release();
			}

			return;
		}

		if (rightPressed && HandNear(HandSide.Right))
		{
			Grab(Grabber.Right);
		}
		else if (leftPressed && HandNear(HandSide.Left))
		{
			Grab(Grabber.Left);
		}
		else if (mousePressed && MouseOnOrb())
		{
			Grab(Grabber.Mouse);
		}
	}

	private bool HandNear(HandSide side)
	{
		Transform hand = Hands.Local(side);
		return hand != null && root != null && Vector3.Distance(hand.position, root.transform.position) <= GrabRadius;
	}

	private bool MouseOnOrb()
	{
		Camera camera = PhysicsLayers.ViewCamera();
		if (camera == null || root == null)
		{
			return false;
		}

		Ray ray = camera.ScreenPointToRay(PhysicsLayers.PointerPosition());
		Vector3 toOrb = root.transform.position - ray.origin;
		float along = Vector3.Dot(toOrb, ray.direction);
		if (along <= 0f || along > MouseMaxDistance * 2f)
		{
			return false;
		}

		float miss = (toOrb - ray.direction * along).magnitude;
		if (miss > MouseGrabRadius + along * 0.02f)
		{
			return false;
		}

		mouseDistance = Mathf.Clamp(along, MouseMinDistance, MouseMaxDistance);
		return true;
	}

	private void Grab(Grabber grabber)
	{
		bool wasDeployed = State == OrbState.Deployed;
		holder = grabber;
		State = OrbState.Held;
		Freeze();
		sampleCount = 0;
		Vibrate(grabber, 0.35f, 0.06f);
		emblem.Burst();
		if (wasDeployed)
		{
			Recalled?.Invoke();
		}
	}

	private void Release()
	{
		Vector3 velocity = Vector3.ClampMagnitude(TrackedVelocity() * ThrowBoost, MaxThrowSpeed);
		Grabber released = holder;
		holder = Grabber.None;
		if (velocity.magnitude < PlaceSpeed)
		{
			Deploy(root.transform.position);
			return;
		}

		PhysicsLayers.IgnorePlayer(root);
		sphere.isTrigger = false;
		body.isKinematic = false;
		body.interpolation = RigidbodyInterpolation.Interpolate;
		body.linearVelocity = velocity;
		body.angularVelocity = UnityEngine.Random.insideUnitSphere * 6f;
		contact.Touched = false;
		flightClock = 0f;
		settleClock = 0f;
		launchPoint = root.transform.position;
		State = OrbState.Flying;
		Vibrate(released, 0.25f, 0.04f);
	}

	private void FollowHolder()
	{
		if (!TryHolderPose(out Vector3 position))
		{
			return;
		}

		root.transform.position = position;
		RecordSample(position);
	}

	private bool TryHolderPose(out Vector3 position)
	{
		position = Vector3.zero;
		switch (holder)
		{
			case Grabber.Left:
			case Grabber.Right:
				Transform hand = Hands.Local(holder == Grabber.Left ? HandSide.Left : HandSide.Right);
				if (hand == null)
				{
					return false;
				}

				position = hand.position;
				return true;
			case Grabber.Mouse:
				Camera camera = PhysicsLayers.ViewCamera();
				if (camera == null)
				{
					return false;
				}

				position = camera.ScreenPointToRay(PhysicsLayers.PointerPosition()).GetPoint(mouseDistance);
				return true;
			default:
				return false;
		}
	}

	private void RecordSample(Vector3 position)
	{
		float now = Time.time;
		int previous = (sampleIndex - 1 + VelocitySamples) % VelocitySamples;
		if (sampleCount > 0 && now - sampleTimes[previous] < 0.0001f)
		{
			samplePositions[previous] = position;
			return;
		}

		samplePositions[sampleIndex] = position;
		sampleTimes[sampleIndex] = now;
		sampleIndex = (sampleIndex + 1) % VelocitySamples;
		sampleCount = Mathf.Min(sampleCount + 1, VelocitySamples);
	}

	private Vector3 TrackedVelocity()
	{
		if (sampleCount < 2)
		{
			return Vector3.zero;
		}

		int newest = (sampleIndex - 1 + VelocitySamples) % VelocitySamples;
		int oldest = (sampleIndex - sampleCount + VelocitySamples) % VelocitySamples;
		float span = sampleTimes[newest] - sampleTimes[oldest];
		return span > 0.0001f ? (samplePositions[newest] - samplePositions[oldest]) / span : Vector3.zero;
	}

	private void UpdateFlight(float deltaTime)
	{
		flightClock += deltaTime;
		Vector3 position = root.transform.position;
		if (position.y < launchPoint.y - FallDepth || !Vectors.IsFinite(position))
		{
			Summon();
			return;
		}

		float speed = body.linearVelocity.magnitude;
		bool resting = contact.Touched && speed < 2.5f || speed < SettleSpeed && flightClock > 0.25f;
		settleClock = resting ? settleClock + deltaTime : 0f;
		if (settleClock >= SettleTime || flightClock >= MaxFlightTime)
		{
			Deploy(position);
		}
	}

	private void Deploy(Vector3 point)
	{
		Transform head = PhysicsLayers.Head();
		Vector3 headPosition = head != null ? head.position : point + Vector3.up * 1.4f;
		Freeze();

		Vector3 center = point;
		center.y = point.y + Mathf.Clamp(headPosition.y - 0.2f - point.y, 0.45f, 1.6f);
		Vector3 flat = center - headPosition;
		flat.y = 0f;
		if (flat.magnitude < MinMenuDistance)
		{
			Vector3 direction = flat.sqrMagnitude > 0.0001f ? flat.normalized : (head != null ? PhysicsLayers.FlatForward(head) : Vector3.forward);
			center = new Vector3(headPosition.x, center.y, headPosition.z) + direction * MinMenuDistance;
			flat = center - headPosition;
			flat.y = 0f;
		}

		menuCenter = center;
		menuRotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
		State = OrbState.Deployed;
		emblem.Burst();
		SpawnBurst(point, menuRotation, 1.4f);
		SpawnBurst(point + Vector3.up * 0.02f, Quaternion.Euler(90f, 0f, 0f), 1.1f);
		Vibrate(Grabber.Left, 0.2f, 0.05f);
		Vibrate(Grabber.Right, 0.2f, 0.05f);
		Deployed?.Invoke();
	}

	private Vector3 HoverPoint()
	{
		float halfHeight = MenuSurface.MainHeight * 0.5f * MenuSurface.WorldScale * settings.SizeScale * DeployScale;
		return menuCenter + menuRotation * Vector3.up * (halfHeight + HoverGap);
	}

	private void Bob(Vector3 anchor, float deltaTime)
	{
		Vector3 target = anchor + Vector3.up * (Mathf.Sin(Time.time * 2.1f) * 0.02f);
		root.transform.position = Vector3.Lerp(root.transform.position, target, 1f - Mathf.Exp(-10f * deltaTime));
	}

	private void Freeze()
	{
		if (body == null)
		{
			return;
		}

		if (!body.isKinematic)
		{
			body.linearVelocity = Vector3.zero;
			body.angularVelocity = Vector3.zero;
		}

		body.isKinematic = true;
		body.interpolation = RigidbodyInterpolation.None;
		sphere.isTrigger = true;
		root.transform.rotation = Quaternion.identity;
	}

	private static void Vibrate(Grabber grabber, float strength, float duration)
	{
		GorillaTagger tagger = GorillaTagger.Instance;
		if (tagger == null || grabber == Grabber.Mouse || grabber == Grabber.None)
		{
			return;
		}

		tagger.StartVibration(grabber == Grabber.Left, strength, duration);
	}

	private void SpawnBurst(Vector3 position, Quaternion rotation, float size)
	{
		int index = 0;
		if (bursts[0] != null && bursts[0].Age >= 0f)
		{
			index = bursts[1] == null || bursts[1].Age < 0f ? 1 : bursts[0].Age > bursts[1].Age ? 0 : 1;
		}

		BurstRing burst = Prepare(index);

		burst.Root.SetPositionAndRotation(position, rotation);
		burst.Size = size;
		burst.Age = 0f;
		burst.Root.gameObject.SetActive(true);
		burst.Root.localScale = Vector3.one * 0.0001f;
	}

	private BurstRing Prepare(int index)
	{
		if (bursts[index] != null && bursts[index].Root != null)
		{
			return bursts[index];
		}

		GameObject canvasObject = new GameObject("AUPOrbBurst", typeof(RectTransform));
		DontDestroyOnLoad(canvasObject);
		Canvas canvas = canvasObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		RectTransform rect = (RectTransform)canvasObject.transform;
		rect.sizeDelta = new Vector2(100f, 100f);
		Image ring = UiKit.NewImage("Ring", rect, SpriteFactory.Ring(), false);
		ring.type = Image.Type.Simple;
		UiKit.Stretch(ring.rectTransform);
		Image glow = UiKit.NewImage("Glow", rect, SpriteFactory.Glow(), false);
		glow.type = Image.Type.Simple;
		UiKit.Stretch(glow.rectTransform);
		canvasObject.SetActive(false);
		bursts[index] = new BurstRing { Root = rect, Ring = ring, Glow = glow, Age = -1f };
		return bursts[index];
	}

	private void AdvanceBursts(float deltaTime)
	{
		foreach (BurstRing burst in bursts)
		{
			if (burst == null || burst.Root == null || burst.Age < 0f)
			{
				continue;
			}

			burst.Age += deltaTime;
			float t = burst.Age / BurstDuration;
			if (t >= 1f)
			{
				burst.Age = -1f;
				burst.Root.gameObject.SetActive(false);
				continue;
			}

			float eased = UiMotion.EaseOutCubic(t);
			float worldSize = Mathf.Lerp(0.05f, burst.Size, eased);
			burst.Root.localScale = Vector3.one * (worldSize / 100f);
			burst.Ring.color = new Color(1f, 0.55f, 0.2f, 0.9f * (1f - t));
			burst.Glow.color = new Color(1f, 0.45f, 0.12f, 0.35f * (1f - eased));
		}
	}

	private sealed class BurstRing
	{
		public RectTransform Root;
		public Image Ring;
		public Image Glow;
		public float Size;
		public float Age = -1f;
	}
}

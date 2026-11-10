using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AUP;

public sealed class GunPointer : MonoBehaviour
{
	private const float LineWidth = 0.012f;
	private const float ReticleSize = 0.12f;
	private const float FollowSpeed = 15f;
	private const float PlayerHeightOffset = 0.2f;
	private const float AimAssistDegrees = 6f;
	private const float RigRefreshInterval = 0.5f;
	private const float MouseRange = 30f;

	private static readonly Color IdleColor = new Color(0.72f, 0.74f, 0.95f, 0.85f);
	private static readonly Color HoverColor = new Color(0.45f, 0.95f, 0.6f, 1f);
	private static readonly Color LockedColor = new Color(1f, 0.82f, 0.25f, 1f);

	private readonly List<VRRig> rigs = new List<VRRig>();

	private MenuSettings settings;
	private PlayerSelection selection;
	private Func<bool> menuOpen;
	private Func<bool> typing;
	private LineRenderer line;
	private Transform reticle;
	private Material lineMaterial;
	private Material reticleMaterial;
	private VRRig lockedRig;
	private Vector3 endPoint;
	private bool endPointValid;
	private bool triggerHeld;
	private float nextRigRefresh;

	public event Action<VRRig> LockedOn;

	private float MaxDistance => settings.PointerRange.Value;

	public void Initialize(MenuSettings menuSettings, PlayerSelection playerSelection, Func<bool> isMenuOpen, Func<bool> isTyping)
	{
		settings = menuSettings;
		selection = playerSelection;
		menuOpen = isMenuOpen;
		typing = isTyping;
	}

	private void Update()
	{
		HandleMouseSelect();
		Transform hand = Hands.Local(HandSide.Right);
		bool active = hand != null && settings.GunRay.Value && SimpleInputs.RightGrab && menuOpen();
		if (!active)
		{
			Hide();
			ReleaseLock();
			triggerHeld = false;
			return;
		}

		if (selection.Locked && lockedRig == null)
		{
			selection.SetLocked(false);
		}

		EnsureVisuals();
		bool trigger = SimpleInputs.RightTrigger;
		bool clicked = trigger && !triggerHeld;
		triggerHeld = trigger;

		Vector3 origin = hand.position;
		Vector3 forward = hand.forward;
		VRRig hovered = FindHovered(origin, forward, MaxDistance, out Vector3 hoverPoint);

		if (clicked)
		{
			HandleClick(hovered);
		}
		else if (lockedRig == null && hovered != null && settings.AutoLock.Value)
		{
			LockOn(hovered);
		}

		Vector3 target = origin + forward * MaxDistance;
		if (lockedRig != null)
		{
			target = TargetPoint(lockedRig);
		}
		else if (hovered != null)
		{
			target = hoverPoint;
		}

		Draw(origin, target, lockedRig != null ? LockedColor : hovered != null ? HoverColor : IdleColor);
	}

	private void HandleMouseSelect()
	{
		Mouse mouse = Mouse.current;
		Keyboard keyboard = Keyboard.current;
		bool middle = mouse != null && mouse.middleButton.wasPressedThisFrame;
		bool key = keyboard != null && keyboard.fKey.wasPressedThisFrame && (typing == null || !typing());
		if (!middle && !key)
		{
			return;
		}

		Camera camera = PhysicsLayers.ViewCamera();
		if (camera == null)
		{
			return;
		}

		Ray ray = camera.ScreenPointToRay(PhysicsLayers.PointerPosition());
		VRRig hovered = FindHovered(ray.origin, ray.direction, MouseRange, out _);
		if (hovered == null)
		{
			return;
		}

		selection.Select(hovered);
		LockedOn?.Invoke(hovered);
		UI.Feedback.Click();
	}

	private void OnDisable()
	{
		Hide();
		ReleaseLock();
	}

	private void OnDestroy()
	{
		if (line != null)
		{
			Destroy(line.gameObject);
		}

		if (reticle != null)
		{
			Destroy(reticle.gameObject);
		}

		if (lineMaterial != null)
		{
			Destroy(lineMaterial);
		}

		if (reticleMaterial != null)
		{
			Destroy(reticleMaterial);
		}
	}

	private void HandleClick(VRRig hovered)
	{
		if (hovered != null && hovered != lockedRig)
		{
			LockOn(hovered);
			return;
		}

		ReleaseLock();
	}

	private void LockOn(VRRig rig)
	{
		lockedRig = rig;
		selection.Select(rig);
		selection.SetLocked(true);
		LockedOn?.Invoke(rig);
	}

	private void ReleaseLock()
	{
		if (lockedRig == null && !selection.Locked)
		{
			return;
		}

		lockedRig = null;
		selection.SetLocked(false);
	}

	private VRRig FindHovered(Vector3 origin, Vector3 direction, float range, out Vector3 point)
	{
		point = origin + direction * range;
		if (Physics.Raycast(origin, direction, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
		{
			point = hit.point;
			VRRig struck = hit.collider.GetComponentInParent<VRRig>();
			if (struck != null && !struck.isLocal)
			{
				return struck;
			}
		}

		return FindNearestToRay(origin, direction, range, ref point);
	}

	private VRRig FindNearestToRay(Vector3 origin, Vector3 direction, float range, ref Vector3 point)
	{
		RefreshRigs();
		VRRig best = null;
		float bestAngle = AimAssistDegrees;
		foreach (VRRig rig in rigs)
		{
			if (rig == null)
			{
				continue;
			}

			Vector3 toRig = TargetPoint(rig) - origin;
			float distance = toRig.magnitude;
			if (distance < 0.3f || distance > range)
			{
				continue;
			}

			float angle = Vector3.Angle(direction, toRig);
			if (angle < bestAngle)
			{
				bestAngle = angle;
				best = rig;
			}
		}

		if (best != null)
		{
			point = TargetPoint(best);
		}

		return best;
	}

	private void RefreshRigs()
	{
		if (Time.unscaledTime < nextRigRefresh)
		{
			return;
		}

		nextRigRefresh = Time.unscaledTime + RigRefreshInterval;
		rigs.Clear();
		rigs.AddRange(PlayerInfo.RemoteRigs());
	}

	private static Vector3 TargetPoint(VRRig rig)
	{
		return rig.transform.position + Vector3.up * PlayerHeightOffset;
	}

	private void Draw(Vector3 origin, Vector3 target, Color color)
	{
		float blend = endPointValid ? Mathf.Clamp01(Time.deltaTime * FollowSpeed) : 1f;
		endPoint = Vector3.Lerp(endPointValid ? endPoint : target, target, blend);
		endPointValid = true;

		line.SetPosition(0, origin);
		line.SetPosition(1, endPoint);
		line.startColor = color;
		line.endColor = new Color(color.r, color.g, color.b, 0.35f);
		reticleMaterial.color = color;
		reticle.position = endPoint;
	}

	private void Hide()
	{
		endPointValid = false;
		if (line != null && line.gameObject.activeSelf)
		{
			line.gameObject.SetActive(false);
			reticle.gameObject.SetActive(false);
		}
	}

	private void EnsureVisuals()
	{
		if (line == null)
		{
			lineMaterial = Materials.CreateUnlit(IdleColor);
			GameObject lineObject = new GameObject("AUPGunRay");
			line = lineObject.AddComponent<LineRenderer>();
			line.positionCount = 2;
			line.startWidth = LineWidth;
			line.endWidth = LineWidth * 0.6f;
			line.material = lineMaterial;
			line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			line.receiveShadows = false;
		}

		if (reticle == null)
		{
			reticleMaterial = Materials.CreateUnlit(IdleColor);
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = "AUPGunReticle";
			sphere.transform.localScale = Vector3.one * ReticleSize;
			Destroy(sphere.GetComponent<Collider>());
			sphere.GetComponent<Renderer>().sharedMaterial = reticleMaterial;
			reticle = sphere.transform;
		}

		line.gameObject.SetActive(true);
		reticle.gameObject.SetActive(true);
	}
}

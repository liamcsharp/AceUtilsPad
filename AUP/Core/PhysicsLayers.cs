using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AUP;

internal static class PhysicsLayers
{
	private static Camera cachedCamera;
	private static float cameraStamp = -10f;

	public static Camera ViewCamera()
	{
		if (cachedCamera != null && cachedCamera.isActiveAndEnabled && Time.unscaledTime - cameraStamp < 1f)
		{
			return cachedCamera;
		}

		cameraStamp = Time.unscaledTime;
		cachedCamera = FindViewCamera();
		return cachedCamera;
	}

	private static Camera FindViewCamera()
	{
		Camera main = Camera.main;
		if (main != null && main.isActiveAndEnabled)
		{
			return main;
		}

		foreach (Camera candidate in Camera.allCameras)
		{
			if (candidate != null && candidate.isActiveAndEnabled && candidate.targetTexture == null)
			{
				return candidate;
			}
		}

		return null;
	}

	public static Transform Head()
	{
		Camera camera = ViewCamera();
		if (camera != null)
		{
			return camera.transform;
		}

		GorillaLocomotion.GTPlayer player = GorillaLocomotion.GTPlayer.Instance;
		return player != null ? player.transform : null;
	}

	public static Vector3 FlatForward(Transform head)
	{
		Vector3 forward = head.forward;
		forward.y = 0f;
		return forward.sqrMagnitude < 0.001f ? Vector3.forward : forward.normalized;
	}

	public static Vector2 PointerPosition()
	{
		UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
		if (mouse == null || Cursor.lockState == CursorLockMode.Locked)
		{
			return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
		}

		return mouse.position.ReadValue();
	}

	public static int PickLayer(Vector3 position)
	{
		int ground = 0;
		if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 300f, ~0, QueryTriggerInteraction.Ignore))
		{
			ground = hit.collider.gameObject.layer;
		}

		Camera camera = ViewCamera();
		int visible = camera != null ? camera.cullingMask : ~0;
		int locomotion = LocomotionMask();

		List<int> candidates = new List<int> { 2, 0, ground };
		for (int layer = 1; layer < 32; layer++)
		{
			if (layer != 4 && layer != 5 && !candidates.Contains(layer))
			{
				candidates.Add(layer);
			}
		}

		for (int pass = 0; pass < 2; pass++)
		{
			foreach (int layer in candidates)
			{
				bool isVisible = ((visible >> layer) & 1) != 0;
				bool collides = !Physics.GetIgnoreLayerCollision(layer, ground);
				bool free = pass == 1 || ((locomotion >> layer) & 1) == 0;
				if (isVisible && collides && free)
				{
					return layer;
				}
			}
		}

		return 0;
	}

	public static void SetLayer(GameObject root, int layer)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
		{
			child.gameObject.layer = layer;
		}
	}

	public static void IgnorePlayer(GameObject item)
	{
		Collider[] mine = item.GetComponentsInChildren<Collider>();
		IgnoreAll(mine, GorillaLocomotion.GTPlayer.Instance != null ? GorillaLocomotion.GTPlayer.Instance.transform : null);
		VRRig local = VRRig.LocalRig;
		IgnoreAll(mine, local != null ? local.transform : null);
	}

	private static void IgnoreAll(Collider[] mine, Transform root)
	{
		if (root == null)
		{
			return;
		}

		foreach (Collider other in root.GetComponentsInChildren<Collider>())
		{
			foreach (Collider collider in mine)
			{
				if (collider != null && other != null)
				{
					Physics.IgnoreCollision(collider, other, true);
				}
			}
		}
	}

	private static int LocomotionMask()
	{
		try
		{
			GorillaLocomotion.GTPlayer player = GorillaLocomotion.GTPlayer.Instance;
			if (player == null)
			{
				return 0;
			}

			FieldInfo field = typeof(GorillaLocomotion.GTPlayer).GetField("locomotionEnabledLayers",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			object value = field != null ? field.GetValue(player) : null;
			if (value is LayerMask mask)
			{
				return mask.value;
			}

			if (value is int number)
			{
				return number;
			}
		}
		catch (Exception)
		{
		}

		return 0;
	}
}

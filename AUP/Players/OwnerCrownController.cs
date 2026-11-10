using System;
using System.Collections.Generic;
using UnityEngine;

namespace AUP;

public static class OwnerConfiguration
{
	public static readonly string[] UserIds = { "D17270A44BA546C7" };

	public static bool IsOwner(string userId)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			return false;
		}

		foreach (string ownerId in UserIds)
		{
			if (string.Equals(userId, ownerId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}

public sealed class OwnerCrownController : MonoBehaviour
{
	private const float ScanInterval = 1f;
	private const float CrownHeight = 0.62f;

	private static readonly Color CrownColor = new Color(1f, 0.76f, 0.1f, 1f);

	private readonly Dictionary<VRRig, GameObject> crowns = new Dictionary<VRRig, GameObject>();
	private readonly List<VRRig> staleRigs = new List<VRRig>();
	private readonly HashSet<VRRig> ownerRigs = new HashSet<VRRig>();

	private Material crownMaterial;
	private float nextScanTime;

	private void Update()
	{
		if (Time.unscaledTime < nextScanTime)
		{
			return;
		}

		nextScanTime = Time.unscaledTime + ScanInterval;
		RefreshCrowns();
	}

	private void OnDestroy()
	{
		foreach (GameObject crown in crowns.Values)
		{
			if (crown != null)
			{
				Destroy(crown);
			}
		}

		crowns.Clear();
		if (crownMaterial != null)
		{
			Destroy(crownMaterial);
		}
	}

	private void RefreshCrowns()
	{
		ownerRigs.Clear();
		foreach (VRRig rig in UnityEngine.Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None))
		{
			if (rig == null || !OwnerConfiguration.IsOwner(PlayerInfo.UserId(rig)))
			{
				continue;
			}

			ownerRigs.Add(rig);
			if (!crowns.ContainsKey(rig))
			{
				crowns[rig] = CreateCrown(rig.transform);
			}
		}

		staleRigs.Clear();
		foreach (KeyValuePair<VRRig, GameObject> crown in crowns)
		{
			if (crown.Key == null || !ownerRigs.Contains(crown.Key))
			{
				if (crown.Value != null)
				{
					Destroy(crown.Value);
				}

				staleRigs.Add(crown.Key);
			}
		}

		foreach (VRRig rig in staleRigs)
		{
			crowns.Remove(rig);
		}
	}

	private GameObject CreateCrown(Transform parent)
	{
		GameObject crown = new GameObject("AUP Owner Crown");
		crown.transform.SetParent(parent, false);
		crown.transform.localPosition = Vector3.up * CrownHeight;
		AddPiece(crown.transform, Vector3.zero, new Vector3(0.2f, 0.035f, 0.2f));
		AddPiece(crown.transform, new Vector3(-0.11f, 0.08f, 0f), new Vector3(0.035f, 0.12f, 0.035f));
		AddPiece(crown.transform, new Vector3(0f, 0.11f, 0f), new Vector3(0.04f, 0.16f, 0.04f));
		AddPiece(crown.transform, new Vector3(0.11f, 0.08f, 0f), new Vector3(0.035f, 0.12f, 0.035f));
		return crown;
	}

	private void AddPiece(Transform parent, Vector3 localPosition, Vector3 localScale)
	{
		GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
		piece.transform.SetParent(parent, false);
		piece.transform.localPosition = localPosition;
		piece.transform.localScale = localScale;
		Destroy(piece.GetComponent<Collider>());
		piece.GetComponent<Renderer>().sharedMaterial = CrownMaterial();
	}

	private Material CrownMaterial()
	{
		if (crownMaterial == null)
		{
			crownMaterial = Materials.CreateUnlit(CrownColor);
		}

		return crownMaterial;
	}
}

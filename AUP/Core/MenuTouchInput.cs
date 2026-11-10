using System.Collections.Generic;
using AUP.UI;
using UnityEngine;

namespace AUP;

internal static class FingerTip
{
	private const float BoneExtension = 0.8f;
	private const float MinBoneLength = 0.001f;
	private const float MaxBoneLength = 0.2f;

	public static bool TryGet(out Vector3 position)
	{
		position = Vector3.zero;
		VRRig rig = FindLocalRig();
		if (rig != null && rig.rightIndex != null && rig.rightIndex.fingerBone3 != null)
		{
			Transform tip = rig.rightIndex.fingerBone3;
			Transform knuckle = rig.rightIndex.fingerBone2;
			position = tip.position;
			if (knuckle != null)
			{
				Vector3 direction = tip.position - knuckle.position;
				float length = direction.magnitude;
				if (length > MinBoneLength && length < MaxBoneLength)
				{
					position += direction / length * (length * BoneExtension);
				}
			}

			return Vectors.IsFinite(position);
		}

		Transform hand = Hands.Local(HandSide.Right);
		if (hand == null)
		{
			return false;
		}

		position = hand.position;
		return Vectors.IsFinite(position);
	}

	private static VRRig FindLocalRig()
	{
		VRRig rig = VRRig.LocalRig;
		if (rig == null && GorillaTagger.Instance != null)
		{
			rig = GorillaTagger.Instance.offlineVRRig;
		}

		return rig;
	}
}

internal sealed class MenuTouchInput
{
	private const float CooldownSeconds = 0.18f;

	private readonly MenuButtonTouchState state = new MenuButtonTouchState();
	private float nextPressTime;

	public void Reset()
	{
		state.Reset();
	}

	public void Update(Transform panel, List<UiButton> targets)
	{
		if (panel == null || !FingerTip.TryGet(out Vector3 tip))
		{
			state.Reset();
			return;
		}

		state.UpdateDistance(Vector3.Dot(tip - panel.position, -panel.forward));
		if (Time.unscaledTime < nextPressTime)
		{
			return;
		}

		foreach (UiButton button in targets)
		{
			if (TryPress(tip, button))
			{
				nextPressTime = Time.unscaledTime + CooldownSeconds;
				button.PressAt(tip);
				return;
			}
		}
	}

	private bool TryPress(Vector3 tip, UiButton button)
	{
		RectTransform rect = button.Rect;
		Rect bounds = rect.rect;
		if (bounds.width <= 0f || bounds.height <= 0f)
		{
			return false;
		}

		Vector3 local = rect.InverseTransformPoint(tip);
		float front = Vector3.Dot(tip - rect.position, -rect.forward);
		float x = (local.x - bounds.xMin) / bounds.width;
		float y = (local.y - bounds.yMin) / bounds.height;
		return state.TryPress(x, y, front);
	}
}

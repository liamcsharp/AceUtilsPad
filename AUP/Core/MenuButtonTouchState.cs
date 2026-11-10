namespace AUP;

internal sealed class MenuButtonTouchState
{
	private const float EdgeInset = 0.05f;
	private const float FrontContactDistance = 0.015f;
	private const float PushThroughDistance = 0.035f;
	private const float ReleaseDistance = 0.04f;
	private const float CancelDistance = 0.08f;

	private bool armed;

	public void Reset()
	{
		armed = false;
	}

	public void UpdateDistance(float frontDistance)
	{
		if (float.IsNaN(frontDistance) || float.IsInfinity(frontDistance) || frontDistance < -CancelDistance)
		{
			Reset();
		}
		else if (frontDistance >= ReleaseDistance)
		{
			armed = true;
		}
	}

	public bool TryPress(float normalizedX, float normalizedY, float frontDistance)
	{
		if (!armed || !Contains(normalizedX, normalizedY, frontDistance))
		{
			return false;
		}

		armed = false;
		return true;
	}

	public static bool Contains(float normalizedX, float normalizedY, float frontDistance)
	{
		return normalizedX >= EdgeInset && normalizedX < 1f - EdgeInset
										&& normalizedY >= EdgeInset && normalizedY < 1f - EdgeInset
										&& frontDistance >= -PushThroughDistance && frontDistance <= FrontContactDistance;
	}
}

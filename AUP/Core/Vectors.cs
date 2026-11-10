using UnityEngine;

namespace AUP;

public static class Vectors
{
	public static bool IsFinite(Vector3 value)
	{
		return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z)
			   && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
	}
}

using System;

namespace AUP;

public sealed class PlayerSelection
{
	public VRRig Selected { get; private set; }
	public bool Locked { get; private set; }

	public event Action Changed;

	public bool HasSelection => Selected != null;

	public void Select(VRRig rig)
	{
		if (Selected == rig)
		{
			return;
		}

		Selected = rig;
		Changed?.Invoke();
	}

	public void SetLocked(bool locked)
	{
		if (Locked == locked)
		{
			return;
		}

		Locked = locked;
		Changed?.Invoke();
	}

	public void Clear()
	{
		Locked = false;
		Select(null);
	}

	public void Validate()
	{
		bool destroyed = !ReferenceEquals(Selected, null) && Selected == null;
		if (destroyed)
		{
			Clear();
		}
	}
}

using System.Reflection;
using GorillaNetworking;
using HarmonyLib;

namespace AUP;

public sealed class CosmeticsService
{
	private static readonly FieldInfo MaxOutfitsField = AccessTools.Field(typeof(CosmeticsController), "maxOutfits");

	public string Status { get; private set; } = "Pick one of your saved outfits.";

	public bool Available => CosmeticsController.instance != null;

	public bool Ready => Available && CosmeticsController.CanScrollOutfits();

	public int OutfitCount
	{
		get
		{
			object value = MaxOutfitsField == null ? null : MaxOutfitsField.GetValue(null);
			return value is int count && count > 0 ? count : 0;
		}
	}

	public int SelectedOutfit => CosmeticsController.SelectedOutfit;

	public string Summary
	{
		get
		{
			if (!Available)
			{
				return "Cosmetics are not loaded yet.";
			}

			if (OutfitCount == 0 || !Ready)
			{
				return "Loading your saved outfits...";
			}

			return "Outfit <b>" + (SelectedOutfit + 1) + "</b> of " + OutfitCount;
		}
	}

	public void Select(int index)
	{
		if (!Ready)
		{
			Status = "Outfits are still loading or saving.";
			return;
		}

		if (index == SelectedOutfit)
		{
			Status = "Outfit " + (index + 1) + " is already worn.";
			return;
		}

		CosmeticsController.instance.LoadSavedOutfit(index);
		Status = "Switched to outfit " + (index + 1) + ".";
	}

	public void Step(int direction)
	{
		if (!Ready)
		{
			Status = "Outfits are still loading or saving.";
			return;
		}

		CosmeticsController.instance.PressWardrobeScrollOutfit(direction > 0);
		Status = "Switched to outfit " + (SelectedOutfit + 1) + ".";
	}
}

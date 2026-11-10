using System.Collections.Generic;
using AUP.UI;
using TMPro;

namespace AUP.Pages;

public sealed class CosmeticsPage : MenuPage
{
	private const int SlotColumns = 4;
	private const int SlotRows = 3;

	public CosmeticsPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Cosmetics";

	public override string IconId => "shirt";

	private CosmeticsService Cosmetics => Context.Cosmetics;

	public override void Build(PageBuilder ui)
	{
		ui.Label(() => Cosmetics.Summary, 1, 12f);
		ui.Label(() => Cosmetics.Status, 1, 8.5f, FontStyles.Normal, Pick.Muted);
		ui.Space(4f);
		ui.Buttons(
			new ButtonSpec("< Prev Outfit", () => Cosmetics.Step(-1), null, HasOutfits),
			new ButtonSpec("Next Outfit >", () => Cosmetics.Step(1), null, HasOutfits));
		ui.Space(4f);
		ui.Heading("Saved outfits");
		ui.Grid(SlotColumns, BuildSlots());
		ui.Label("These are the outfits saved at the in-game wardrobe.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private bool HasOutfits()
	{
		return Cosmetics.Available && Cosmetics.OutfitCount > 0;
	}

	private List<ButtonSpec> BuildSlots()
	{
		List<ButtonSpec> slots = new List<ButtonSpec>();
		for (int i = 0; i < SlotColumns * SlotRows; i++)
		{
			int slot = i;
			slots.Add(new ButtonSpec(
				(slot + 1).ToString(),
				() => Cosmetics.Select(slot),
				() => Cosmetics.Available && Cosmetics.SelectedOutfit == slot,
				() => slot < Cosmetics.OutfitCount));
		}

		return slots;
	}
}

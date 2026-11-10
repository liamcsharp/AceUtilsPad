using System.Collections.Generic;
using AUP.UI;
using AUP.Widgets;
using TMPro;
using UnityEngine;

namespace AUP.Pages;

public sealed class WidgetHubPage : MenuPage
{
	public WidgetHubPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Widget Hub";

	public override string IconId => "star";

	private WidgetManager Manager => Context.Widgets;

	private WidgetProfile Profile => Manager.Profile;

	public override void Build(PageBuilder ui)
	{
		BuildLayouts(ui);
		BuildProfiles(ui);
		BuildStudio(ui);
		BuildSettings(ui);
	}

	private void BuildLayouts(PageBuilder ui)
	{
		ui.Section("Layouts");
		List<ButtonSpec> layouts = new List<ButtonSpec>();
		for (int i = 0; i < WidgetOptions.LayoutNames.Length; i++)
		{
			int index = i;
			layouts.Add(new ButtonSpec(() => index < Profile.Layouts.Count ? Rich.Escape(Profile.Layouts[index].Name) : string.Empty,
				() => Manager.SetLayout(index), () => Manager.ActiveLayoutIndex == index));
		}

		ui.Grid(2, layouts);
		ui.Button("Reset This Layout", Manager.ResetLayout);
		for (int style = 0; style < WidgetOptions.StyleNames.Length; style++)
		{
			int captured = style;
			ui.Stepper(() => WidgetOptions.StyleNames[captured] + ": " + Rich.Escape(Manager.StyleLayoutName(captured)),
				() => Manager.StepStyleLayout(captured, -1), () => Manager.StepStyleLayout(captured, 1));
		}

		ui.Label("Each layout keeps its own positions, sizes, colors and visibility. Shared means that menu style uses the layout picked above.", 3, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildProfiles(PageBuilder ui)
	{
		ui.Section("Profiles");
		ui.Stepper(() => Manager.SelectedProfileName != null ? Rich.Escape(Manager.SelectedProfileName) : "No saved profiles",
			() => Manager.StepProfile(-1), () => Manager.StepProfile(1));
		ui.Buttons(
			new ButtonSpec("Load", Manager.LoadSelectedProfile, null, HasProfile),
			new ButtonSpec("Overwrite", () => Manager.Store.SaveProfile(Manager.SelectedProfileName), null, HasProfile),
			new ButtonSpec("Delete", () => Manager.Store.DeleteProfile(Manager.SelectedProfileName), null, HasProfile));
		ui.Button("Save As New Profile", SaveNew);
		ui.Buttons(
			new ButtonSpec("Copy Share Code", () => Manager.Store.ExportCode()),
			new ButtonSpec("Paste Share Code", Manager.ImportCode));
		ui.Label(() => Manager.Store.LastMessage, 2, 8.5f, FontStyles.Bold, Pick.Accent);
		ui.Label("A share code holds every layout and widget you made. Send it to a friend and they paste it here. "
				 + "There is no online marketplace: AceWidgets has no server, so sharing works through codes.", 5, 8f, FontStyles.Normal, Pick.Muted);
	}

	private bool HasProfile()
	{
		return Manager.SelectedProfileName != null;
	}

	private void SaveNew()
	{
		Context.Text.Begin("Profile name", "My Setup", Manager.Store.SaveProfile);
		Context.ShowPage<KeyboardPage>();
	}

	private void BuildStudio(PageBuilder ui)
	{
		ui.Section("Studio");
		ui.Stepper(() => Manager.StudioWidget != null ? Rich.Escape(WidgetManager.Name(Manager.StudioWidget)) : "No studio widgets yet",
			() => Manager.StepStudio(-1), () => Manager.StepStudio(1));
		ui.Buttons(
			new ButtonSpec("New Widget", Manager.NewStudioWidget),
			new ButtonSpec("Rename", RenameStudio, null, HasStudio),
			new ButtonSpec("Delete", Manager.RemoveStudioWidget, null, HasStudio));
		ui.Stepper(ElementTitle, () => Manager.StepElement(-1), () => Manager.StepElement(1));
		ui.Stepper(() => Manager.StudioElement != null ? "Type: " + WidgetOptions.ElementTypes[Manager.StudioElement.Type] : "Type",
			() => Manager.StepElementType(-1), () => Manager.StepElementType(1));
		ui.Stepper(() => Manager.StudioElement != null ? "Uses: " + WidgetData.BindName(Manager.StudioElement) : "Uses",
			() => Manager.StepElementBind(-1), () => Manager.StepElementBind(1));
		ui.Buttons(
			new ButtonSpec("Add", Manager.AddElement, null, HasStudio),
			new ButtonSpec("Remove", Manager.RemoveElement, null, HasElement),
			new ButtonSpec("Move Up", Manager.MoveElementUp, null, HasElement));
		ui.Button("Type Element Text", TypeElementText, null, HasElement);
		ui.Label("Build a widget from labels, info, icons, buttons, sliders, toggles, bars, panels and separators. "
				 + "Buttons, sliders and toggles can only use the safe actions in the list.", 3, 8f, FontStyles.Normal, Pick.Muted);
	}

	private bool HasStudio()
	{
		return Manager.StudioWidget != null;
	}

	private bool HasElement()
	{
		return Manager.StudioElement != null;
	}

	private string ElementTitle()
	{
		WidgetConfig widget = Manager.StudioWidget;
		WidgetElement element = Manager.StudioElement;
		if (widget == null)
		{
			return "Make a widget first";
		}

		if (element == null)
		{
			return "No elements";
		}

		string text = element.Text.Length > 0 ? "  \"" + Rich.Escape(element.Text) + "\"" : string.Empty;
		return "Element " + (Manager.ElementIndex + 1) + "/" + widget.Elements.Count + text;
	}

	private void RenameStudio()
	{
		WidgetConfig target = Manager.StudioWidget;
		if (target == null)
		{
			return;
		}

		Context.Text.Begin("Widget name", WidgetManager.Name(target), value =>
		{
			target.Title = PlayerTags.Clean(value);
			Manager.Restyle(target);
		});
		Context.ShowPage<KeyboardPage>();
	}

	private void TypeElementText()
	{
		WidgetConfig widget = Manager.StudioWidget;
		WidgetElement element = Manager.StudioElement;
		if (widget == null || element == null)
		{
			return;
		}

		Context.Text.Begin("Element text", element.Text, value =>
		{
			element.Text = PlayerTags.Clean(value);
			Manager.Restyle(widget);
		});
		Context.ShowPage<KeyboardPage>();
	}

	private void BuildSettings(PageBuilder ui)
	{
		ui.Section("Settings");
		ui.Stepper(() => "All widgets size " + Mathf.RoundToInt(Profile.GlobalScale * 100f) + "%",
			() => Set(() => Profile.GlobalScale = Mathf.Clamp(Mathf.Round((Profile.GlobalScale - 0.1f) * 10f) / 10f, 0.5f, 2f)),
			() => Set(() => Profile.GlobalScale = Mathf.Clamp(Mathf.Round((Profile.GlobalScale + 0.1f) * 10f) / 10f, 0.5f, 2f)));
		ui.Stepper(() => "All transparency " + Mathf.RoundToInt((1f - Profile.GlobalAlpha) * 100f) + "%",
			() => Set(() => Profile.GlobalAlpha = Mathf.Clamp(Mathf.Round((Profile.GlobalAlpha + 0.1f) * 10f) / 10f, 0.2f, 1f)),
			() => Set(() => Profile.GlobalAlpha = Mathf.Clamp(Mathf.Round((Profile.GlobalAlpha - 0.1f) * 10f) / 10f, 0.2f, 1f)));
		ui.Stepper(() => "Animation speed " + Profile.AnimationSpeed.ToString("0.##") + "x",
			() => Set(() => Profile.AnimationSpeed = Mathf.Clamp(Profile.AnimationSpeed - 0.25f, 0.25f, 3f)),
			() => Set(() => Profile.AnimationSpeed = Mathf.Clamp(Profile.AnimationSpeed + 0.25f, 0.25f, 3f)));
		ui.Stepper(() => "Updates: " + WidgetOptions.RateNames[Profile.UpdateRate],
			() => Set(() => Profile.UpdateRate = WidgetOptions.Wrap(Profile.UpdateRate - 1, WidgetOptions.RateNames.Length)),
			() => Set(() => Profile.UpdateRate = WidgetOptions.Wrap(Profile.UpdateRate + 1, WidgetOptions.RateNames.Length)));
		ui.Buttons(
			new ButtonSpec(() => "Hide With Menu  " + (Profile.HideWithMenu ? "ON" : "OFF"), () => Set(() => Profile.HideWithMenu = !Profile.HideWithMenu), () => Profile.HideWithMenu),
			new ButtonSpec(() => "Lobby Only  " + (Profile.LobbyOnly ? "ON" : "OFF"), () => Set(() => Profile.LobbyOnly = !Profile.LobbyOnly), () => Profile.LobbyOnly));
		ui.Buttons(
			new ButtonSpec(() => "Tap sound: " + NotifySound.Name(Profile.ClickSound), () => Set(() =>
			{
				Profile.ClickSound = WidgetOptions.Wrap(Profile.ClickSound + 1, NotifySound.Names.Length);
				NotifySound.Play(Profile.ClickSound, Context.Settings.SoundVolume.Value);
			})),
			new ButtonSpec(() => "Show: " + WidgetOptions.DisplayNames[WidgetOptions.Wrap(Profile.DisplayMode, WidgetOptions.DisplayNames.Length)],
				() => Set(() => Profile.DisplayMode = WidgetOptions.Wrap(Profile.DisplayMode + 1, WidgetOptions.DisplayNames.Length))));
		ui.Buttons(
			new ButtonSpec(() => "New spot: " + WidgetOptions.SpotNames[WidgetOptions.Wrap(Profile.DefaultSpot, WidgetOptions.SpotNames.Length)],
				() => Set(() => Profile.DefaultSpot = WidgetOptions.Wrap(Profile.DefaultSpot + 1, WidgetOptions.SpotNames.Length))),
			new ButtonSpec("Reset All Widgets", Manager.ResetAll));
		ui.Label("Lobby Only shows widgets when you join a lobby and hides them when you leave.", 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void Set(System.Action change)
	{
		change();
		Manager.Touch(false);
	}
}

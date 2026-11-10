using System.Collections.Generic;
using System.Text;
using AUP.UI;
using TMPro;

namespace AUP.Pages;

public sealed class ModsPage : MenuPage
{
	private const int ListLines = 7;

	private readonly StringBuilder builder = new StringBuilder();

	public ModsPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Mod Detection";

	public override string IconId => "bolt";

	private VRRig Selected => Context.Selection.Selected;

	public override void Build(PageBuilder ui)
	{
		BuildScanSection(ui);
		BuildRiskSection(ui);
	}

	private void BuildScanSection(PageBuilder ui)
	{
		ui.Section("Scan");
		ui.Label(PlayerName, 1, 12f, FontStyles.Bold);
		ui.Label(() => "<b>Color code:</b> " + PlayerInfo.ColorMarkup(Selected), 1, 9.5f);
		ui.Label(Verdict, 1, 10f, FontStyles.Bold);
		ui.Label(DetectionList, ListLines, 9f);
		ui.Space(2f);
		ui.Buttons(
			new ButtonSpec("Refresh Lists", RefreshLists),
			new ButtonSpec("Re-alert", Context.DetectionAlerts.Forget));
		ui.Label(() => DetectionService.Summary, 2, 8f, FontStyles.Normal, Pick.Muted);
	}

	private void BuildRiskSection(PageBuilder ui)
	{
		ui.Section("Ban risk");
		ui.Heading("What the labels mean");
		ui.Label(RiskLine(BanRisk.High), 3, 8.5f);
		ui.Space(2f);
		ui.Label(RiskLine(BanRisk.PrivateOnly), 3, 8.5f);
		ui.Space(2f);
		ui.Label(RiskLine(BanRisk.Low), 3, 8.5f);
		ui.Space(4f);
		ui.Label("Labels are a best guess from the cheat list and the mod name. Only the game's own rules decide bans, "
				 + "so when in doubt keep mods to private lobbies.", 4, 8f, FontStyles.Normal, Pick.Muted);
	}

	private static string RiskLine(BanRisk risk)
	{
		return "<color=" + BanRiskInfo.Color(risk) + "><b>" + BanRiskInfo.Short(risk) + "</b></color>  " + BanRiskInfo.Explain(risk);
	}

	private string PlayerName()
	{
		return Selected == null ? "No player selected" : Rich.Escape(PlayerInfo.DisplayName(Selected));
	}

	private string Verdict()
	{
		if (Selected == null)
		{
			return "<color=#9AA0B8>Aim at a player and pull the trigger, or middle-click them on PC</color>";
		}

		List<DetectionResult> detections = DetectionService.Detect(Selected);
		if (detections.Count == 0)
		{
			return "<color=" + BanRiskInfo.LowColor + ">Clean scan</color>";
		}

		BanRisk worst = BanRiskInfo.Worst(detections);
		return "<color=" + BanRiskInfo.Color(worst) + ">" + detections.Count + " found - " + BanRiskInfo.Label(worst) + "</color>";
	}

	private string DetectionList()
	{
		if (Selected == null)
		{
			return "-";
		}

		List<DetectionResult> detections = DetectionService.Detect(Selected);
		if (detections.Count == 0)
		{
			return "No known mod markers detected";
		}

		builder.Length = 0;
		foreach (DetectionResult detection in detections)
		{
			builder.Append("<color=").Append(BanRiskInfo.Color(detection.Risk)).Append("><b>[").Append(BanRiskInfo.Short(detection.Risk))
				.Append("]</b></color> <color=").Append(DetectionService.ColorFor(detection.Kind)).Append('>')
				.Append(detection.Kind).Append(": ").Append(Rich.Escape(detection.Name)).Append("</color>\n");
		}

		return builder.ToString().TrimEnd('\n');
	}

	private void RefreshLists()
	{
		DetectionService.RefreshLists();
		Context.DetectionAlerts.Forget();
	}
}

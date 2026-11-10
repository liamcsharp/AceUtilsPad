using AUP.UI;
using TMPro;

namespace AUP.Pages;

public sealed class CreditsPage : MenuPage
{
	public CreditsPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Credits";

	public override string IconId => "heart";

	public override void Build(PageBuilder ui)
	{
		ui.Space(24f);
		ui.Label(PluginInfo.Name, 1, 16f, FontStyles.Bold, Pick.Accent);
		ui.Label("version " + PluginInfo.Version, 1, 9f, FontStyles.Normal, Pick.Muted);
		ui.Space(16f);
		ui.Label(PluginInfo.Credits, 3, 10f);
	}
}

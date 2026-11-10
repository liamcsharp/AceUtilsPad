using AUP.UI;
using TMPro;

namespace AUP.Pages;

public sealed class MediaPage : MenuPage
{
	public MediaPage(MenuContext context) : base(context)
	{
	}

	public override string Title => "Media Player";

	public override string IconId => "play";

	private AudioHandler Audio => Context.Audio;

	public override void Build(PageBuilder ui)
	{
		ui.Section("Now playing");
		ui.Label(SourceLine, 1, 8.5f, FontStyles.Bold, Pick.Accent);
		ui.Label(() => Rich.Escape(Audio.CurrentTitle), 2, 13f, FontStyles.Bold);
		ui.Label(() => Rich.Escape(Audio.CurrentArtist), 1, 9.5f, FontStyles.Normal, Pick.Muted);
		ui.Space(6f);
		ui.Progress(() => Audio.Progress, null, 7f);
		ui.Pair(() => Audio.PositionText, () => Audio.DurationText, 9f);
		ui.Space(2f);
		ui.Buttons(
			new ButtonSpec("Back", Audio.PreviousTrack),
			new ButtonSpec(() => Audio.IsPlaying ? "Pause" : "Play", Audio.TogglePlayPause, () => Audio.IsPlaying),
			new ButtonSpec("Next", Audio.NextTrack));
		ui.Buttons(
			new ButtonSpec("-10s", () => Audio.SeekBy(-10f), null, () => Audio.HasTimeline),
			new ButtonSpec("+10s", () => Audio.SeekBy(10f), null, () => Audio.HasTimeline));
		ui.Buttons(
			new ButtonSpec("Volume -", Audio.VolumeDown),
			new ButtonSpec("Volume +", Audio.VolumeUp));
		ui.Label(() => Audio.Status, 2, 8f, FontStyles.Normal, Pick.Muted);

		ui.Section("How it works");
		ui.Heading("Works with any app");
		ui.Label("The player reads the song from Windows itself, the same info shown in the volume popup. "
				 + "Spotify, YouTube in Chrome, Edge or Firefox, Apple Music, SoundCloud and most other players show up on their own.", 5, 8.5f, FontStyles.Normal, Pick.Muted);
		ui.Space(4f);
		ui.Heading("Nothing extra to install");
		ui.Label("No QuickSong.exe or helper app is needed. The time keeps counting smoothly between updates, "
				 + "and seeking works in players that allow it.", 4, 8.5f, FontStyles.Normal, Pick.Muted);
		ui.Space(4f);
		ui.Label(() => "<b>Source:</b> " + (Audio.SourceName.Length > 0 ? Rich.Escape(Audio.SourceName) : "none yet")
					   + "\n<b>Timeline:</b> " + Audio.TimelineText, 2, 9f);
	}

	private string SourceLine()
	{
		if (Audio.Connecting)
		{
			return "Looking for music...";
		}

		if (!Audio.HasMedia)
		{
			return "Nothing playing";
		}

		string state = Audio.IsPlaying ? "Playing" : "Paused";
		return Audio.SourceName.Length > 0 ? state + " on " + Rich.Escape(Audio.SourceName) : state;
	}
}

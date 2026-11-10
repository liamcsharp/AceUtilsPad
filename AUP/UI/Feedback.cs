namespace AUP.UI;

public static class Feedback
{
	public static void Click()
	{
		ClickSound.Play();

		GorillaTagger tagger = GorillaTagger.Instance;
		if (tagger != null)
		{
			tagger.StartVibration(false, tagger.tagHapticStrength / 2f, tagger.tagHapticDuration / 2f);
		}
	}
}

namespace AUP;

internal static class SimpleInputs
{
	private static ControllerInputPoller Poller => ControllerInputPoller.instance;

	public static bool Available => Poller != null;
	public static bool RightTrigger => Available && Poller.rightControllerIndexFloat > 0.5f;
	public static bool LeftGrab => Available && Poller.leftGrab;
	public static bool RightGrab => Available && Poller.rightGrab;
	public static bool LeftY => Available && Poller.leftControllerSecondaryButton;
}

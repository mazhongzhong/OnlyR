namespace OnlyR.Services.Tray;

internal enum WindowCloseAction
{
    CloseApplication,
    HideToTray,
}

internal static class WindowClosePolicy
{
    public static WindowCloseAction GetAction(bool closeToTray, bool exitRequested) =>
        closeToTray && !exitRequested
            ? WindowCloseAction.HideToTray
            : WindowCloseAction.CloseApplication;
}
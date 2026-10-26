using System.Runtime.InteropServices;

namespace ZGF.Desktop;

/// <summary>The right to bring a window to the front. Windows lets only the app the user is working
/// with take the focus; that app can lend the right to another, as one run of an app does when it
/// hands its work to a run already open. Elsewhere any app may take it, and lending does nothing.</summary>
public static class ForegroundRights
{
    /// <summary>Lets the process <paramref name="processId"/> bring its window to the front, when this
    /// process may.</summary>
    public static void LendTo(int processId)
    {
        if (!OperatingSystem.IsWindows())
            return;
        AllowSetForegroundWindow(processId);
    }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);
}

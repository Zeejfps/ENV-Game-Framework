using System.Runtime.CompilerServices;

namespace ZGF.Gui;

/// <summary>
/// Channel-based debug log for the popup window subsystem. Enable channels via
/// PopupDebugLog.Enabled = Channel.X | Channel.Y. Output goes to stderr so it
/// doesn't interleave with whatever's on stdout.
/// </summary>
public static class PopupDebugLog
{
    [Flags]
    public enum Channel
    {
        None      = 0,
        Lifecycle = 1 << 0,   // Acquire/Release/Pool
        Render    = 1 << 1,   // RenderFrame calls, viewport/projection state
        Layout    = 1 << 2,   // Measure/Position
        Capture   = 1 << 3,   // Decorator BeginCapture/EndCapture/TransferCapture
        Context   = 1 << 4,   // GL context switches
        Outside   = 1 << 5,   // Outside-click resolution
        All       = Lifecycle | Render | Layout | Capture | Context | Outside,
    }

    // Default: everything EXCEPT the per-frame Render channel. The Render channel
    // logs once per IssueDraws (60Hz × N windows) — enable it on demand when
    // diagnosing render-time issues (projection/viewport/draw-call counts).
    public static Channel Enabled { get; set; } = Channel.All & ~Channel.Render;

    public static bool IsOn(Channel c) => (Enabled & c) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(Channel c, string message)
    {
        if ((Enabled & c) == 0) return;
        Console.Error.WriteLine($"[popup:{c}] {message}");
    }
}

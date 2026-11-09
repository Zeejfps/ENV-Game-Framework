using System.Runtime.InteropServices;

namespace ZGF.Gui.Automation.Tests;

/// <summary>
/// Records every window shown, brought to the foreground or focused anywhere on the desktop, through WinEvent hooks
/// installed before the app starts, so its startup is watched too, and by polling the watched process's windows
/// every millisecond. A hook delivers each event, a transient one included, so an activation that is undone at once
/// is still seen. Windows only: <see cref="Start"/> throws elsewhere, and throws if a hook cannot be installed.
/// </summary>
internal sealed class ProcessWindowWatch : IDisposable
{
    private const uint EventSystemForeground = 0x0003;
    private const uint EventObjectShow = 0x8002;
    private const uint EventObjectFocus = 0x8005;
    private const uint OutOfContext = 0x0000;

    private readonly List<(uint Process, string Event)> _events = new();
    private readonly Thread _thread;
    private volatile bool _stop;
    private volatile uint _process;
    private Exception? _hookFailure;
    private Win32.WinEventProc? _hook;

    private ProcessWindowWatch()
    {
        using var hooked = new ManualResetEventSlim();
        _thread = new Thread(() => Watch(hooked)) { IsBackground = true, Name = "Process window watch" };
        _thread.Start();
        hooked.Wait();
        if (_hookFailure != null)
        {
            _thread.Join();
            throw _hookFailure;
        }
    }

    public static ProcessWindowWatch Start()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The window watch reads Win32 events.");
        return new ProcessWindowWatch();
    }

    /// <summary>Starts polling <paramref name="processId"/>'s windows; its events are kept from the start either way.</summary>
    public void Follow(int processId) => _process = (uint)processId;

    /// <summary>How many top-level windows the followed process has, shown or not.</summary>
    public int WindowCount => Win32.ProcessWindows(_process).Count;

    /// <summary>Every show, foreground and focus event on a window of <paramref name="processId"/>, and every time polling
    /// found one of its windows visible or in the foreground.</summary>
    public IReadOnlyList<string> Violations(int processId)
    {
        Thread.Sleep(50);
        lock (_events)
            return _events.Where(e => e.Process == (uint)processId).Select(e => e.Event).Distinct().ToArray();
    }

    private void Watch(ManualResetEventSlim hooked)
    {
        _hook = OnEvent;
        var hooks = new[]
        {
            Win32.SetWinEventHook(EventSystemForeground, EventSystemForeground, 0, _hook, 0, 0, OutOfContext),
            Win32.SetWinEventHook(EventObjectShow, EventObjectShow, 0, _hook, 0, 0, OutOfContext),
            Win32.SetWinEventHook(EventObjectFocus, EventObjectFocus, 0, _hook, 0, 0, OutOfContext),
        };
        try
        {
            if (hooks.Any(h => h == 0))
            {
                _hookFailure = new InvalidOperationException($"SetWinEventHook failed (error {Marshal.GetLastPInvokeError()}); the watch would see nothing.");
                return;
            }
            hooked.Set();
            while (!_stop)
            {
                while (Win32.PeekMessage(out var message, 0, 0, 0, 1))
                {
                    Win32.TranslateMessage(ref message);
                    Win32.DispatchMessage(ref message);
                }
                Poll();
                Thread.Sleep(1);
            }
        }
        finally
        {
            foreach (var hook in hooks.Where(h => h != 0))
                Win32.UnhookWinEvent(hook);
            hooked.Set();
        }
    }

    private void OnEvent(nint hook, uint eventType, nint window, int objectId, int childId, uint thread, uint time)
    {
        if (window == 0)
            return;
        if (eventType == EventObjectShow && objectId != 0)
            return;
        Win32.GetWindowThreadProcessId(window, out var owner);
        Note(owner, $"event 0x{eventType:X} on window {window}");
    }

    private void Poll()
    {
        var process = _process;
        if (process == 0)
            return;
        foreach (var window in Win32.ProcessWindows(process))
        {
            if (Win32.IsWindowVisible(window))
                Note(process, $"window {window} visible");
        }
        var foreground = Win32.GetForegroundWindow();
        if (foreground == 0)
            return;
        Win32.GetWindowThreadProcessId(foreground, out var owner);
        if (owner == process)
            Note(process, "a window of the app is in the foreground");
    }

    private void Note(uint process, string violation)
    {
        lock (_events)
            _events.Add((process, violation));
    }

    public void Dispose()
    {
        _stop = true;
        _thread.Join();
    }

    private static class Win32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Msg
        {
            public nint Window;
            public uint Message;
            public nint WParam;
            public nint LParam;
            public uint Time;
            public int X;
            public int Y;
            public uint Private;
        }

        public delegate void WinEventProc(nint hook, uint eventType, nint window, int objectId, int childId, uint thread, uint time);

        private delegate bool EnumWindowsProc(nint window, nint parameter);

        public static List<nint> ProcessWindows(uint process)
        {
            var windows = new List<nint>();
            if (process == 0)
                return windows;
            EnumWindows((window, _) =>
            {
                GetWindowThreadProcessId(window, out var owner);
                if (owner == process)
                    windows.Add(window);
                return true;
            }, 0);
            return windows;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(nint window);

        [DllImport("user32.dll")]
        public static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(nint window, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint module, WinEventProc callback, uint processId, uint threadId, uint flags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(nint hook);

        [DllImport("user32.dll")]
        public static extern bool PeekMessage(out Msg message, nint window, uint filterMin, uint filterMax, uint remove);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref Msg message);

        [DllImport("user32.dll")]
        public static extern nint DispatchMessage(ref Msg message);
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.InputDemo;

/// <summary>Where a simulated keystroke goes.</summary>
internal interface ITypeSink
{
    void Type(Rune rune);
    void Press(KeyboardKey key, bool control = false);
    string Describe { get; }
}

/// <summary>
/// Types through the OS itself: <c>SendInput</c> with <c>KEYEVENTF_UNICODE</c> posts a real
/// <c>WM_CHAR</c> to the focused window, which is what GLFW's character callback listens to. Nothing
/// about the app is faked — the keystroke enters at the same place a physical keyboard's would, so
/// this exercises the whole chain: OS → GLFW → IWindow.OnText → InputSystem → the text field.
/// </summary>
internal sealed class OsTypeSink : ITypeSink
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;

    private const ushort VkBack = 0x08;
    private const ushort VkTab = 0x09;
    private const ushort VkReturn = 0x0D;
    private const ushort VkControl = 0x11;
    private const ushort VkA = 0x41;

    private IntPtr _window;

    public string Describe => "OS SendInput (real WM_CHAR -> GLFW char callback)";

    public static OsTypeSink? TryCreate() => OperatingSystem.IsWindows() ? new OsTypeSink() : null;

    /// <summary>The app's top-level window, resolved on first use. It can't be resolved up front: the
    /// GLFW window doesn't exist until the app runs, and Process caches a zero handle until refreshed.</summary>
    private IntPtr Window
    {
        get
        {
            if (_window != IntPtr.Zero) return _window;
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            _window = process.MainWindowHandle;
            return _window;
        }
    }

    public bool HasWindow => Window != IntPtr.Zero;

    /// <summary>SendInput goes to whatever window is focused, so make sure that's ours before typing —
    /// otherwise a stray alt-tab sends the script's keystrokes into someone else's editor.</summary>
    public bool IsTargetFocused() => HasWindow && GetForegroundWindow() == Window;

    public void Focus()
    {
        if (HasWindow) SetForegroundWindow(Window);
    }

    public void Type(Rune rune)
    {
        // A UTF-16 surrogate pair is two KEYEVENTF_UNICODE events; Windows recombines them.
        Span<char> utf16 = stackalloc char[2];
        var length = rune.EncodeToUtf16(utf16);
        for (var i = 0; i < length; i++)
            SendUnicode(utf16[i]);
    }

    public void Press(KeyboardKey key, bool control = false)
    {
        var vk = key switch
        {
            KeyboardKey.Backspace => VkBack,
            KeyboardKey.Tab => VkTab,
            KeyboardKey.Enter => VkReturn,
            KeyboardKey.A => VkA,
            _ => (ushort)0,
        };
        if (vk == 0) throw new NotSupportedException($"No virtual-key mapping for {key}.");

        if (control) SendVirtualKey(VkControl, up: false);
        SendVirtualKey(vk, up: false);
        SendVirtualKey(vk, up: true);
        if (control) SendVirtualKey(VkControl, up: true);
    }

    private static void SendUnicode(char unit)
    {
        Span<Input> events =
        [
            KeyboardEvent(0, unit, KeyEventUnicode),
            KeyboardEvent(0, unit, KeyEventUnicode | KeyEventKeyUp),
        ];
        Send(events);
    }

    private static void SendVirtualKey(ushort vk, bool up)
    {
        Span<Input> events = [KeyboardEvent(vk, '\0', up ? KeyEventKeyUp : 0)];
        Send(events);
    }

    private static Input KeyboardEvent(ushort vk, char scan, uint flags) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion
        {
            Keyboard = new KeyboardInput
            {
                VirtualKey = vk,
                Scan = (ushort)scan,
                Flags = flags,
                Time = 0,
                ExtraInfo = IntPtr.Zero,
            },
        },
    };

    private static void Send(Span<Input> events)
    {
        var sent = SendInput((uint)events.Length, ref MemoryMarshal.GetReference(events), Marshal.SizeOf<Input>());
        if (sent != events.Length)
            throw new InvalidOperationException($"SendInput sent {sent}/{events.Length} events (win32 error {Marshal.GetLastWin32Error()}).");
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, ref Input inputs, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint Data;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Msg;
        public ushort ParamL;
        public ushort ParamH;
    }
}

/// <summary>
/// Types by handing events straight to the <see cref="InputSystem"/>, skipping the OS. Used where
/// <see cref="OsTypeSink"/> can't run (macOS, Linux). Still drives the real window, the real
/// dispatch and the real controller — it just can't vouch for the GLFW callback above it.
/// </summary>
internal sealed class InjectedTypeSink : ITypeSink
{
    private readonly InputSystem _input;

    public InjectedTypeSink(InputSystem input) => _input = input;

    public string Describe => "injected into InputSystem (GLFW callback not exercised)";

    public void Type(Rune rune)
    {
        var e = new TextInputEvent { Rune = rune, Phase = EventPhase.Capturing };
        _input.SendTextInputEvent(ref e);
    }

    public void Press(KeyboardKey key, bool control = false)
    {
        var mods = control ? InputModifiers.Control : InputModifiers.None;
        Send(key, mods, InputState.Pressed);
        Send(key, mods, InputState.Released);
    }

    private void Send(KeyboardKey key, InputModifiers mods, InputState state)
    {
        var e = new KeyboardKeyEvent
        {
            Key = key,
            State = state,
            Modifiers = mods,
            Phase = EventPhase.Capturing,
        };
        _input.SendKeyboardKeyEvent(ref e);
    }
}

using System.Runtime.InteropServices;
using System.Text;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Automation;

/// <summary>
/// Types through the operating system itself, with <c>SendInput</c>. Characters go out as
/// <c>KEYEVENTF_UNICODE</c>, which posts a real <c>WM_CHAR</c> — the message GLFW's character
/// callback listens to — so a scripted keystroke enters the app at exactly the point a physical
/// keyboard's would, exercising the whole chain rather than starting halfway down it.
///
/// Windows only, and it lands on whatever window the OS has focused, so a caller must keep the
/// target window in the foreground (<see cref="IsForeground"/>) or the keystrokes go elsewhere.
/// Everywhere else, drive <see cref="GuiDriver"/> with injected events instead.
/// </summary>
public static class OsKeyboard
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;

    public static bool IsSupported => OperatingSystem.IsWindows();

    public static bool IsForeground(IntPtr window) =>
        IsSupported && window != IntPtr.Zero && GetForegroundWindow() == window;

    public static void Focus(IntPtr window)
    {
        if (IsSupported && window != IntPtr.Zero) SetForegroundWindow(window);
    }

    public static void TypeRune(Rune rune)
    {
        // A surrogate pair is two KEYEVENTF_UNICODE events; Windows recombines them into one char.
        Span<char> utf16 = stackalloc char[2];
        var length = rune.EncodeToUtf16(utf16);
        for (var i = 0; i < length; i++)
        {
            SendOne(KeyboardEvent(0, utf16[i], KeyEventUnicode));
            SendOne(KeyboardEvent(0, utf16[i], KeyEventUnicode | KeyEventKeyUp));
        }
    }

    public static void PressKey(KeyboardKey key, InputModifiers modifiers = InputModifiers.None)
    {
        var vk = VirtualKey(key);

        foreach (var mod in ModifierKeys(modifiers))
            SendOne(KeyboardEvent(mod, '\0', 0));

        SendOne(KeyboardEvent(vk, '\0', 0));
        SendOne(KeyboardEvent(vk, '\0', KeyEventKeyUp));

        foreach (var mod in ModifierKeys(modifiers))
            SendOne(KeyboardEvent(mod, '\0', KeyEventKeyUp));
    }

    private static IEnumerable<ushort> ModifierKeys(InputModifiers modifiers)
    {
        if (modifiers.HasFlag(InputModifiers.Control)) yield return 0x11; // VK_CONTROL
        if (modifiers.HasFlag(InputModifiers.Shift)) yield return 0x10;   // VK_SHIFT
        if (modifiers.HasFlag(InputModifiers.Alt)) yield return 0x12;     // VK_MENU
    }

    private static ushort VirtualKey(KeyboardKey key) => key switch
    {
        KeyboardKey.Backspace => 0x08,
        KeyboardKey.Tab => 0x09,
        KeyboardKey.Enter or KeyboardKey.NumpadEnter => 0x0D,
        KeyboardKey.Escape => 0x1B,
        KeyboardKey.Space => 0x20,
        KeyboardKey.End => 0x23,
        KeyboardKey.Home => 0x24,
        KeyboardKey.LeftArrow => 0x25,
        KeyboardKey.UpArrow => 0x26,
        KeyboardKey.RightArrow => 0x27,
        KeyboardKey.DownArrow => 0x28,
        KeyboardKey.Delete => 0x2E,
        >= KeyboardKey.A and <= KeyboardKey.Z => (ushort)(0x41 + (key - KeyboardKey.A)),
        _ => throw new NotSupportedException(
            $"{key} has no virtual-key mapping for OS input. Add one, or drive the driver with injected events."),
    };

    private static void SendOne(Input input)
    {
        var sent = SendInput(1, ref input, Marshal.SizeOf<Input>());
        if (sent != 1)
            throw new InvalidOperationException(
                $"SendInput was rejected (win32 error {Marshal.GetLastWin32Error()}).");
    }

    private static Input KeyboardEvent(ushort virtualKey, char scan, uint flags) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion
        {
            Keyboard = new KeyboardInput
            {
                VirtualKey = virtualKey,
                Scan = scan,
                Flags = flags,
                Time = 0,
                ExtraInfo = IntPtr.Zero,
            },
        },
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, ref Input inputs, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);

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

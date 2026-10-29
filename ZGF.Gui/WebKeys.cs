using ZGF.KeyboardModule;

namespace ZGF.Gui;

public static class WebKeys
{
    private const int Extended = 0x100;

    /// <summary>The Windows virtual-key code a browser expects for <paramref name="key"/> on a key down or
    /// up: letters and digits as their ASCII code, punctuation as its US-layout <c>VK_OEM_*</c> code (so
    /// '.' is OEM_PERIOD, not VK_DELETE). 0 for keys without one.</summary>
    public static int VirtualKeyOf(KeyboardKey key)
    {
        if (key is >= KeyboardKey.A and <= KeyboardKey.Z)
            return 'A' + (key - KeyboardKey.A);
        if (key is >= KeyboardKey.Alpha0 and <= KeyboardKey.Alpha9)
            return '0' + (key - KeyboardKey.Alpha0);
        if (key is >= KeyboardKey.F1 and <= KeyboardKey.F24)
            return 0x70 + (key - KeyboardKey.F1);
        if (key is >= KeyboardKey.Numpad0 and <= KeyboardKey.Numpad9)
            return 0x60 + (key - KeyboardKey.Numpad0);
        return key switch
        {
            KeyboardKey.Backspace => 0x08,
            KeyboardKey.Tab => 0x09,
            KeyboardKey.Enter or KeyboardKey.NumpadEnter => 0x0D,
            KeyboardKey.Pause => 0x13,
            KeyboardKey.CapsLock => 0x14,
            KeyboardKey.Escape => 0x1B,
            KeyboardKey.Space => 0x20,
            KeyboardKey.PageUp => 0x21,
            KeyboardKey.PageDown => 0x22,
            KeyboardKey.End => 0x23,
            KeyboardKey.Home => 0x24,
            KeyboardKey.LeftArrow => 0x25,
            KeyboardKey.UpArrow => 0x26,
            KeyboardKey.RightArrow => 0x27,
            KeyboardKey.DownArrow => 0x28,
            KeyboardKey.PrintScreen => 0x2C,
            KeyboardKey.Insert => 0x2D,
            KeyboardKey.Delete => 0x2E,
            KeyboardKey.LeftShift or KeyboardKey.RightShift => 0x10,
            KeyboardKey.LeftControl or KeyboardKey.RightControl => 0x11,
            KeyboardKey.LeftAlt or KeyboardKey.RightAlt => 0x12,
            KeyboardKey.LeftSuper => 0x5B,
            KeyboardKey.RightSuper => 0x5C,
            KeyboardKey.Menu => 0x5D,
            KeyboardKey.NumpadMultiply => 0x6A,
            KeyboardKey.NumpadAdd => 0x6B,
            KeyboardKey.NumpadSubtract => 0x6D,
            KeyboardKey.NumpadDecimal => 0x6E,
            KeyboardKey.NumpadDivide => 0x6F,
            KeyboardKey.NumLock => 0x90,
            KeyboardKey.ScrollLock => 0x91,
            KeyboardKey.SemiColon => 0xBA,
            KeyboardKey.Equals or KeyboardKey.NumpadEquals => 0xBB,
            KeyboardKey.Comma => 0xBC,
            KeyboardKey.Minus => 0xBD,
            KeyboardKey.Period => 0xBE,
            KeyboardKey.Slash => 0xBF,
            KeyboardKey.GraveAccent => 0xC0,
            KeyboardKey.LeftBracket => 0xDB,
            KeyboardKey.Backslash => 0xDC,
            KeyboardKey.RightBracket => 0xDD,
            KeyboardKey.Apostrophe => 0xDE,
            _ => 0,
        };
    }

    /// <summary>
    /// The native key code CEF on Windows takes a key event's physical key from: its set-1 scan code, with
    /// <c>0xE000</c> added for the keys sent with an <c>E0</c> prefix. Chromium turns it into the DOM
    /// <c>KeyboardEvent.code</c> (<c>"KeyW"</c>, <c>"ArrowUp"</c>), which games read for movement; given
    /// anything else, such as the virtual-key code, a page reads W as <c>"F11"</c>. 0 for keys without one.
    /// </summary>
    public static int NativeKeyCodeOf(KeyboardKey key)
    {
        var scan = ScanCodeOf(key);
        return (scan & Extended) != 0 ? 0xE000 | (scan & 0xFF) : scan;
    }

    /// <summary>The PC set-1 scan code of the key at <paramref name="key"/>'s position, with
    /// <see cref="Extended"/> set for the keys sent with an <c>E0</c> prefix; 0 for none.</summary>
    private static int ScanCodeOf(KeyboardKey key)
    {
        if (key is >= KeyboardKey.F1 and <= KeyboardKey.F10)
            return 0x3B + (key - KeyboardKey.F1);
        if (key is >= KeyboardKey.F13 and <= KeyboardKey.F24)
            return 0x64 + (key - KeyboardKey.F13);
        return key switch
        {
            KeyboardKey.Escape => 0x01,
            KeyboardKey.Alpha1 => 0x02,
            KeyboardKey.Alpha2 => 0x03,
            KeyboardKey.Alpha3 => 0x04,
            KeyboardKey.Alpha4 => 0x05,
            KeyboardKey.Alpha5 => 0x06,
            KeyboardKey.Alpha6 => 0x07,
            KeyboardKey.Alpha7 => 0x08,
            KeyboardKey.Alpha8 => 0x09,
            KeyboardKey.Alpha9 => 0x0A,
            KeyboardKey.Alpha0 => 0x0B,
            KeyboardKey.Minus => 0x0C,
            KeyboardKey.Equals => 0x0D,
            KeyboardKey.Backspace => 0x0E,
            KeyboardKey.Tab => 0x0F,
            KeyboardKey.Q => 0x10,
            KeyboardKey.W => 0x11,
            KeyboardKey.E => 0x12,
            KeyboardKey.R => 0x13,
            KeyboardKey.T => 0x14,
            KeyboardKey.Y => 0x15,
            KeyboardKey.U => 0x16,
            KeyboardKey.I => 0x17,
            KeyboardKey.O => 0x18,
            KeyboardKey.P => 0x19,
            KeyboardKey.LeftBracket => 0x1A,
            KeyboardKey.RightBracket => 0x1B,
            KeyboardKey.Enter => 0x1C,
            KeyboardKey.LeftControl => 0x1D,
            KeyboardKey.A => 0x1E,
            KeyboardKey.S => 0x1F,
            KeyboardKey.D => 0x20,
            KeyboardKey.F => 0x21,
            KeyboardKey.G => 0x22,
            KeyboardKey.H => 0x23,
            KeyboardKey.J => 0x24,
            KeyboardKey.K => 0x25,
            KeyboardKey.L => 0x26,
            KeyboardKey.SemiColon => 0x27,
            KeyboardKey.Apostrophe => 0x28,
            KeyboardKey.GraveAccent => 0x29,
            KeyboardKey.LeftShift => 0x2A,
            KeyboardKey.Backslash => 0x2B,
            KeyboardKey.Z => 0x2C,
            KeyboardKey.X => 0x2D,
            KeyboardKey.C => 0x2E,
            KeyboardKey.V => 0x2F,
            KeyboardKey.B => 0x30,
            KeyboardKey.N => 0x31,
            KeyboardKey.M => 0x32,
            KeyboardKey.Comma => 0x33,
            KeyboardKey.Period => 0x34,
            KeyboardKey.Slash => 0x35,
            KeyboardKey.RightShift => 0x36,
            KeyboardKey.NumpadMultiply => 0x37,
            KeyboardKey.LeftAlt => 0x38,
            KeyboardKey.Space => 0x39,
            KeyboardKey.CapsLock => 0x3A,
            KeyboardKey.NumLock => Extended | 0x45,
            KeyboardKey.ScrollLock => 0x46,
            KeyboardKey.Numpad7 => 0x47,
            KeyboardKey.Numpad8 => 0x48,
            KeyboardKey.Numpad9 => 0x49,
            KeyboardKey.NumpadSubtract => 0x4A,
            KeyboardKey.Numpad4 => 0x4B,
            KeyboardKey.Numpad5 => 0x4C,
            KeyboardKey.Numpad6 => 0x4D,
            KeyboardKey.NumpadAdd => 0x4E,
            KeyboardKey.Numpad1 => 0x4F,
            KeyboardKey.Numpad2 => 0x50,
            KeyboardKey.Numpad3 => 0x51,
            KeyboardKey.Numpad0 => 0x52,
            KeyboardKey.NumpadDecimal => 0x53,
            KeyboardKey.F11 => 0x57,
            KeyboardKey.F12 => 0x58,
            KeyboardKey.NumpadEquals => 0x59,
            KeyboardKey.NumpadEnter => Extended | 0x1C,
            KeyboardKey.RightControl => Extended | 0x1D,
            KeyboardKey.NumpadDivide => Extended | 0x35,
            KeyboardKey.PrintScreen => Extended | 0x37,
            KeyboardKey.RightAlt => Extended | 0x38,
            KeyboardKey.Pause => 0x45,
            KeyboardKey.Home => Extended | 0x47,
            KeyboardKey.UpArrow => Extended | 0x48,
            KeyboardKey.PageUp => Extended | 0x49,
            KeyboardKey.LeftArrow => Extended | 0x4B,
            KeyboardKey.RightArrow => Extended | 0x4D,
            KeyboardKey.End => Extended | 0x4F,
            KeyboardKey.DownArrow => Extended | 0x50,
            KeyboardKey.PageDown => Extended | 0x51,
            KeyboardKey.Insert => Extended | 0x52,
            KeyboardKey.Delete => Extended | 0x53,
            KeyboardKey.LeftSuper => Extended | 0x5B,
            KeyboardKey.RightSuper => Extended | 0x5C,
            KeyboardKey.Menu => Extended | 0x5D,
            _ => 0,
        };
    }
}

using ZGF.KeyboardModule;

namespace ZGF.Gui;

public static class WebKeys
{
    /// <summary>The Windows virtual-key code a browser expects for <paramref name="key"/> on a key down or
    /// up: letters and digits as their ASCII code, punctuation as its US-layout <c>VK_OEM_*</c> code (so
    /// '.' is OEM_PERIOD, not VK_DELETE). 0 for keys without one.</summary>
    public static int VirtualKeyOf(KeyboardKey key)
    {
        if (key is >= KeyboardKey.A and <= KeyboardKey.Z)
            return 'A' + (key - KeyboardKey.A);
        if (key is >= KeyboardKey.Alpha0 and <= KeyboardKey.Alpha9)
            return '0' + (key - KeyboardKey.Alpha0);
        if (key is >= KeyboardKey.F1 and <= KeyboardKey.F12)
            return 0x70 + (key - KeyboardKey.F1);
        return key switch
        {
            KeyboardKey.Backspace => 0x08,
            KeyboardKey.Tab => 0x09,
            KeyboardKey.Enter or KeyboardKey.NumpadEnter => 0x0D,
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
            KeyboardKey.Insert => 0x2D,
            KeyboardKey.Delete => 0x2E,
            KeyboardKey.LeftShift or KeyboardKey.RightShift => 0x10,
            KeyboardKey.LeftControl or KeyboardKey.RightControl => 0x11,
            KeyboardKey.LeftAlt or KeyboardKey.RightAlt => 0x12,
            KeyboardKey.LeftSuper or KeyboardKey.RightSuper => 0x5B,
            KeyboardKey.SemiColon => 0xBA,
            KeyboardKey.Equals => 0xBB,
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
}

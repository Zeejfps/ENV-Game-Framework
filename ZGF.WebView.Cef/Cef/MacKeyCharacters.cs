namespace ZGF.WebView.Cef;

/// <summary>
/// CEF on macOS builds a key event's DOM <c>key</c> from its characters, as an <c>NSEvent</c> carries
/// them, rather than from the Windows virtual-key code. These are the characters a US-layout key down
/// or up would carry: the typed character, or Cocoa's function-key character for keys that type none.
/// </summary>
internal static class MacKeyCharacters
{
    private const string Digits = "0123456789";
    private const string ShiftedDigits = ")!@#$%^&*(";

    public static char Of(int windowsKeyCode, bool shift)
    {
        if (windowsKeyCode is >= 'A' and <= 'Z')
            return shift ? (char)windowsKeyCode : char.ToLowerInvariant((char)windowsKeyCode);
        if (windowsKeyCode is >= '0' and <= '9')
            return (shift ? ShiftedDigits : Digits)[windowsKeyCode - '0'];
        if (windowsKeyCode is >= 0x60 and <= 0x69)
            return Digits[windowsKeyCode - 0x60];
        if (windowsKeyCode is >= 0x70 and <= 0x87)
            return (char)(0xF704 + windowsKeyCode - 0x70); // NSF1FunctionKey onwards
        return windowsKeyCode switch
        {
            0x08 => '\u007F', // Backspace sends DEL on macOS
            0x09 => '\t',
            0x0D => '\r',
            0x1B => '\u001B',
            0x20 => ' ',
            0x21 => '', // NSPageUpFunctionKey
            0x22 => '', // NSPageDownFunctionKey
            0x23 => '', // NSEndFunctionKey
            0x24 => '', // NSHomeFunctionKey
            0x25 => '', // NSLeftArrowFunctionKey
            0x26 => '', // NSUpArrowFunctionKey
            0x27 => '', // NSRightArrowFunctionKey
            0x28 => '', // NSDownArrowFunctionKey
            0x2D => '', // NSHelpFunctionKey, Insert's place on a Mac keyboard
            0x2E => '', // NSDeleteFunctionKey
            0x6A => '*',
            0x6B => '+',
            0x6D => '-',
            0x6E => '.',
            0x6F => '/',
            0xBA => shift ? ':' : ';',
            0xBB => shift ? '+' : '=',
            0xBC => shift ? '<' : ',',
            0xBD => shift ? '_' : '-',
            0xBE => shift ? '>' : '.',
            0xBF => shift ? '?' : '/',
            0xC0 => shift ? '~' : '`',
            0xDB => shift ? '{' : '[',
            0xDC => shift ? '|' : '\\',
            0xDD => shift ? '}' : ']',
            0xDE => shift ? '"' : '\'',
            _ => '\0',
        };
    }
}

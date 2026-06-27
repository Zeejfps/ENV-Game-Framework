using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Input;

/// <summary>Character → (key, shift) over the ASCII subset, shared by the test harness's
/// <c>Type</c> and the live debug server's <c>/type</c>. Round-trips against
/// <see cref="KeyboardKeyExtensions.ToChar"/> (asserted by a test), so it can't drift from the
/// controller's decoding.</summary>
public static class AsciiKeyMap
{
    public static readonly IReadOnlyDictionary<char, (KeyboardKey Key, bool Shift)> Map =
        new Dictionary<char, (KeyboardKey, bool)>
        {
            ['1'] = (KeyboardKey.Alpha1, false), ['!'] = (KeyboardKey.Alpha1, true),
            ['2'] = (KeyboardKey.Alpha2, false), ['@'] = (KeyboardKey.Alpha2, true),
            ['3'] = (KeyboardKey.Alpha3, false), ['#'] = (KeyboardKey.Alpha3, true),
            ['4'] = (KeyboardKey.Alpha4, false), ['$'] = (KeyboardKey.Alpha4, true),
            ['5'] = (KeyboardKey.Alpha5, false), ['%'] = (KeyboardKey.Alpha5, true),
            ['6'] = (KeyboardKey.Alpha6, false), ['^'] = (KeyboardKey.Alpha6, true),
            ['7'] = (KeyboardKey.Alpha7, false), ['&'] = (KeyboardKey.Alpha7, true),
            ['8'] = (KeyboardKey.Alpha8, false), ['*'] = (KeyboardKey.Alpha8, true),
            ['9'] = (KeyboardKey.Alpha9, false), ['('] = (KeyboardKey.Alpha9, true),
            ['0'] = (KeyboardKey.Alpha0, false), [')'] = (KeyboardKey.Alpha0, true),
            ['a'] = (KeyboardKey.A, false), ['A'] = (KeyboardKey.A, true),
            ['b'] = (KeyboardKey.B, false), ['B'] = (KeyboardKey.B, true),
            ['c'] = (KeyboardKey.C, false), ['C'] = (KeyboardKey.C, true),
            ['d'] = (KeyboardKey.D, false), ['D'] = (KeyboardKey.D, true),
            ['e'] = (KeyboardKey.E, false), ['E'] = (KeyboardKey.E, true),
            ['f'] = (KeyboardKey.F, false), ['F'] = (KeyboardKey.F, true),
            ['g'] = (KeyboardKey.G, false), ['G'] = (KeyboardKey.G, true),
            ['h'] = (KeyboardKey.H, false), ['H'] = (KeyboardKey.H, true),
            ['i'] = (KeyboardKey.I, false), ['I'] = (KeyboardKey.I, true),
            ['j'] = (KeyboardKey.J, false), ['J'] = (KeyboardKey.J, true),
            ['k'] = (KeyboardKey.K, false), ['K'] = (KeyboardKey.K, true),
            ['l'] = (KeyboardKey.L, false), ['L'] = (KeyboardKey.L, true),
            ['m'] = (KeyboardKey.M, false), ['M'] = (KeyboardKey.M, true),
            ['n'] = (KeyboardKey.N, false), ['N'] = (KeyboardKey.N, true),
            ['o'] = (KeyboardKey.O, false), ['O'] = (KeyboardKey.O, true),
            ['p'] = (KeyboardKey.P, false), ['P'] = (KeyboardKey.P, true),
            ['q'] = (KeyboardKey.Q, false), ['Q'] = (KeyboardKey.Q, true),
            ['r'] = (KeyboardKey.R, false), ['R'] = (KeyboardKey.R, true),
            ['s'] = (KeyboardKey.S, false), ['S'] = (KeyboardKey.S, true),
            ['t'] = (KeyboardKey.T, false), ['T'] = (KeyboardKey.T, true),
            ['u'] = (KeyboardKey.U, false), ['U'] = (KeyboardKey.U, true),
            ['v'] = (KeyboardKey.V, false), ['V'] = (KeyboardKey.V, true),
            ['w'] = (KeyboardKey.W, false), ['W'] = (KeyboardKey.W, true),
            ['x'] = (KeyboardKey.X, false), ['X'] = (KeyboardKey.X, true),
            ['y'] = (KeyboardKey.Y, false), ['Y'] = (KeyboardKey.Y, true),
            ['z'] = (KeyboardKey.Z, false), ['Z'] = (KeyboardKey.Z, true),
            [' '] = (KeyboardKey.Space, false),
            ['\''] = (KeyboardKey.Apostrophe, false), ['"'] = (KeyboardKey.Apostrophe, true),
            [','] = (KeyboardKey.Comma, false), ['<'] = (KeyboardKey.Comma, true),
            ['.'] = (KeyboardKey.Period, false), ['>'] = (KeyboardKey.Period, true),
            ['/'] = (KeyboardKey.Slash, false), ['?'] = (KeyboardKey.Slash, true),
            [';'] = (KeyboardKey.SemiColon, false), [':'] = (KeyboardKey.SemiColon, true),
            ['='] = (KeyboardKey.Equals, false), ['+'] = (KeyboardKey.Equals, true),
            ['-'] = (KeyboardKey.Minus, false), ['_'] = (KeyboardKey.Minus, true),
            ['['] = (KeyboardKey.LeftBracket, false), ['{'] = (KeyboardKey.LeftBracket, true),
            [']'] = (KeyboardKey.RightBracket, false), ['}'] = (KeyboardKey.RightBracket, true),
            ['\\'] = (KeyboardKey.Backslash, false), ['|'] = (KeyboardKey.Backslash, true),
            ['`'] = (KeyboardKey.GraveAccent, false), ['~'] = (KeyboardKey.GraveAccent, true),
            ['\t'] = (KeyboardKey.Tab, false),
            ['\n'] = (KeyboardKey.Enter, false),
        };
}

namespace ZGF.KeyboardModule;

public static class KeyboardKeyExtensions
{
    public static char ToChar(this KeyboardKey key, bool isShiftPressed = false)
    {
        return key switch
        {
            KeyboardKey.Alpha1 => isShiftPressed ? '!' : '1',
            KeyboardKey.Alpha2 => isShiftPressed ? '@' : '2',
            KeyboardKey.Alpha3 => isShiftPressed ? '#' : '3',
            KeyboardKey.Alpha4 => isShiftPressed ? '$' : '4',
            KeyboardKey.Alpha5 => isShiftPressed ? '%' : '5',
            KeyboardKey.Alpha6 => isShiftPressed ? '^' : '6',
            KeyboardKey.Alpha7 => isShiftPressed ? '&' : '7',
            KeyboardKey.Alpha8 => isShiftPressed ? '*' : '8',
            KeyboardKey.Alpha9 => isShiftPressed ? '(' : '9',
            KeyboardKey.Alpha0 => isShiftPressed ? ')' : '0',
            KeyboardKey.A => isShiftPressed ? 'A' : 'a',
            KeyboardKey.B => isShiftPressed ? 'B' : 'b',
            KeyboardKey.C => isShiftPressed ? 'C' : 'c',
            KeyboardKey.D => isShiftPressed ? 'D' : 'd',
            KeyboardKey.E => isShiftPressed ? 'E' : 'e',
            KeyboardKey.F => isShiftPressed ? 'F' : 'f',
            KeyboardKey.G => isShiftPressed ? 'G' : 'g',
            KeyboardKey.H => isShiftPressed ? 'H' : 'h',
            KeyboardKey.I => isShiftPressed ? 'I' : 'i',
            KeyboardKey.J => isShiftPressed ? 'J' : 'j',
            KeyboardKey.K => isShiftPressed ? 'K' : 'k',
            KeyboardKey.L => isShiftPressed ? 'L' : 'l',
            KeyboardKey.M => isShiftPressed ? 'M' : 'm',
            KeyboardKey.N => isShiftPressed ? 'N' : 'n',
            KeyboardKey.O => isShiftPressed ? 'O' : 'o',
            KeyboardKey.P => isShiftPressed ? 'P' : 'p',
            KeyboardKey.Q => isShiftPressed ? 'Q' : 'q',
            KeyboardKey.R => isShiftPressed ? 'R' : 'r',
            KeyboardKey.S => isShiftPressed ? 'S' : 's',
            KeyboardKey.T => isShiftPressed ? 'T' : 't',
            KeyboardKey.U => isShiftPressed ? 'U' : 'u',
            KeyboardKey.V => isShiftPressed ? 'V' : 'v',
            KeyboardKey.W => isShiftPressed ? 'W' : 'w',
            KeyboardKey.X => isShiftPressed ? 'X' : 'x',
            KeyboardKey.Y => isShiftPressed ? 'Y' : 'y',
            KeyboardKey.Z => isShiftPressed ? 'Z' : 'z',
            KeyboardKey.Space => ' ',
            KeyboardKey.Apostrophe => isShiftPressed ? '"' : '\'',
            KeyboardKey.Comma => isShiftPressed ? '<' : ',',
            KeyboardKey.Period => isShiftPressed ? '>' : '.',
            KeyboardKey.Slash => isShiftPressed ? '?' : '/',
            KeyboardKey.SemiColon => isShiftPressed ? ':' : ';',
            KeyboardKey.Equals => isShiftPressed ? '+' : '=',
            KeyboardKey.Minus => isShiftPressed ? '_' : '-',
            _ => '\0'
        };
    }
}
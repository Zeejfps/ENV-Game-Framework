namespace ZGF.KeyboardModule;

public static class KeyboardKeyExtensions
{
    public static char ToChar(this KeyboardKey key, bool isShiftPressed = false)
    {
        switch (key)
        {
            case KeyboardKey.Alpha1: return isShiftPressed ?  '!' : '1';
            case KeyboardKey.Alpha2: return isShiftPressed ?  '@' : '2';
            case KeyboardKey.Alpha3: return isShiftPressed ?  '#' : '3';
            case KeyboardKey.Alpha4: return isShiftPressed ?  '$' : '4';
            case KeyboardKey.Alpha5: return isShiftPressed ?  '%' : '5';
            case KeyboardKey.Alpha6: return isShiftPressed ?  '^' : '6';
            case KeyboardKey.Alpha7: return isShiftPressed ?  '&' : '7';
            case KeyboardKey.Alpha8: return isShiftPressed ?  '*' : '8';
            case KeyboardKey.Alpha9: return isShiftPressed ?  '(' : '9';
            case KeyboardKey.Alpha0: return isShiftPressed ?  ')' : '0';

            case KeyboardKey.A: return isShiftPressed ? 'A' : 'a';
            case KeyboardKey.B: return isShiftPressed ? 'B' : 'b';
            case KeyboardKey.C: return isShiftPressed ? 'C' : 'c';
            case KeyboardKey.D: return isShiftPressed ? 'D' : 'd';
            case KeyboardKey.E: return isShiftPressed ? 'E' : 'e';
            case KeyboardKey.F: return isShiftPressed ? 'F' : 'f';
            case KeyboardKey.G: return isShiftPressed ? 'G' : 'g';
            case KeyboardKey.H: return isShiftPressed ? 'H' : 'h';
            case KeyboardKey.I: return isShiftPressed ? 'I' : 'i';
            case KeyboardKey.J: return isShiftPressed ? 'J' : 'j';
            case KeyboardKey.K: return isShiftPressed ? 'K' : 'k';
            case KeyboardKey.L: return isShiftPressed ? 'L' : 'l';
            case KeyboardKey.M: return isShiftPressed ? 'M' : 'm';
            case KeyboardKey.N: return isShiftPressed ? 'N' : 'n';
            case KeyboardKey.O: return isShiftPressed ? 'O' : 'o';
            case KeyboardKey.P: return isShiftPressed ? 'P' : 'p';
            case KeyboardKey.Q: return isShiftPressed ? 'Q' : 'q';
            case KeyboardKey.R: return isShiftPressed ? 'R' : 'r';
            case KeyboardKey.S: return isShiftPressed ? 'S' : 's';
            case KeyboardKey.T: return isShiftPressed ? 'T' : 't';
            case KeyboardKey.U: return isShiftPressed ? 'U' : 'u';
            case KeyboardKey.V: return isShiftPressed ? 'V' : 'v';
            case KeyboardKey.W: return isShiftPressed ? 'W' : 'w';
            case KeyboardKey.X: return isShiftPressed ? 'X' : 'x';
            case KeyboardKey.Y: return isShiftPressed ? 'Y' : 'y';
            case KeyboardKey.Z: return isShiftPressed ? 'Z' : 'z';

            case KeyboardKey.Space: return ' ';
            case KeyboardKey.Apostrophe: return isShiftPressed ? '"' : '\'';
            case KeyboardKey.Comma: return isShiftPressed ? '<' : ',';
            case KeyboardKey.Period: return isShiftPressed ? '>' : '.';
            case KeyboardKey.Slash: return isShiftPressed ? '?' : '/';
            case KeyboardKey.SemiColon: return isShiftPressed ? ':' : ';';
            case KeyboardKey.Equals: return isShiftPressed ? '+' : '=';
            case KeyboardKey.Minus: return isShiftPressed ? '_' : '-';

            case KeyboardKey.Escape:
            case KeyboardKey.Backspace:
            case KeyboardKey.UpArrow:
            case KeyboardKey.DownArrow:
            case KeyboardKey.LeftArrow:
            case KeyboardKey.RightArrow:
                return '\0'; // Not a printable character

            default:
                return '\0'; // Unhandled key
        }
    }
}
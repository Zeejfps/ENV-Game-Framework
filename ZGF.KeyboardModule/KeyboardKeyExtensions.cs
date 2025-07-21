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

            case KeyboardKey.A: return 'a';
            case KeyboardKey.B: return 'b';
            case KeyboardKey.C: return 'c';
            case KeyboardKey.D: return 'd';
            case KeyboardKey.E: return 'e';
            case KeyboardKey.F: return 'f';
            case KeyboardKey.G: return 'g';
            case KeyboardKey.H: return 'h';
            case KeyboardKey.I: return 'i';
            case KeyboardKey.J: return 'j';
            case KeyboardKey.K: return 'k';
            case KeyboardKey.L: return 'l';
            case KeyboardKey.M: return 'm';
            case KeyboardKey.N: return 'n';
            case KeyboardKey.O: return 'o';
            case KeyboardKey.P: return 'p';
            case KeyboardKey.Q: return 'q';
            case KeyboardKey.R: return 'r';
            case KeyboardKey.S: return 's';
            case KeyboardKey.T: return 't';
            case KeyboardKey.U: return 'u';
            case KeyboardKey.V: return 'v';
            case KeyboardKey.W: return 'w';
            case KeyboardKey.X: return 'x';
            case KeyboardKey.Y: return 'y';
            case KeyboardKey.Z: return 'z';

            case KeyboardKey.Space: return ' ';
            case KeyboardKey.Apostrophe: return '\'';
            case KeyboardKey.Comma: return ',';
            case KeyboardKey.Period: return '.';
            case KeyboardKey.Slash: return '/';
            case KeyboardKey.SemiColon: return ';';
            case KeyboardKey.Equals: return '=';
            case KeyboardKey.Minus: return '-';

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
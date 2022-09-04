using EasyGameFramework.Api.InputDevices;
using GLFW;

namespace EasyGameFramework.Glfw;

public static class KeysExtensions
{
    public static KeyboardKey ToKeyboardKey(this Keys key)
    {
        switch (key)
        {
            case Keys.Unknown:
                return KeyboardKey.Unknown;
            case Keys.Space:
                return KeyboardKey.Space;
            case Keys.Apostrophe:
                return KeyboardKey.Apostrophe;
            case Keys.Comma:
                return KeyboardKey.Comma;
            case Keys.Minus:
                return KeyboardKey.Minus;
            case Keys.Period:
                return KeyboardKey.Period;
            case Keys.Slash:
                return KeyboardKey.Slash;
            case Keys.Alpha0:
                return KeyboardKey.Alpha0;
            case Keys.Alpha1:
                return KeyboardKey.Alpha1;
            case Keys.Alpha2:
                return KeyboardKey.Alpha2;
            case Keys.Alpha3:
                return KeyboardKey.Alpha3;
            case Keys.Alpha4:
                return KeyboardKey.Alpha4;
            case Keys.Alpha5:
                break;
            case Keys.Alpha6:
                break;
            case Keys.Alpha7:
                break;
            case Keys.Alpha8:
                break;
            case Keys.Alpha9:
                break;
            case Keys.SemiColon:
                break;
            case Keys.Equal:
                return KeyboardKey.Equals;
            case Keys.A:
                return KeyboardKey.A;
            case Keys.B:
                return KeyboardKey.B;
            case Keys.C:
                return KeyboardKey.C;
            case Keys.D:
                return KeyboardKey.D;
            case Keys.E:
                return KeyboardKey.E;
            case Keys.F:
                return KeyboardKey.F;
            case Keys.G:
                return KeyboardKey.G;
            case Keys.H:
                return KeyboardKey.H;
            case Keys.I:
                return KeyboardKey.I;
            case Keys.J:
                return KeyboardKey.J;
            case Keys.K:
                return KeyboardKey.K;
            case Keys.L:
                break;
            case Keys.M:
                break;
            case Keys.N:
                break;
            case Keys.O:
                break;
            case Keys.P:
                break;
            case Keys.Q:
                return KeyboardKey.Q;
            case Keys.R:
                return KeyboardKey.R;
            case Keys.S:
                return KeyboardKey.S;
            case Keys.T:
                return KeyboardKey.T;
            case Keys.U:
                break;
            case Keys.V:
                break;
            case Keys.W:
                return KeyboardKey.W;
            case Keys.X:
                break;
            case Keys.Y:
                return KeyboardKey.Y;
            case Keys.Z:
                break;
            case Keys.LeftBracket:
                break;
            case Keys.Backslash:
                break;
            case Keys.RightBracket:
                break;
            case Keys.GraveAccent:
                break;
            case Keys.World1:
                break;
            case Keys.World2:
                break;
            case Keys.Escape:
                return KeyboardKey.Escape;
            case Keys.Enter:
                break;
            case Keys.Tab:
                break;
            case Keys.Backspace:
                break;
            case Keys.Insert:
                break;
            case Keys.Delete:
                break;
            case Keys.Right:
                return KeyboardKey.RightArrow;
            case Keys.Left:
                return KeyboardKey.LeftArrow;
            case Keys.Down:
                return KeyboardKey.DownArrow;
            case Keys.Up:
                return KeyboardKey.UpArrow;
            case Keys.PageUp:
                break;
            case Keys.PageDown:
                break;
            case Keys.Home:
                break;
            case Keys.End:
                break;
            case Keys.CapsLock:
                break;
            case Keys.ScrollLock:
                break;
            case Keys.NumLock:
                break;
            case Keys.PrintScreen:
                break;
            case Keys.Pause:
                break;
            case Keys.F1:
                break;
            case Keys.F2:
                break;
            case Keys.F3:
                break;
            case Keys.F4:
                break;
            case Keys.F5:
                break;
            case Keys.F6:
                break;
            case Keys.F7:
                break;
            case Keys.F8:
                break;
            case Keys.F9:
                break;
            case Keys.F10:
                break;
            case Keys.F11:
                break;
            case Keys.F12:
                break;
            case Keys.F13:
                break;
            case Keys.F14:
                break;
            case Keys.F15:
                break;
            case Keys.F16:
                break;
            case Keys.F17:
                break;
            case Keys.F18:
                break;
            case Keys.F19:
                break;
            case Keys.F20:
                break;
            case Keys.F21:
                break;
            case Keys.F22:
                break;
            case Keys.F23:
                break;
            case Keys.F24:
                break;
            case Keys.F25:
                break;
            case Keys.Numpad0:
                break;
            case Keys.Numpad1:
                return KeyboardKey.Alpha1;
            case Keys.Numpad2:
                return KeyboardKey.Alpha2;
            case Keys.Numpad3:
                return KeyboardKey.Alpha3;
            case Keys.Numpad4:
                return KeyboardKey.Alpha4;
            case Keys.Numpad5:
                break;
            case Keys.Numpad6:
                break;
            case Keys.Numpad7:
                break;
            case Keys.Numpad8:
                break;
            case Keys.Numpad9:
                break;
            case Keys.NumpadDecimal:
                break;
            case Keys.NumpadDivide:
                break;
            case Keys.NumpadMultiply:
                break;
            case Keys.NumpadSubtract:
                break;
            case Keys.NumpadAdd:
                break;
            case Keys.NumpadEnter:
                break;
            case Keys.NumpadEqual:
                break;
            case Keys.LeftShift:
                break;
            case Keys.LeftControl:
                break;
            case Keys.LeftAlt:
                break;
            case Keys.LeftSuper:
                break;
            case Keys.RightShift:
                break;
            case Keys.RightControl:
                break;
            case Keys.RightAlt:
                break;
            case Keys.RightSuper:
                break;
            case Keys.Menu:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        return KeyboardKey.D;
    }
}
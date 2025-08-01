﻿using GLFW;

namespace ZGF.KeyboardModule.GlfwAdapter;

public static class KeysExtensions
{
    public static KeyboardKey Adapt(this Keys key)
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
                return KeyboardKey.Alpha5;
            case Keys.Alpha6:
                return KeyboardKey.Alpha6;
            case Keys.Alpha7:
                return KeyboardKey.Alpha7;
            case Keys.Alpha8:
                return KeyboardKey.Alpha8;
            case Keys.Alpha9:
                return KeyboardKey.Alpha9;
            case Keys.SemiColon:
                return KeyboardKey.SemiColon;
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
                return KeyboardKey.L;
            case Keys.M:
                return KeyboardKey.M;
            case Keys.N:
                return KeyboardKey.N;
            case Keys.O:
                return KeyboardKey.O;
            case Keys.P:
                return KeyboardKey.P;
            case Keys.Q:
                return KeyboardKey.Q;
            case Keys.R:
                return KeyboardKey.R;
            case Keys.S:
                return KeyboardKey.S;
            case Keys.T:
                return KeyboardKey.T;
            case Keys.U:
                return KeyboardKey.U;
            case Keys.V:
                return KeyboardKey.V;
            case Keys.W:
                return KeyboardKey.W;
            case Keys.X:
                return KeyboardKey.X;
            case Keys.Y:
                return KeyboardKey.Y;
            case Keys.Z:
                return KeyboardKey.Z;
            case Keys.LeftBracket:
            case Keys.Backslash:
            case Keys.RightBracket:
            case Keys.GraveAccent:
            case Keys.World1:
            case Keys.World2:
                break;
            case Keys.Escape:
                return KeyboardKey.Escape;
            case Keys.Enter:
            case Keys.Tab:
                break;
            case Keys.Backspace:
                return KeyboardKey.Backspace;
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
            case Keys.PageDown:
            case Keys.Home:
            case Keys.End:
            case Keys.CapsLock:
            case Keys.ScrollLock:
            case Keys.NumLock:
            case Keys.PrintScreen:
            case Keys.Pause:
            case Keys.F1:
            case Keys.F2:
            case Keys.F3:
            case Keys.F4:
            case Keys.F5:
            case Keys.F6:
            case Keys.F7:
            case Keys.F8:
            case Keys.F9:
            case Keys.F10:
            case Keys.F11:
            case Keys.F12:
            case Keys.F13:
            case Keys.F14:
            case Keys.F15:
            case Keys.F16:
            case Keys.F17:
            case Keys.F18:
            case Keys.F19:
            case Keys.F20:
            case Keys.F21:
            case Keys.F22:
            case Keys.F23:
            case Keys.F24:
            case Keys.F25:
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
            case Keys.Numpad6:
            case Keys.Numpad7:
            case Keys.Numpad8:
            case Keys.Numpad9:
            case Keys.NumpadDecimal:
            case Keys.NumpadDivide:
            case Keys.NumpadMultiply:
            case Keys.NumpadSubtract:
            case Keys.NumpadAdd:
            case Keys.NumpadEnter:
            case Keys.NumpadEqual:
            case Keys.LeftShift:
            case Keys.LeftControl:
            case Keys.LeftAlt:
            case Keys.LeftSuper:
            case Keys.RightShift:
            case Keys.RightControl:
            case Keys.RightAlt:
            case Keys.RightSuper:
            case Keys.Menu:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        return KeyboardKey.Unknown;
    }
}
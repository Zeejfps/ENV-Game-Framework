using EasyGameFramework.Api.InputDevices;
using GLFW;

namespace EasyGameFramework.Glfw;

internal static class GamepadButtonMapper_GLFW
{
    public static GamepadButton ToGamepadButton(this GamePadButton button)
    {
        return button switch
        {
            GamePadButton.A => GamepadButton.South,
            GamePadButton.B => GamepadButton.East,
            GamePadButton.X => GamepadButton.West,
            GamePadButton.Y => GamepadButton.North,
            GamePadButton.LeftBumper => GamepadButton.LeftBumper,
            GamePadButton.RightBumper => GamepadButton.RightBumper,
            GamePadButton.Back => GamepadButton.Back,
            GamePadButton.Start => GamepadButton.Start,
            GamePadButton.Guide => GamepadButton.Guide,
            GamePadButton.LeftThumb => GamepadButton.LeftThumb,
            GamePadButton.RightThumb => GamepadButton.RightThumb,
            GamePadButton.DpadUp => GamepadButton.DPadUp,
            GamePadButton.DpadRight => GamepadButton.DPadRight,
            GamePadButton.DpadDown => GamepadButton.DPadDown,
            GamePadButton.DpadLeft => GamepadButton.DPadLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };
    } 
}
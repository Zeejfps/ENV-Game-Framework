using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class TestBindings
{
    public InputAction MoveLeft { get; } = new()
    {
        Bindings = new IButtonBinding[]
        {
            new KeyboardKeyBinding(KeyboardKey.A),
            new GamepadButtonBinding(0, GamepadButton.South),
            new GamepadButtonBinding(0, GamepadButton.PS_Circle),
        }
    };

    public void Bind(IInput input)
    {
        foreach (var binding in MoveLeft.Bindings)
        {
            binding.Bind(input);
        }
    }
}
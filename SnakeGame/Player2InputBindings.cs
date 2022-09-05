using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public sealed class Player2InputBindings : InputBindings
{
    public override IReadOnlyDictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; } =
        new Dictionary<KeyboardKey, string>
        {
            { KeyboardKey.UpArrow,          InputActions.MoveUpAction },
            { KeyboardKey.LeftArrow,          InputActions.MoveLeftAction },
            { KeyboardKey.RightArrow,          InputActions.MoveRightAction },
            { KeyboardKey.DownArrow,          InputActions.MoveDownAction },
        };

    public override IReadOnlyDictionary<MouseButton, string> DefaultMouseButtonBindings { get; } = 
        new Dictionary<MouseButton, string>();
    
    public override bool TryResolveBinding(IGenericGamepad gamepad, InputButton button, out string? action)
    {
        action = null;
        return false;
    }
}
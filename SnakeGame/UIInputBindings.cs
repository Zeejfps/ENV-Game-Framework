using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public class UIInputBindings : IInputBindings
{
    public Dictionary<KeyboardKey, string> KeyboardKeyActionBindings { get; } = new()
    {
        {KeyboardKey.P, "Game/Pause"}
    };
}
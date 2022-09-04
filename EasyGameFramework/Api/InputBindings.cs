using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public abstract class InputBindings : IInputBindings
{
    public abstract Dictionary<KeyboardKey, string> KeyboardKeyToActionBindings { get; }
    public abstract Dictionary<MouseButton, string> MouseButtonToActionBindings { get; }

    public Task LoadFromFileAsync(string pathToFile)
    {
        return Task.CompletedTask;
    }
}
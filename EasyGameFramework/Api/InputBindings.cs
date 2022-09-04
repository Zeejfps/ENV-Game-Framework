using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public abstract class InputBindings : IInputBindings
{
    protected abstract Dictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; }
    protected abstract Dictionary<MouseButton, string> DefaultMouseButtonBindings { get; }
    
    public bool TryGetAction(KeyboardKey key, out string? action)
    {
        return DefaultKeyboardKeyBindings.TryGetValue(key, out action);
    }

    public bool TryGetAction(MouseButton button, out string? action)
    {
        return DefaultMouseButtonBindings.TryGetValue(button, out action);
    }

    public void LoadDefaults()
    {
        
    }
    
    public Task LoadFromFileAsync(string pathToFile)
    {
        return Task.CompletedTask;
    }
}
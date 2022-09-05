using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public abstract class InputBindings : IInputBindings
{
    protected abstract Dictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; }
    protected abstract Dictionary<MouseButton, string> DefaultMouseButtonBindings { get; }
    
    private Dictionary<KeyboardKey, string>? ActiveKeyboardKeyBindings { get; set; }
    private Dictionary<MouseButton, string>? ActiveMouseButtonBindings { get; set; }

    protected ILogger Logger { get; }
    
    protected InputBindings(ILogger logger)
    {
        Logger = logger;
    }

    public IEnumerable<KeyValuePair<KeyboardKey, string>> KeyboardBindings => ActiveKeyboardKeyBindings;
    public IEnumerable<KeyValuePair<MouseButton, string>> MouseBindings => ActiveMouseButtonBindings;

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
        ActiveKeyboardKeyBindings = new Dictionary<KeyboardKey, string>(DefaultKeyboardKeyBindings);
        ActiveMouseButtonBindings = new Dictionary<MouseButton, string>(DefaultMouseButtonBindings);
    }

    public void BindKeyboardKey(KeyboardKey key, string action)
    {
        ActiveKeyboardKeyBindings[key] = action;
    }

    public void BindMouseButton(MouseButton button, string action)
    {
        ActiveMouseButtonBindings[button] = action;
    }
}
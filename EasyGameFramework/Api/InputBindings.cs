using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public abstract class InputBindings : IInputBindings
{
    public abstract IReadOnlyDictionary<KeyboardKey, string> DefaultKeyboardKeyBindings { get; }
    public abstract IReadOnlyDictionary<MouseButton, string> DefaultMouseButtonBindings { get; }
    
    public IDictionary<KeyboardKey, string>? ActiveKeyboardKeyBindings { get; set; }
    public IDictionary<MouseButton, string>? ActiveMouseButtonBindings { get; set; }

    protected ILogger Logger { get; }
    
    protected InputBindings(ILogger logger)
    {
        Logger = logger;
    }
    
    public void LoadDefaults()
    {
        ActiveKeyboardKeyBindings = new Dictionary<KeyboardKey, string>(DefaultKeyboardKeyBindings);
        ActiveMouseButtonBindings = new Dictionary<MouseButton, string>(DefaultMouseButtonBindings);
    }
}
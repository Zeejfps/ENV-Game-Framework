using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using IniParser.Model;
using IniParser.Parser;

namespace EasyGameFramework.Core;

public class IniPlayerPrefs : IPlayerPrefs
{
    private string pathToFile = "C:/Users/zvasi/Documents/Dev/ENV Game Framework/SnakeGame/Assets/test.ini";

    private IContainer Container { get; }
    private ILogger Logger { get; }
    
    public IniPlayerPrefs(IContainer container, ILogger logger)
    {
        Container = container;
        Logger = logger;
    }
    
    public async Task<T> LoadInputBindingsAsync<T>(CancellationToken cancellationToken = default) where T : IInputBindings
    {
        var bindings = Container.New<T>();
        
        if (!File.Exists(pathToFile))
            return bindings;
        
        var keyBindingsIniGroupName = typeof(T).Name + ".Keyboard";
        var mouseBindingInitGroupName = typeof(T).Name + ".Mouse";
        
        var text = await File.ReadAllTextAsync(pathToFile, cancellationToken);
        var parser = new IniDataParser();
        var data = parser.Parse(text);
        
        // var keyboardKeyMappings = data[keyBindingsIniGroupName];
        // foreach (var mapping in keyboardKeyMappings)
        // {
        //     if (!int.TryParse(mapping.Value, out var value))
        //     {
        //         Logger.Warn($"Could not parse key value: {mapping.Value}");
        //         continue;
        //     }
        //     var key = (KeyboardKey)value;
        //     var action = mapping.KeyName;
        //     bindings.OverrideKeyboardKeyBindings[key] = action;
        // }
        //
        // var mouseButtonMappings = data[mouseBindingInitGroupName];
        // foreach (var mapping in mouseButtonMappings)
        // {
        //     if (!int.TryParse(mapping.Value, out var value))
        //     {
        //         Logger.Warn($"Could not parse key value: {mapping.Value}");
        //         continue;
        //     }
        //
        //     var button = new MouseButton(value);
        //     var action = mapping.KeyName;
        //     bindings.OverrideMouseButtonBindings[button] = action;
        // }

        return bindings;
    }

    public async Task SaveInputBindingsAsync(IInputBindings inputBindings, CancellationToken cancellationToke = default)
    {
        IniData data;
        if (File.Exists(pathToFile))
        {
            var text = await File.ReadAllTextAsync(pathToFile, cancellationToke);
            var parser = new IniDataParser();
            data = parser.Parse(text);
        }
        else
        {
            data = new IniData();
        }
        
        var keyBindingsIniGroupName = inputBindings.GetType().Name + ".Keyboard";
        var mouseBindingsIniGroupName = inputBindings.GetType().Name + ".Mouse";
        
        // foreach (var (key, action) in inputBindings.OverrideKeyboardKeyBindings)
        //     data[keyBindingsIniGroupName][action] = ((int)key).ToString();
        //
        // foreach (var (button, action) in inputBindings.OverrideMouseButtonBindings)
        //     data[mouseBindingsIniGroupName][action] = button.ToString();

        await File.WriteAllTextAsync(pathToFile, data.ToString(), cancellationToke);
    }
}
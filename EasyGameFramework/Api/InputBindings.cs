﻿using EasyGameFramework.Api.InputDevices;
using IniParser.Model;
using IniParser.Parser;

namespace EasyGameFramework.Api;

public abstract class InputBindings : IInputBindings
{
    private const string KeyBindingsIniGroupName = "Bindings.Keyboard";
    private const string MouseButtonBindingsIniGroupName = "Bindings.Mouse";
    
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
    
    public async Task LoadFromFileAsync(string pathToFile, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pathToFile))
            return;
        
        var text = await File.ReadAllTextAsync(pathToFile, cancellationToken);
        var parser = new IniDataParser();
        var data = parser.Parse(text);
        var mappings = data[KeyBindingsIniGroupName];
        
    }

    public async Task SaveToFileAsync(string pathToFile)
    {
        IniData data;
        if (File.Exists(pathToFile))
        {
            var text = await File.ReadAllTextAsync(pathToFile);
            var parser = new IniDataParser();
            data = parser.Parse(text);
        }
        else
        {
            data = new IniData();
        }
        
        foreach (var (key, action) in DefaultKeyboardKeyBindings)
            data[KeyBindingsIniGroupName][action] = ((int)key).ToString();
        
        foreach (var (button, action) in DefaultMouseButtonBindings)
            data[MouseButtonBindingsIniGroupName][action] = button.ToString();

        await File.WriteAllTextAsync(pathToFile, data.ToString());
    }
}
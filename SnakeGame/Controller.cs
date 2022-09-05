using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace SampleGames;

public abstract class Controller
{
    protected abstract IInputBindings Bindings { get; }
    
    private Dictionary<string, HashSet<Action>> ActionToHandlerMap { get; } = new();

    protected void BindAction(string action, Action handler)
    {
        if (!ActionToHandlerMap.TryGetValue(action, out var handlers))
        {
            handlers = new HashSet<Action>();
            ActionToHandlerMap[action] = handlers;
        }
        handlers.Add(handler);
    }

    protected void UnbindAction(string action, Action handler)
    {
        if (ActionToHandlerMap.TryGetValue(action, out var handlers))
            handlers.Remove(handler);
    }
    
    public void Bind(IInput input)
    {
        input.Mouse.ButtonPressed += OnMouseButtonPressed;
        input.Keyboard.KeyPressed += OnKeyboardKeyPressed;
        input.GamepadButtonPressed += OnGamepadButtonPressed;
    }

    public void Unbind(IInput input)
    {
        input.Mouse.ButtonPressed -= OnMouseButtonPressed;
        input.Keyboard.KeyPressed -= OnKeyboardKeyPressed;
        input.GamepadButtonPressed -= OnGamepadButtonPressed;
    }

    private void OnGamepadButtonPressed(GamepadButtonStateChangedEvent evt)
    {
        if (Bindings == null)
            return;
        
        if (Bindings.TryResolveBinding(evt.Gamepad, evt.Button, out var action))
        {
            OnActionPerformed(action!);
        }
    }

    private void OnMouseButtonPressed(in MouseButtonPressedEvent evt)
    {
        if (Bindings == null)
            return;
        
        var button = evt.Button;
        if (Bindings.OverrideMouseButtonBindings.TryGetValue(button, out var action))
            OnActionPerformed(action!);
        else if (Bindings.DefaultMouseButtonBindings.TryGetValue(button, out action))
            OnActionPerformed(action!);
    }

    private void OnKeyboardKeyPressed(in KeyboardKeyPressedEvent evt)
    {
        if (Bindings == null)
            return;
        
        var key = evt.Key;
        if (Bindings.OverrideKeyboardKeyBindings.TryGetValue(key, out var action))
            OnActionPerformed(action!);
        else if (Bindings.DefaultKeyboardKeyBindings.TryGetValue(key, out action))
            OnActionPerformed(action!);
    }

    private void OnActionPerformed(string action)
    {
        if (!ActionToHandlerMap.TryGetValue(action, out var handlers))
            return;
        
        foreach (var handler in handlers)
            handler.Invoke();
    }
}
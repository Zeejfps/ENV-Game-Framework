using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using ZGF.KeyboardModule;

namespace EasyGameFramework.Api;

public sealed class KeyboardKeyBinding : IButtonBinding
{
    private KeyboardKey Key { get; }

    public KeyboardKeyBinding(KeyboardKey key)
    {
        Key = key;
    }

    public event Action? Pressed;
    public event Action? Released;

    public void Bind(IInputSystem input)
    {
        input.Keyboard.KeyPressed += OnKeyPressed;
    }

    public void Unbind(IInputSystem input)
    {
        
    }

    private void OnKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        if (evt.Key == Key)
        {
            Pressed?.Invoke();
        }
    }
}
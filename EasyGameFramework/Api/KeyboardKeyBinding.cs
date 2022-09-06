using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

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

    public void Bind(IInput input)
    {
        input.Keyboard.KeyPressed += OnKeyPressed;
    }

    public void Unbind(IInput input)
    {
        
    }

    private void OnKeyPressed(in KeyboardKeyPressedEvent evt)
    {
        if (evt.Key == Key)
        {
            Pressed?.Invoke();
        }
    }
}
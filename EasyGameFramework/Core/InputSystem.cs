using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class InputSystem : IInputSystem
{
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }
    public IGamepadManager GamepadManager { get; }

    private ILogger Logger { get; }
    private IEventBus EventBus { get; }
    
    public InputSystem(ILogger logger, IEventBus eventBus, IMouse mouse, IKeyboard keyboard, IGamepadManager gamepadManager)
    {
        Logger = logger;
        EventBus = eventBus;
        Mouse = mouse;
        Keyboard = keyboard;
        GamepadManager = gamepadManager;
        
        // TODO: Maybe only subscribe when we have bindings?
        Mouse.ButtonPressed += OnMouseButtonPressed;
        Keyboard.KeyPressed += OnKeyboardKeyPressed;
    }

    private void OnMouseButtonPressed(in MouseButtonStateChangedEvent evt)
    {
        EventBus.Publish(evt);
    }

    private void OnKeyboardKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        EventBus.Publish(evt);
    }
}
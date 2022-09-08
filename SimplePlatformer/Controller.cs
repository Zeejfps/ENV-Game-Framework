using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public sealed partial class Controller
{
    private Dictionary<KeyboardKey, Action> KeyboardKeyToActionBindings { get; } = new();
    private Dictionary<GamepadButton, Action> GamepadButtonToActionBinding { get; } = new();
    private IGamepad? Gamepad { get; set; }
    private int Slot { get; set; }

    private IInputSystem InputSystem { get; }

    public Controller(IInputSystem inputSystem)
    {
        InputSystem = inputSystem;
    }

    public ButtonBindingBuilder Bind(Action action)
    {
        return new ButtonBindingBuilder(this, action);
    }
    
    public AxisBindingBuilder Bind(Action<float> action)
    {
        return new AxisBindingBuilder();
    }

    public void Attach(int slotIndex)
    {
        Slot = slotIndex;
        InputSystem.GamepadManager.GamepadConnected += GamepadManager_OnGamepadConnected;
        InputSystem.GamepadManager.GamepadDisconnected += GamepadManager_OnGamepadDisconnected;
        InputSystem.Keyboard.KeyPressed += Keyboard_OnKeyPressed;
        if (InputSystem.GamepadManager.TryGetGamepadInSlot(slotIndex, out var gamepad))
        {
            Gamepad = gamepad!;
            Gamepad.ButtonPressed += Gamepad_OnButtonPressed;
        }
    }

    public void Detach()
    {
        InputSystem.Keyboard.KeyPressed -= Keyboard_OnKeyPressed;
        InputSystem.GamepadManager.GamepadConnected -= GamepadManager_OnGamepadConnected;
        InputSystem.GamepadManager.GamepadDisconnected -= GamepadManager_OnGamepadDisconnected;
    }

    private void GamepadManager_OnGamepadConnected(GamepadConnectedEvent evt)
    {
        if (Gamepad == null && evt.Slot == Slot)
        {
            Gamepad = evt.Gamepad;
            Gamepad.ButtonPressed += Gamepad_OnButtonPressed;
        }
    }

    private void GamepadManager_OnGamepadDisconnected(GamepadDisconnectedEvent evt)
    {
        if (evt.Gamepad == Gamepad)
        {
            Gamepad.ButtonPressed -= Gamepad_OnButtonPressed;
            Gamepad = null;
        }
    }

    private void Keyboard_OnKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        if(KeyboardKeyToActionBindings.TryGetValue(evt.Key, out var action))
            action.Invoke();
    }

    private void Gamepad_OnButtonPressed(GamepadButtonStateChangedEvent evt)
    {
        if (GamepadButtonToActionBinding.TryGetValue(evt.Button, out var action))
            action.Invoke();
    }

    public class ButtonBindingBuilder
    {
        private Controller Bindings { get; }
        private Action Action { get; }

        public ButtonBindingBuilder(Controller bindings, Action action)
        {
            Bindings = bindings;
            Action = action;
        }

        public ButtonBindingBuilder To(KeyboardKey key)
        {
            Bindings.KeyboardKeyToActionBindings[key] = Action;
            return this;
        }
        
        public ButtonBindingBuilder To(MouseButton button)
        {
            return this;
        }
        
        public ButtonBindingBuilder To(GamepadButton button)
        {
            Bindings.GamepadButtonToActionBinding[button] = Action;
            return this;
        }
    }

    public class AxisBindingBuilder
    {
        public AxisBindingBuilder To(KeyboardKey key)
        {
            return this;
        }

        public AxisBindingBuilder To(GamepadAxis axis)
        {
            return this;
        }
    }
}
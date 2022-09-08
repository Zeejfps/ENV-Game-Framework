using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public interface IActionBinding
{
    void ExecuteAction();
}

public sealed partial class Controller
{
    private Dictionary<KeyboardKey, Action> KeyboardKeyToActionBindings { get; } = new();
    private Dictionary<GamepadButton, Action> GamepadButtonToActionBinding { get; } = new();
    
    private Dictionary<KeyboardKey, Action> KeyboardKeyToFloatActionBindings { get; } = new();
    private Dictionary<GamepadAxis, Action<float>> GamepadAxisToActionBindings { get; } = new();

    private IGamepad? Gamepad { get; set; }
    private int Slot { get; set; }

    private IInputSystem InputSystem { get; }
    private IClock Clock { get; }

    public Controller(IInputSystem inputSystem, IClock clock)
    {
        InputSystem = inputSystem;
        Clock = clock;
    }

    public ButtonBindingBuilder Bind(Action action)
    {
        return new ButtonBindingBuilder(this, action);
    }
    
    public AxisBindingBuilder Bind(Action<float> action)
    {
        return new AxisBindingBuilder(this, action);
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
        
        Clock.Ticked += Update;
    }

    public void Detach()
    {
        Clock.Ticked -= Update;
        InputSystem.Keyboard.KeyPressed -= Keyboard_OnKeyPressed;
        InputSystem.GamepadManager.GamepadConnected -= GamepadManager_OnGamepadConnected;
        InputSystem.GamepadManager.GamepadDisconnected -= GamepadManager_OnGamepadDisconnected;
    }

    private void Update()
    {
        var keyboard = InputSystem.Keyboard;
        var gamepad = Gamepad;
        
        foreach (var (key, action) in KeyboardKeyToFloatActionBindings)
        {
            if (keyboard.IsKeyPressed(key))
                action.Invoke();
        }

        if (gamepad != null)
        {
            foreach (var (axis, action) in GamepadAxisToActionBindings)
            {
                var axisValue = gamepad.GetAxisValue(axis);
                action.Invoke(axisValue);
            }
        }
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
        private Controller Controller { get; }
        private Action Action { get; }

        public ButtonBindingBuilder(Controller controller, Action action)
        {
            Controller = controller;
            Action = action;
        }

        public ButtonBindingBuilder To(KeyboardKey key)
        {
            Controller.KeyboardKeyToActionBindings[key] = Action;
            return this;
        }
        
        public ButtonBindingBuilder To(MouseButton button)
        {
            return this;
        }
        
        public ButtonBindingBuilder To(GamepadButton button)
        {
            Controller.GamepadButtonToActionBinding[button] = Action;
            return this;
        }
    }

    public class AxisBindingBuilder
    {
        private Controller Controller { get; }
        private Action<float> Action { get; }

        public AxisBindingBuilder(Controller controller, Action<float> action)
        {
            Controller = controller;
            Action = action;
        }

        public AxisBindingBuilder To(KeyboardKey key, float value)
        {
            Controller.KeyboardKeyToFloatActionBindings[key] = () => Action.Invoke(value);
            return this;
        }

        public AxisBindingBuilder To(GamepadAxis axis, float deadZoneRadius = 0.01f)
        {
            Controller.GamepadAxisToActionBindings[axis] = axisValue =>
            {
                if (axisValue <= deadZoneRadius)
                    axisValue = 0f;
                Action.Invoke(axisValue);
            };
            return this;
        }
    }
}
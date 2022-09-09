using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public sealed class Controller
{
    private Dictionary<ButtonInput, HashSet<IButtonInputBinding>> ButtonInputBindings { get; } = new();
    private Dictionary<AxisInput, HashSet<IAxisInputBinding>> AxisInputBindings { get; } = new();

    public IMouse? Mouse { get; private set; }
    public IKeyboard? Keyboard { get; private set; }
    public IGamepad? Gamepad { get; private set; }
    
    private int Slot { get; set; }
    private IInputSystem InputSystem { get; }
    private IClock Clock { get; }

    public Controller(IInputSystem inputSystem, IClock clock)
    {
        InputSystem = inputSystem;
        Clock = clock;
    }

    public ButtonInputBindingBuilder Bind(ButtonInput input)
    {
        return new ButtonInputBindingBuilder(this, input);
    }
    
    public AxisBindingBuilder Bind(AxisInput input)
    {
        return new AxisBindingBuilder(this, input);
    }

    public void Attach(int slotIndex)
    {
        Keyboard = InputSystem.Keyboard;
        Mouse = InputSystem.Mouse;
        Slot = slotIndex;
        InputSystem.GamepadManager.GamepadConnected += GamepadManager_OnGamepadConnected;
        InputSystem.GamepadManager.GamepadDisconnected += GamepadManager_OnGamepadDisconnected;
        if (InputSystem.GamepadManager.TryGetGamepadInSlot(slotIndex, out var gamepad))
            Gamepad = gamepad!;

        Clock.Ticked += Update;
    }

    public void Detach()
    {
        Keyboard = null;
        Mouse = null;
        Gamepad = null;
        Clock.Ticked -= Update;
        InputSystem.GamepadManager.GamepadConnected -= GamepadManager_OnGamepadConnected;
        InputSystem.GamepadManager.GamepadDisconnected -= GamepadManager_OnGamepadDisconnected;
    }

    private void Update()
    {
        var mouse = InputSystem.Mouse;
        var keyboard = InputSystem.Keyboard;
        var gamepad = Gamepad;
        
        foreach (var (input, bindings) in ButtonInputBindings)
        {
            foreach (var binding in bindings)
            {
                input.IsPressed |= binding.Poll(this);
            }
        }

        foreach (var (input, bindings) in AxisInputBindings)
        {
            var value = 0f;
            foreach (var binding in bindings)
            {
                value += binding.Poll(keyboard, mouse, gamepad);
            }
            
            if (value < -1)
                value = -1f;
            else if (value > 1f)
                value = 1f;

            input.Value = value;
        }
    }

    private void GamepadManager_OnGamepadConnected(GamepadConnectedEvent evt)
    {
        if (Gamepad == null && evt.Slot == Slot)
            Gamepad = evt.Gamepad;
    }

    private void GamepadManager_OnGamepadDisconnected(GamepadDisconnectedEvent evt)
    {
        if (evt.Gamepad == Gamepad)
            Gamepad = null;
    }

    public class ButtonInputBindingBuilder
    {
        private Controller Controller { get; }
        private HashSet<IButtonInputBinding> Bindings { get; }

        public ButtonInputBindingBuilder(Controller controller, ButtonInput buttonInput)
        {
            Controller = controller;
            if (!Controller.ButtonInputBindings.TryGetValue(buttonInput, out var bindings))
            {
                bindings = new HashSet<IButtonInputBinding>();
                Controller.ButtonInputBindings[buttonInput] = bindings;
            }

            Bindings = bindings;
        }

        public ButtonInputBindingBuilder To(KeyboardKey key)
        {
            Bindings.Add(new KeyboardKeyToButtonInputBinding(key));
            return this;
        }
        
        public ButtonInputBindingBuilder To(MouseButton button)
        {
            return this;
        }
        
        public ButtonInputBindingBuilder To(GamepadButton button)
        {
            Bindings.Add(new GamepadButtonToButtonInputBinding(button));
            return this;
        }
    }

    public class AxisBindingBuilder
    {
        private Controller Controller { get; }
        private HashSet<IAxisInputBinding> Bindings { get; }

        public AxisBindingBuilder(Controller controller, AxisInput axisInput)
        {
            Controller = controller;

            if (!controller.AxisInputBindings.TryGetValue(axisInput, out var bindings))
            {
                bindings = new HashSet<IAxisInputBinding>();
                controller.AxisInputBindings[axisInput] = bindings;
            }

            Bindings = bindings;
        }

        public AxisBindingBuilder To(KeyboardKey key, float value)
        {
            Bindings.Add(new KeyboardKeyToAxisInputBinding(key, value));
            return this;
        }

        public AxisBindingBuilder To(GamepadAxis axis, float deadZoneRadius = 0.01f)
        {
            Bindings.Add(new GamepadAxisToAxisInputBinding(axis, deadZoneRadius));
            return this;
        }
    }
}
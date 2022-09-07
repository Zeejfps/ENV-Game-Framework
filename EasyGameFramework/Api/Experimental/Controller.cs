namespace EasyGameFramework.Api;

public abstract class Controller
{
    protected abstract IInputBindings Bindings { get; }

    protected IInputSystem InputSystem { get; }
    
    public Controller(IInputSystem inputSystem)
    {
        InputSystem = inputSystem;
    }
    
    protected void BindOnPressed(InputAction action, Action handler)
    {
        foreach (var binding in action.ButtonBindings)
            binding.Pressed += handler;
    }

    public void Enable()
    {
        foreach (var inputAction in Bindings.InputActions)
        {
            foreach (var buttonBinding in inputAction.ButtonBindings)
            {
                buttonBinding.Bind(InputSystem);
            }
        }

        OnEnable();
    }

    public void Disable()
    {
        
    }

    protected abstract void OnEnable();
}
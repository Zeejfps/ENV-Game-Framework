namespace EasyGameFramework.Api;

public abstract class Controller
{
    protected abstract IInputBindings Bindings { get; }
    
    protected void BindOnPressed(InputAction action, Action handler)
    {
        foreach (var binding in action.ButtonBindings)
            binding.Pressed += handler;
    }

    public void Bind(IInput input)
    {
        foreach (var inputAction in Bindings.InputActions)
        {
            foreach (var buttonBinding in inputAction.ButtonBindings)
            {
                buttonBinding.Bind(input);
            }
        }
    }
}
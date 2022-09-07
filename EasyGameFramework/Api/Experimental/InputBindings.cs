namespace EasyGameFramework.Api;

public abstract class InputBindings : IInputBindings
{
    public abstract IEnumerable<InputAction> InputActions { get; }
}
namespace EasyGameFramework.Api.InputDevices;

public interface IMouseBindings
{
    void BindButtonToAction(MouseButton button, string action);
    void UnbindButton(MouseButton button);
    bool TryGetAction(MouseButton button, out string? action);
}
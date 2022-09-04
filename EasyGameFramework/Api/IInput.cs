using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInput
{
    public IMouse Mouse { get; }
    public IKeyboard Keyboard { get; }

    void Update();
    
    void BindAction(string actionName, Action handler);
    void UnbindAction(string actionName, Action handler);

    void BindAxis(string axisName, Action<float> handler);
    void UnbindAxis(string axisName, Action<float> handler);
    
    void ApplyBindings(IInputBindings inputBindings);
}
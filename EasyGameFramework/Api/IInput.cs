using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Api;

public interface IInput
{
    IMouse Mouse { get; }
    IKeyboard Keyboard { get; }
    IInputBindings? Bindings { get; set; }
    
    void Update();
    
    void BindAction(string actionName, Action handler);
    void UnbindAction(string actionName, Action handler);

    void BindAxis(string axisName, Action<float> handler);
    void UnbindAxis(string axisName, Action<float> handler);
}
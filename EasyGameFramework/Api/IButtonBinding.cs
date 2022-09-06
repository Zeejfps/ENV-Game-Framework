namespace EasyGameFramework.Api;

public interface IButtonBinding
{
    event Action Pressed;
    event Action Released;
    
    void Bind(IInputSystem input);
    void Unbind(IInputSystem input);
}
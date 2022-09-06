namespace EasyGameFramework.Api;

public interface IButtonBinding
{
    event Action Pressed;
    event Action Released;
    
    void Bind(IInput input);
    void Unbind(IInput input);
}
namespace EasyGameFramework.Api;

public interface IInputLayer
{
    void Bind(IInput input);
    void Unbind(IInput input);
}
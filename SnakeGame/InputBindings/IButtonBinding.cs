using EasyGameFramework.Api;

namespace SampleGames;

public interface IButtonBinding
{
    event Action Pressed;
    event Action Released;
    
    void Bind(IInput input);
    void Unbind();
}
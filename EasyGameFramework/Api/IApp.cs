namespace EasyGameFramework.Api;

public interface IApp
{
    bool IsRunning { get; }

    void Setup();
    void Update();
    void Quit();
}
namespace EasyGameFramework.Api;

public interface IApp
{
    bool IsRunning { get; }

    void Start();
    void Update();
    void Stop();
}
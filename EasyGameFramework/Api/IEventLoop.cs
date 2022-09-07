namespace EasyGameFramework.Api;

public interface IEventLoop
{
    event Action OnEarlyUpdate;
    event Action OnUpdate;
    event Action OnLateUpdate;
    
    bool IsRunning { get; }
    
    void Start();
    void Stop();
}
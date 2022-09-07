namespace EasyGameFramework.Api;

public interface IEventLoop
{
    event Action OnStart;
    event Action OnEarlyUpdate;
    event Action OnUpdate;
    event Action OnLateUpdate;
    event Action OnStop;
    
    bool IsRunning { get; }
    
    void Start();
    void Stop();
}
using EasyGameFramework.Api;

namespace SimplePlatformer;

public class Player
{
    private ILogger Logger { get; }
    
    public Player(ILogger logger)
    {
        Logger = logger;
    }
    
    public void Jump()
    {
        Logger.Trace("Jump!");
    }

    public void Move(float axisValue)
    {
        Logger.Trace($"Moving {axisValue}");   
    }
}
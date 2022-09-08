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

    public void MoveLeft(float multiplier)
    {
        
    }
}
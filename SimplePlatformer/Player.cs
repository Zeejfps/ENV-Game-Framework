using EasyGameFramework.Api;

namespace SimplePlatformer;

public class Player
{
    public ButtonInput JumpInput { get; }
    public AxisInput MovementInput { get; }
    
    private ILogger Logger { get; }
    
    public Player(ILogger logger)
    {
        Logger = logger;

        JumpInput = new ButtonInput();
        MovementInput = new AxisInput();

        JumpInput.Pressed += Jump;
        MovementInput.ValueChanged += Move;
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
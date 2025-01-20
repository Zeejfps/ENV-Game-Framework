namespace Bricks;

public sealed class BrickzGame
{
    private IInput Input { get; }
    
    public Paddle Paddle { get; }

    public BrickzGame(IInput input)
    {
        Input = input;
    }

    public void Update()
    {
        
    }
}
using Bricks.Entities;

namespace Bricks;

public sealed class BrickzGame
{
    private IInput Input { get; }
    
    public PaddleEntity Paddle { get; }

    public BrickzGame(IInput input)
    {
        Input = input;
    }

    public void Update()
    {
        
    }
}
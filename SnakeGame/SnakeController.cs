using EasyGameFramework.Api;

namespace SampleGames;

public class SnakeController
{
    private Snake Snake { get; }

    public SnakeController(Snake snake)
    {
        Snake = snake;
    }

    public void Bind(IInput input)
    {
    }

    public void Unbind()
    {
        
    }
    
    private void IncreaseSpeed()
    {
        Snake.Speed += 0.5f;
    }

    private void DecreaseSpeed()
    {
        Snake.Speed -= 0.5f;
    }
}
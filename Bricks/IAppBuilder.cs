namespace Bricks;

public interface IApp : IDisposable
{
    bool IsCloseRequested { get; }
    IInput Input { get; }
    void Update();
    void Render(Paddle paddle, Ball ball);
}

public interface IAppBuilder
{
    void WithWindowName(string brickz);
    void WithCanvasSize(int width, int height);
    IApp Build();
}
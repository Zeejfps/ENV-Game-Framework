using Bricks.Entities;

namespace Bricks;

public interface IApp : IDisposable
{
    bool IsCloseRequested { get; }
    IInput Input { get; }
    void Update();
    void Render(PaddleEntity paddle, BallEntity ball, BricksRepo bricks);
}

public interface IAppBuilder
{
    void WithWindowName(string brickz);
    void WithCanvasSize(int width, int height);
    IApp Build();
}
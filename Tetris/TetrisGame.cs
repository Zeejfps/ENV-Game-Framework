using EasyGameFramework.Api;

namespace Tetris;

public sealed class TetrisGame : Game
{
    public TetrisGame(IContext context) : base(context)
    {
    }

    protected override void OnStartup()
    {
        Window.Title = "Tetris";
        Window.SetScreenSize(640, 480);
        Window.OpenCentered();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnShutdown()
    {
    }
}
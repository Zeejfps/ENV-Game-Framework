using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Experimental;

namespace DataOriented.Pong;

public sealed class PongGame : Game
{
    private IPixelCanvas Canvas2D { get; }
    private Vector2 MousePosition { get; set; }
    
    public PongGame(IContext context) : base(context)
    {
        Canvas2D = new PixelCanvas(Logger, Context.Window, 200, 200);
    }

    protected override void OnStartup()
    {
        var window = Window;
        window.Title = "Pong - Data Oriented";
        window.IsResizable = true;
        window.SetScreenSize(640, 640);
    }

    protected override void OnUpdate()
    {
        var mouse = Context.Window.Input.Mouse;
        var mouseScreenPosition = new Vector2(mouse.ScreenX, mouse.ScreenY);
        MousePosition = Canvas2D.ScreenToCanvasPoint(mouseScreenPosition);
    }

    protected override void OnRender()
    {
        var gpu = Gpu;
        gpu.FramebufferController.ClearColorBuffers(0, 0, 0, 0);
        
        Canvas2D.Clear();
        Canvas2D.DrawLine(0, 0, (int)MousePosition.X, (int)MousePosition.Y);
        Canvas2D.Render();
    }

    protected override void OnShutdown()
    {
    }
}
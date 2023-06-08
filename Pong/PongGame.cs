using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;

namespace Pong;

public sealed class PongGame : Game
{
    private IWindow Window { get; }
    private IGpu Gpu => Window.Gpu;
    private IInputSystem InputSystem => Window.Input;
    
    public PongGame(IWindow window, IEventLoop eventLoop, ILogger logger) : base(eventLoop, logger)
    {
        Window = window;
    }

    protected override void OnStart()
    {
        
    }

    protected override void OnUpdate()
    {
        if (InputSystem.Keyboard.IsKeyPressed(KeyboardKey.Escape))
            Window.Close();
    }

    protected override void OnRender()
    {
    }

    protected override void OnStop()
    {
    }
}
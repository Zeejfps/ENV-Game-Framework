using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;

namespace Core;

public class SnakeGame : Game
{
    private OrthographicCamera m_Camera;
    private SnakeRenderer m_SnakeRenderer;

    private IContext Context { get; }
    private IGpu Gpu { get; }
    private ILogger Logger { get; }
    private IAllocator Allocator { get; }
    private Snake Snake { get; }
    
    public SnakeGame(IContext context) : base(context.Window, context.Input)
    {
        Context = context;
        Gpu = context.Gpu;
        Logger = context.Logger;
        Allocator = context.Allocator;

        Snake = Allocator.New<Snake>();
        
        m_Camera = new OrthographicCamera(40, 40, 0.1f, 10)
        {
            Transform =
            {
                WorldPosition = new Vector3(0f, 0f, -5f),
            }
        };
        m_SnakeRenderer = new SnakeRenderer(Gpu);
    }

    protected override void OnStart()
    {
        var window = Window;
        window.Width = 500;
        window.Height = 500;
        window.IsVsyncEnabled = true;
        window.IsResizable = false;
        window.Title = "SNAEK";
        window.ShowCentered();

        m_SnakeRenderer.LoadResources();
        Snake.Reset();
    }

    protected override void OnUpdate(float dt)
    {
        var keyboard = Input.Keyboard;
        
        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Escape))
        {
            Quit();
            return;
        }
        
        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.A))
        {
            Snake.TurnWest();
        }
        else if (keyboard.WasKeyPressedThisFrame(KeyboardKey.D)) 
        {
            Snake.TurnEast();
        }
        else if (keyboard.WasKeyPressedThisFrame(KeyboardKey.W)) 
        {
            Snake.TurnNorth();
        }
        else if (keyboard.WasKeyPressedThisFrame(KeyboardKey.S)) 
        {
            Snake.TurnSouth();
        }

        if (keyboard.WasAnyKeyPressedThisFrame(out var key))
        {
            //Logger.Trace(key);
        }

        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Space))
        {
            Snake.Reset();
        }

        Snake.Update(dt);
    }

    protected override void OnRender(float dt)
    {
        var gpu = Gpu;
        gpu.SaveState();
        gpu.EnableBackfaceCulling = false;

        var renderbuffer = Gpu.Renderbuffer;
        renderbuffer.BindWindow();
        renderbuffer.ClearColorBuffers(0f, 0.3f, 0f, 1f);
        
        m_SnakeRenderer.Render(Snake.Segments, m_Camera);
        
        gpu.RestoreState();
    }

    protected override void OnQuit()
    {
    }
}
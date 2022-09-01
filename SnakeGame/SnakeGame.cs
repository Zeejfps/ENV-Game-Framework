using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;

namespace SampleGames;

public class SnakeGame : Game
{
    private OrthographicCamera m_Camera;
    private SnakeRenderer m_SnakeRenderer;

    private IContext Context { get; }
    private IGpu Gpu { get; }
    private ILogger Logger { get; }
    private IContainer Container { get; }
    private Snake Snake { get; }
    private Grid Grid { get; }
    
    public SnakeGame(IContext context) : base(context.Window, context.Input)
    {
        Context = context;
        Gpu = context.Gpu;
        Logger = context.Logger;
        Container = context.Container;

        Grid = new Grid(20, 20);
        Container.Register<Grid>(() => Grid);

        Snake = Container.New<Snake>();
        
        m_Camera = OrthographicCamera.FromLRBT(0, Grid.Width, 0, Grid.Height, 0.1f, 10f);
        m_Camera.Transform.WorldPosition = new Vector3(0f, 0f, -5f);
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

    protected override void OnUpdate()
    {
        var keyboard = Input.Keyboard;
        
        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Escape))
        {
            Stop();
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

        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.Space))
        {
            Snake.Reset();
        }

        Snake.Update(Clock.UpdateDeltaTime);
    }

    protected override void OnRender()
    {
        var gpu = Gpu;
        gpu.SaveState();
        gpu.EnableBackfaceCulling = false;
        gpu.EnableBlending = true;

        var renderbuffer = Gpu.Renderbuffer;
        renderbuffer.BindWindow();
        renderbuffer.ClearColorBuffers(0f, 0.3f, 0f, 1f);

        var cellWidth = renderbuffer.Width / (float)(Grid.Width);
        var cellHeight = renderbuffer.Height / (float)(Grid.Height);
        
        gpu.Shader.Load("Assets/grid");
        gpu.Shader.SetVector2("u_Pitch", new Vector2(cellWidth, cellHeight));
        gpu.Mesh.Load("Assets/quad");
        gpu.Mesh.Render();
        
        m_SnakeRenderer.Render(Snake, m_Camera);
        
        gpu.RestoreState();
    }

    protected override void OnStop()
    {
    }
}
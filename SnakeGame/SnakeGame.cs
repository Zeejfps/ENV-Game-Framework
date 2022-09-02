using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;

namespace SampleGames;

public class SnakeGame : Game
{
    private OrthographicCamera m_Camera;
    private SpriteRenderer m_SpriteRenderer;

    private IContext Context { get; }
    private IGpu Gpu { get; }
    private ILogger Logger { get; }
    private IContainer Container { get; }
    private Snake Snake { get; }
    private Grid Grid { get; }
    private Vector2 Apple { get; set; }
    private Random Random { get; } = new();
    
    public SnakeGame(IContext context) : base(context.Window, context.Input)
    {
        Context = context;
        Gpu = context.Gpu;
        Logger = context.Logger;
        Container = context.Container;

        Grid = new Grid(21, 21);
        Container.BindInstance(Grid);

        Snake = Container.New<Snake>();
        
        m_Camera = OrthographicCamera.FromLRBT(0, Grid.Width, 0, Grid.Height, 0.1f, 10f);
        m_Camera.Transform.WorldPosition = new Vector3(0f, 0f, -5f);
        m_SpriteRenderer = new SpriteRenderer(Gpu);
    }

    protected override void OnStart()
    {
        var window = Window;
        window.Width = 500;
        window.Height = 500;
        window.IsVsyncEnabled = true;
        window.IsResizable = false;
        window.Title = "SNAEK";
        window.CursorMode = CursorMode.HiddenAndLocked;
        window.ShowCentered();

        m_SpriteRenderer.LoadResources();
        Snake.Reset();

        Apple = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
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

        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.R))
        {
            Snake.Reset();
        }

        Snake.Update(Clock.UpdateDeltaTime);

        if (Snake.Head == Apple)
        {
            Apple = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
            Snake.Grow();
        }
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

        var cellWidth = renderbuffer.Width / Grid.Width;
        var cellHeight = renderbuffer.Height / Grid.Height;
        
        gpu.Shader.Load("Assets/grid");
        gpu.Shader.SetVector2("u_Pitch", new Vector2(cellWidth, cellHeight));
        gpu.Mesh.Load("Assets/quad");
        gpu.Mesh.Render();
        
        m_SpriteRenderer.StartBatch();

        m_SpriteRenderer.DrawSprite(Apple, new Vector3(1f, 0f, 0f));
        
        foreach (var segment in Snake.Segments)
        {
            var color = segment == Snake.Head
                ? new Vector3(0.1f, 1f, 0.1f)
                : new Vector3(1f, 0f, 1f);
            m_SpriteRenderer.DrawSprite(segment, color);
        }
        
        m_SpriteRenderer.RenderBatch(m_Camera);
        
        gpu.RestoreState();
    }

    protected override void OnStop()
    {
    }
}
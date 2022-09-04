using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.Events;
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
    
    private IEventBus EventBus { get; }
    
    public SnakeGame(IContext context, IEventBus eventBus) : base(context.Window, context.Input)
    {
        Context = context;
        Gpu = context.Gpu;
        Logger = context.Logger;
        Container = context.Container;
        EventBus = eventBus;

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

        Apple = SpawnApple();
        
        EventBus.AddListener<KeyboardKeyPressedEvent>(OnKeyboardKeyPressed);
    }

    protected override void OnUpdate()
    {
        Snake.Update(Clock.UpdateDeltaTime);

        if (Snake.Head == Apple)
        {
            Apple = SpawnApple();
            Snake.Grow();
        }

        if (Snake.IsSelfIntersecting)
        {
            Apple = SpawnApple();
            Snake.Reset();
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

    private void OnKeyboardKeyPressed(KeyboardKeyPressedEvent evt)
    {
        var key = evt.Key;
        
        if (key == KeyboardKey.Escape)
        {
            Stop();
            return;
        }
        
        if (key == KeyboardKey.A)
        {
            Snake.TurnWest();
        }
        else if (key == KeyboardKey.D) 
        {
            Snake.TurnEast();
        }
        else if (key == KeyboardKey.W) 
        {
            Snake.TurnNorth();
        }
        else if (key == KeyboardKey.S) 
        {
            Snake.TurnSouth();
        }

        if (key == KeyboardKey.R)
        {
            Snake.Reset();
            Apple = SpawnApple();
        }
        
        if (key == KeyboardKey.Space)
            Snake.Grow();

        if (key == KeyboardKey.Equals)
            Snake.Speed += 0.5f;
        else if (key == KeyboardKey.Minus)
            Snake.Speed -= 0.5f;
    }

    private Vector2 SpawnApple()
    {
        var position = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
        while (Snake.Segments.Contains(position))
            position = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
        return position;
    }
}
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;

namespace SampleGames;

public class SnakeGame : Game
{
    public bool IsPaused { get; set; }

    private OrthographicCamera m_Camera;
    private SpriteRenderer m_SpriteRenderer;

    private IContext Context { get; }
    private IGpu Gpu { get; }
    private ILogger Logger { get; }
    private IContainer Container { get; }
    private IPlayerPrefs PlayerPrefs { get; }
    private Snake[] Snakes { get; }
    private Grid Grid { get; }
    private Vector2 Apple { get; set; }
    private Random Random { get; } = new();
    private IEventBus EventBus { get; }
    private SnakeController Player1Controller { get; }
    private SnakeController Player2Controller { get; }
    private GameController GameController { get; }
    
    public SnakeGame(
        IContext context,
        IEventBus eventBus,
        IPlayerPrefs playerPrefs) : base(context.Window, context.Input)
    {
        Context = context;
        Gpu = context.Gpu;
        Logger = context.Logger;
        Container = context.Container;
        EventBus = eventBus;
        PlayerPrefs = playerPrefs;
        
        Grid = new Grid(21, 21);
        
        Snakes = new[]
        {
            new Snake(Grid, Logger),
            new Snake(Grid, Logger)
        };

        m_Camera = OrthographicCamera.FromLRBT(0, Grid.Width, 0, Grid.Height, 0.1f, 10f);
        m_Camera.Transform.WorldPosition = new Vector3(0f, 0f, -5f);
        m_SpriteRenderer = new SpriteRenderer(Gpu);
        
        Player1Controller = new SnakeController(0, Snakes[0]);
        Player2Controller = new SnakeController(1, Snakes[1]);
        GameController = new GameController(this);
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
        
        Player1Controller.Bind(Input);
        Player2Controller.Bind(Input);
        GameController.Bind(Input);

        ResetLevel();
    }

    protected override void OnUpdate()
    {
        if (IsPaused)
            return;

        foreach (var snake in Snakes)
        {
            snake.Update(Clock.UpdateDeltaTime);
            if (snake.Head == Apple)
            {
                Apple = SpawnApple();
                snake.Grow();
            }

            if (snake.IsSelfIntersecting)
            {
                snake.Reset();
            }
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
        
        m_SpriteRenderer.NewBatch();
        {
            m_SpriteRenderer.DrawSprite(Apple, new Vector3(1f, 0f, 0f));

            foreach (var snake in Snakes)
            {
                foreach (var segment in snake.Segments)
                {
                    var color = segment == snake.Head
                        ? new Vector3(0.1f, 1f, 0.1f)
                        : new Vector3(1f, 0f, 1f);
                    m_SpriteRenderer.DrawSprite(segment, color);
                }
            }
        }
        m_SpriteRenderer.RenderBatch(m_Camera);
        
        gpu.RestoreState();
    }

    protected override void OnStop()
    {
    }

    public void IncreaseSpeed()
    {
        foreach (var snake in Snakes)
        {
            snake.Speed += 0.5f;
        }
    }

    public void DecreaseSpeed()
    {
        foreach (var snake in Snakes)
        {
            snake.Speed -= 0.5f;
        }
    }

    public void ResetLevel()
    {
        foreach (var snake in Snakes)
        {
            snake.Reset();
        }
        Apple = SpawnApple();
    }

    private Vector2 SpawnApple()
    {
        var occupiedPositions = new HashSet<Vector2>();
        foreach (var snake in Snakes)
        {
            foreach (var segment in snake.Segments)
            {
                occupiedPositions.Add(segment);
            }
        }
     
        var position = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
        while (occupiedPositions.Contains(position))
            position = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
        return position;
    }
}
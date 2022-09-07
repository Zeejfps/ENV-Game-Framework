using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.Rendering;

namespace SampleGames;

public class SnakeGameApp : GameApp
{
    public bool IsPaused { get; set; }
    
    private IContext Context { get; }
    private IGpu Gpu { get; }
    private IInputSystem Input { get; }
    private IContainer Container { get; }
    private IPlayerPrefs PlayerPrefs { get; }
    private Snake[] Snakes { get; }
    private GridSize GridSize { get; }
    private Vector2 Apple { get; set; }
    private Random Random { get; } = new();
    private SnakeController Player1Controller { get; }
    private SnakeController Player2Controller { get; }
    private GameController GameController { get; }

    private float Speed { get; set; }
    private float m_AccumulatedTime;
    private readonly OrthographicCamera m_Camera;
    private readonly SpriteRenderer m_SpriteRenderer;

    public SnakeGameApp(
        IContext context,
        IPlayerPrefs playerPrefs,
        IInputSystem inputSystem,
        IEventLoop eventLoop) : base(context.Window, eventLoop, context.Logger)
    {
        Context = context;
        Gpu = context.Gpu;
        Input = inputSystem;
        Container = context.Container;
        PlayerPrefs = playerPrefs;
        
        GridSize = new GridSize(21, 21);
        
        Snakes = new[]
        {
            new Snake(Logger),
            new Snake(Logger)
        };

        m_Camera = OrthographicCamera.FromLRBT(0, GridSize.Width, 0, GridSize.Height, 0.1f, 10f);
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
        window.OpenCentered();

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

        m_AccumulatedTime += Clock.UpdateDeltaTime;
        if (m_AccumulatedTime >= 1f / Speed)
        {
            m_AccumulatedTime = 0f;
            
            foreach (var snake in Snakes)
            {
                snake.Move(GridSize);
                if (snake.Head == Apple)
                {
                    Apple = FindEmptyTile();
                    snake.Grow();
                }

                if (snake.IsSelfIntersecting)
                {
                    ResetLevel();
                    return;
                }

                foreach (var otherSnake in Snakes)
                {
                    if (snake == otherSnake)
                        continue;
                    
                    if (snake.IsCollidingWith(otherSnake.Segments))
                    {
                        ResetLevel();  
                        return;
                    }
                }
            }
        }
    }
    
    protected override void OnRender()
    {
        var gpu = Gpu;
        gpu.EnableBackfaceCulling = false;
        gpu.EnableBlending = true;

        var renderbuffer = Gpu.Renderbuffer;
        renderbuffer.BindWindow();
        renderbuffer.ClearColorBuffers(0f, 0.3f, 0f, 1f);

        var cellWidth = renderbuffer.Width / GridSize.Width;
        var cellHeight = renderbuffer.Height / GridSize.Height;
        
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
    }

    protected override void OnStop()
    {
    }

    public void IncreaseSpeed()
    {
        Speed += 0.5f;
    }

    public void DecreaseSpeed()
    {
        Speed -= 0.5f;
    }

    public void ResetLevel()
    {
        m_AccumulatedTime = 0f;
        Speed = 3f;
        foreach (var snake in Snakes)
        {
            var emptyTile = FindEmptyTile();
            snake.Spawn(emptyTile);
        }
        Apple = FindEmptyTile();
    }

    private Vector2 FindEmptyTile()
    {
        var occupiedTiles = new HashSet<Vector2>();
        foreach (var snake in Snakes)
        {
            foreach (var segment in snake.Segments)
            {
                occupiedTiles.Add(segment);
            }
        }
     
        var tile = new Vector2(Random.Next(0, GridSize.Width), Random.Next(0, GridSize.Height));
        while (occupiedTiles.Contains(tile))
            tile = new Vector2(Random.Next(0, GridSize.Width), Random.Next(0, GridSize.Height));
        return tile;
    }
}
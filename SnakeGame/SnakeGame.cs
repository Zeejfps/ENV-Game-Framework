using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.Rendering;
using SnakeGame;

namespace SampleGames;

public class SnakeGame : Game
{
    public bool IsPaused { get; set; }
    public Snake[] Snakes { get; }
    
    private GridSize GridSize { get; }
    private Vector2 Apple { get; set; }
    private Random Random { get; } = new();
    
    private IWindow Window { get; }
    private IGpu Gpu => Window.Gpu;
    
    private float Speed { get; set; }
    private float m_AccumulatedTime;
    private readonly OrthographicCamera m_Camera;
    private readonly SpriteRenderer m_SpriteRenderer;
    private readonly GridRenderer m_GridRenderer;
    
    public SnakeGame(IWindow window, IEventLoop eventLoop, ILogger logger) : base(eventLoop, logger)
    {
        Window = window;
        GridSize = new GridSize(21, 21);
        
        Snakes = new[]
        {
            new Snake(Logger),
            new Snake(Logger)
        };

        m_Camera = OrthographicCamera.FromLRBT(0, GridSize.Width, 0, GridSize.Height, 0.1f, 10f);
        m_Camera.Transform.WorldPosition = new Vector3(0f, 0f, -5f);
        m_SpriteRenderer = new SpriteRenderer(Gpu);
        m_GridRenderer = new GridRenderer(Gpu, GridSize);
    }

    public void Restart()
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

    public void IncreaseSpeed()
    {
        Speed += 0.5f;
    }

    public void DecreaseSpeed()
    {
        Speed -= 0.5f;
    }

    protected override void OnStart()
    {
        m_SpriteRenderer.LoadResources();
        m_GridRenderer.LoadResources();
        Restart();
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
                    snake.Grow();
                    Apple = FindEmptyTile();
                }

                if (snake.IsSelfIntersecting)
                {
                    Restart();
                    return;
                }

                foreach (var otherSnake in Snakes)
                {
                    if (snake == otherSnake)
                        continue;
                    
                    if (snake.IsCollidingWith(otherSnake.Segments))
                    {
                        Restart();  
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

        var tempRenderbufferHandle = gpu.CreateRenderbuffer(1, false, 320, 320);
        
        var activeRenderbuffer = gpu.Renderbuffer;
        activeRenderbuffer.Bind(tempRenderbufferHandle);
        activeRenderbuffer.ClearColorBuffers(0f, 0.3f, 0f, 1f);

        m_GridRenderer.Render();

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

        activeRenderbuffer.BindToWindow();
        activeRenderbuffer.Blit(tempRenderbufferHandle);
        
        gpu.ReleaseRenderbuffer(tempRenderbufferHandle);
    }

    protected override void OnStop()
    {
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
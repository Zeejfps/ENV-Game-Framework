using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
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
    private GameInputLayer GameInputLayer { get; }
    private UIInputLayer UIInputLayer { get; }
    private bool IsPaused { get; set; }
    
    public SnakeGame(IContext context, IEventBus eventBus) : base(context.Window, context.Input)
    {
        Context = context;
        Gpu = context.Gpu;
        Logger = context.Logger;
        Container = context.Container;
        EventBus = eventBus;

        GameInputLayer = new GameInputLayer();
        UIInputLayer = new UIInputLayer();
        
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
        
        Input.BindAction(InputActions.MoveUpAction, Snake.TurnNorth);
        Input.BindAction(InputActions.MoveLeftAction, Snake.TurnWest);
        Input.BindAction(InputActions.MoveRightAction, Snake.TurnEast);
        Input.BindAction(InputActions.MoveDownAction, Snake.TurnSouth);
        Input.BindAction(InputActions.IncreaseSpeedAction, IncreaseSpeed);
        Input.BindAction(InputActions.DecreaseSpeedAction, DecreaseSpeed);
        Input.BindAction(InputActions.ResetAction, Reset);
        Input.BindAction(InputActions.QuitAction, Stop);
        Input.BindAction(InputActions.PauseResumeAction, TogglePause);
        Input.PushLayer(GameInputLayer);
    }

    protected override void OnUpdate()
    {
        if (IsPaused)
            return;
        
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

    private void TogglePause()
    {
        if (IsPaused)
        {
            IsPaused = false;
            Input.PopLayer();
            return;
        }
        
        IsPaused = true;
        Input.PushLayer(UIInputLayer);
    }

    private void IncreaseSpeed()
    {
        Snake.Speed += 0.5f;
    }

    private void DecreaseSpeed()
    {
        Snake.Speed -= 0.5f;
    }

    private void Reset()
    {
        Snake.Reset();
        Apple = SpawnApple();
    }

    private Vector2 SpawnApple()
    {
        var position = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
        while (Snake.Segments.Contains(position))
            position = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
        return position;
    }
}
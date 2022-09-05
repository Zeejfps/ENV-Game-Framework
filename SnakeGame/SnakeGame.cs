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
    
    private Snake Snake1 { get; }
    private Snake Snake2 { get; }
    
    private Grid Grid { get; }
    private Vector2 Apple { get; set; }
    private Random Random { get; } = new();
    private IEventBus EventBus { get; }
    private GameInputBindings GameInputBindings { get; set; }
    private UIInputBindings UIInputBindings { get; set; }
    
    private SnakeController Player1 { get; }
    private SnakeController Player2 { get; }
    private GameController Controller { get; }
    
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
        Container.BindSingleton(Grid);
        
        Snake1 = new Snake(Grid, Logger);
        Player1 = new SnakeController(0, Snake1);

        Snake2 = new Snake(Grid, Logger);
        Player2 = new SnakeController(1, Snake2);
        
        m_Camera = OrthographicCamera.FromLRBT(0, Grid.Width, 0, Grid.Height, 0.1f, 10f);
        m_Camera.Transform.WorldPosition = new Vector3(0f, 0f, -5f);
        m_SpriteRenderer = new SpriteRenderer(Gpu);

        Controller = new GameController(this);
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
        
        GameInputBindings = PlayerPrefs.LoadInputBindingsAsync<GameInputBindings>().Result;
        UIInputBindings = PlayerPrefs.LoadInputBindingsAsync<UIInputBindings>().Result;

        Player1.Bind(Input);
        Player2.Bind(Input);
        Controller.Bind(Input);
        
        Input.GamepadConnected += OnGamepadConnected;
        Input.GamepadDisconnected += OnGamepadDisconnected;
        
        ResetLevel();
    }

    private void OnGamepadConnected(GamepadConnectedEvent evt)
    {
        Logger.Trace($"Gamepad Connected: {evt.Gamepad}");
        evt.Gamepad.SouthButton.Pressed += SouthButtonOnPressed;
    }

    private void SouthButtonOnPressed(InputButtonStateChangedEvent evt)
    {
        Logger.Trace("A is pressed");
    }

    private void OnGamepadDisconnected(GamePadDisconnectedEvent evt)
    {
        Logger.Trace($"Gamepad Disconnected: {evt.Gamepad}");
        evt.Gamepad.DPadRightButton.Pressed -= SouthButtonOnPressed;
    }

    protected override void OnUpdate()
    {
        if (IsPaused)
            return;

        Snake1.Update(Clock.UpdateDeltaTime);
        Snake2.Update(Clock.UpdateDeltaTime);

        if (Snake1.Head == Apple)
        {
            Apple = SpawnApple();
            Snake1.Grow();
        }

        if (Snake1.IsSelfIntersecting)
        {
            Apple = SpawnApple();
            Snake1.Reset();
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
        
        foreach (var segment in Snake1.Segments)
        {
            var color = segment == Snake1.Head
                ? new Vector3(0.1f, 1f, 0.1f)
                : new Vector3(1f, 0f, 1f);
            m_SpriteRenderer.DrawSprite(segment, color);
        }
        
        foreach (var segment in Snake2.Segments)
        {
            var color = segment == Snake2.Head
                ? new Vector3(0.3f, 0.5f, 0.3f)
                : new Vector3(1f, 0.5f, 0.5f);
            m_SpriteRenderer.DrawSprite(segment, color);
        }
        
        m_SpriteRenderer.RenderBatch(m_Camera);
        
        gpu.RestoreState();
    }

    protected override void OnStop()
    {
    }

    public void IncreaseSpeed()
    {
        Snake1.Speed += 0.5f;
    }

    public void DecreaseSpeed()
    {
        Snake1.Speed -= 0.5f;
    }

    public void ResetLevel()
    {
        Snake1.Reset();
        Snake2.Reset();
        Apple = SpawnApple();
    }

    private Vector2 SpawnApple()
    {
        var position = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
        while (Snake1.Segments.Contains(position))
            position = new Vector2(Random.Next(0, Grid.Width), Random.Next(0, Grid.Height));
        return position;
    }
}
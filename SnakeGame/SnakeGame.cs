using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;

namespace SampleGames;

public class TestLayer : IInputLayer
{
    public void Bind(IInput input)
    {
        input.Keyboard.BindKeyToAction(KeyboardKey.P, "Game/Pause");
    }

    public void Unbind(IInput input)
    {
        input.Keyboard.UnbindKey(KeyboardKey.P);
    }
}

public class SnakeGame : Game, IInputLayer
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
    
    private bool IsPaused { get; set; }
    
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

        Input.BindAction(InputActions.MoveUpAction, Snake.TurnNorth);
        Input.BindAction(InputActions.MoveLeftAction, Snake.TurnWest);
        Input.BindAction(InputActions.MoveRightAction, Snake.TurnEast);
        Input.BindAction(InputActions.MoveDownAction, Snake.TurnSouth);
        Input.BindAction(InputActions.IncreaseSpeedAction, IncreaseSpeed);
        Input.BindAction(InputActions.DecreaseSpeedAction, DecreaseSpeed);
        Input.BindAction(InputActions.ResetAction, Reset);
        Input.BindAction(InputActions.QuitAction, Stop);
        Input.BindAction(InputActions.PauseResumeAction, TogglePause);
        Input.PushLayer(this);
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
        Input.PushLayer(new TestLayer());
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

    public void Bind(IInput input)
    {
        input.Keyboard.BindKeyToAction(KeyboardKey.Escape, InputActions.QuitAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.R, InputActions.ResetAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.Equals, InputActions.IncreaseSpeedAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.Minus, InputActions.DecreaseSpeedAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.W, InputActions.MoveUpAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.UpArrow, InputActions.MoveUpAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.A, InputActions.MoveLeftAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.LeftArrow, InputActions.MoveLeftAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.D, InputActions.MoveRightAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.RightArrow, InputActions.MoveRightAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.S, InputActions.MoveDownAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.DownArrow, InputActions.MoveDownAction);
        input.Keyboard.BindKeyToAction(KeyboardKey.P, InputActions.PauseResumeAction);
    }

    public void Unbind(IInput input)
    {
        input.Keyboard.UnbindKey(KeyboardKey.Escape);
        input.Keyboard.UnbindKey(KeyboardKey.R);
        input.Keyboard.UnbindKey(KeyboardKey.Equals);
        input.Keyboard.UnbindKey(KeyboardKey.Minus);
        input.Keyboard.UnbindKey(KeyboardKey.W);
        input.Keyboard.UnbindKey(KeyboardKey.UpArrow);
        input.Keyboard.UnbindKey(KeyboardKey.A);
        input.Keyboard.UnbindKey(KeyboardKey.LeftArrow);
        input.Keyboard.UnbindKey(KeyboardKey.D);
        input.Keyboard.UnbindKey(KeyboardKey.RightArrow);
        input.Keyboard.UnbindKey(KeyboardKey.S);
        input.Keyboard.UnbindKey(KeyboardKey.DownArrow);
        input.Keyboard.UnbindKey(KeyboardKey.P);
    }
}
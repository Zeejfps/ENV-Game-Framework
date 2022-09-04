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
        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.Escape, "Quit");
        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.R, "Reset");
        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.Equals, "IncreaseSpeed");
        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.Minus, "DecreaseSpeed");
        
        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.W, "MoveUp");
        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.UpArrow, "MoveUp");

        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.A, "MoveLeft");
        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.LeftArrow, "MoveLeft");

        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.D, "MoveRight");
        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.RightArrow, "MoveRight");

        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.S, "MoveDown");
        Input.Keyboard.CreateKeyToActionBinding(KeyboardKey.DownArrow, "MoveDown");

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

        Input.BindAction("MoveUp", Snake.TurnNorth);
        Input.BindAction("MoveLeft", Snake.TurnWest);
        Input.BindAction("MoveRight", Snake.TurnEast);
        Input.BindAction("MoveDown", Snake.TurnSouth);
        Input.BindAction("IncreaseSpeed", IncreaseSpeed);
        Input.BindAction("DecreaseSpeed", DecreaseSpeed);
        Input.BindAction("Reset", Reset);
        Input.BindAction("Quit", Stop);
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
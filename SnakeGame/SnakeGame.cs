using System.Numerics;
using EasyGameFramework;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.API.InputDevices;
using EasyGameFramework.Cameras;

namespace Snake;

public class SnakeGame : Game
{
    private Direction m_SnakeDirection = Direction.North;
    private readonly LinkedList<ITransform3D> m_Snake;
    private readonly IClock m_Clock;

    private float m_AccumulatedTime;

    private OrthographicCamera m_Camera;
    private SpriteRenderer m_SpriteRenderer;

    private IHandle<IGpuMesh> m_QuadMeshHandle;
    private IHandle<IGpuShader> m_UnlitShaderHandle;

    private IContext Context { get; }
    private IInput m_Input;
    private IGpu m_Gpu;
    
    public SnakeGame(IContext app)
    {
        Context = app;
        m_Input = app.Input;
        m_Gpu = app.Gpu;
        
        m_QuadMeshHandle = m_Gpu.LoadMesh("Assets/quad.mesh");
        m_UnlitShaderHandle = m_Gpu.LoadShader("Assets/sprite.shader");

        m_Clock = new Clock();
        m_Camera = new OrthographicCamera(40, 40, 0.1f, 10)
        {
            Transform =
            {
                WorldPosition = new Vector3(0f, 0f, -5f),
            }
        };
        m_SpriteRenderer = new SpriteRenderer();
        
        var width = 20;
        var height = 20;
        
        m_Snake = new LinkedList<ITransform3D>();
        m_Snake.AddFirst(new Transform3D
        {
            WorldPosition = new Vector3(0f, -5f, 0f),
        });
        m_Snake.AddFirst(new Transform3D
        {
            WorldPosition = new Vector3(0f, -7f, 0f),
        });
        m_Snake.AddFirst(new Transform3D
        {
            WorldPosition = new Vector3(0f, -9f, 0f),
        });
        m_Snake.AddFirst(new Transform3D
        {
            WorldPosition = new Vector3(0f, -11f, 0f),
        });
    }

    private void MoveSnake()
    {
        var first = m_Snake.First!.Value;

        var tail = m_Snake.Last.Value;
        m_Snake.RemoveLast();

        tail.WorldPosition = new Vector3(first.WorldPosition.X + m_SnakeDirection.Dx,
            first.WorldPosition.Y + m_SnakeDirection.Dy * 2f, 0f);
        m_Snake.AddFirst(tail);
    }

    protected override void OnStart()
    {
        var window = Context.Window;
        window.Width = 500;
        window.Height = 500;
        window.IsVsyncEnabled = true;
        window.IsResizable = false;
        window.ShowCentered();
    }

    protected override void OnUpdate(float dt)
    {
        if (Context.Input.Keyboard.WasKeyPressedThisFrame(KeyboardKey.Escape))
        {
            Quit();
            return;
        }
        
        m_Clock.Tick();
        m_AccumulatedTime += m_Clock.DeltaTime;
        if (m_AccumulatedTime >= 1f)
        {
            m_AccumulatedTime = 0f;
            MoveSnake();
        }

        var renderbufferManager = m_Gpu.RenderbufferManager;
        renderbufferManager.Bind(null);
        renderbufferManager.ClearColorBuffer(0f, 0.3f, 0f, 1f);
        
        m_Gpu.EnableBackfaceCulling = false;
        m_SpriteRenderer.Render(m_Gpu, m_Camera, m_UnlitShaderHandle, m_QuadMeshHandle, m_Snake);

        if (m_Input.Keyboard.WasKeyPressedThisFrame(KeyboardKey.A))
        {
            m_SnakeDirection = new Direction(-1, 0);
        }
    }

    protected override void OnRender(float dt)
    {
        
    }

    protected override void OnQuit()
    {
    }
}
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

    private float m_AccumulatedTime;

    private OrthographicCamera m_Camera;
    private SpriteRenderer m_SpriteRenderer;

    private IHandle<IGpuMesh> m_QuadMeshHandle;
    private IHandle<IGpuShader> m_UnlitShaderHandle;

    private IContext Context { get; }
    private IInput Input { get; }
    private IGpu Gpu { get; }
    private ILogger Logger { get; }
    
    public SnakeGame(IContext context)
    {
        Context = context;
        Input = context.Input;
        Gpu = context.Gpu;
        Logger = context.Logger;
        
        m_QuadMeshHandle = Gpu.Mesh.Load("Assets/quad");
        m_UnlitShaderHandle = Gpu.Shader.Load("Assets/sprite");

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
        m_Snake.AddLast(new Transform3D
        {
            WorldPosition = new Vector3(0f, -5f, 0f),
        });
        m_Snake.AddLast(new Transform3D
        {
            WorldPosition = new Vector3(0f, -7f, 0f),
        });
        m_Snake.AddLast(new Transform3D
        {
            WorldPosition = new Vector3(0f, -9f, 0f),
        });
        m_Snake.AddLast(new Transform3D
        {
            WorldPosition = new Vector3(0f, -11f, 0f),
        });
    }

    private void MoveSnake()
    {
        var first = m_Snake.First!.Value;

        var tail = m_Snake.Last.Value;
        m_Snake.RemoveLast();

        tail.WorldPosition = new Vector3(first.WorldPosition.X + m_SnakeDirection.Dx * 2f,
            first.WorldPosition.Y + m_SnakeDirection.Dy * 2f, 0f);
        m_Snake.AddFirst(tail);
    }

    private void MoveLeft()
    {
        
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
        if (Input.Keyboard.WasKeyPressedThisFrame(KeyboardKey.Escape))
        {
            Quit();
            return;
        }
        
        if (Input.Keyboard.WasKeyPressedThisFrame(KeyboardKey.A))
        {
            m_SnakeDirection = Direction.West;
        }
        else if (Input.Keyboard.WasKeyPressedThisFrame(KeyboardKey.D)) 
        {
            m_SnakeDirection = Direction.East;
        }
        else if (Input.Keyboard.WasKeyPressedThisFrame(KeyboardKey.W)) 
        {
            m_SnakeDirection = Direction.North;
        }
        else if (Input.Keyboard.WasKeyPressedThisFrame(KeyboardKey.S)) 
        {
            m_SnakeDirection = Direction.South;
        }

        if (Input.Keyboard.WasAnyKeyPressedThisFrame(out var key))
        {
            Logger.Trace(key);
        }
        
        m_AccumulatedTime += dt;
        if (m_AccumulatedTime >= 1f)
        {
            m_AccumulatedTime = 0f;
            MoveSnake();
        }
    }

    protected override void OnRender(float dt)
    {
        var gpu = Gpu;
        gpu.SaveState();
        gpu.EnableBackfaceCulling = false;

        var renderbuffer = Gpu.Renderbuffer;
        renderbuffer.BindWindow();
        renderbuffer.ClearColorBuffers(0f, 0.3f, 0f, 1f);
    
        m_SpriteRenderer.Render(Gpu, m_Camera, m_UnlitShaderHandle, m_QuadMeshHandle, m_Snake);
        
        gpu.RestoreState();
    }

    protected override void OnQuit()
    {
    }
}
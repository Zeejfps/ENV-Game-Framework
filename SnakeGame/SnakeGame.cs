using System.Numerics;
using EasyGameFramework;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.API.InputDevices;
using EasyGameFramework.Cameras;
using Framework;

namespace SnakeGame;

public class Game
{
    private Direction m_SnakeDirection = Direction.North;
    private readonly LinkedList<ITransform3D> m_Snake;
    private readonly IClock m_Clock;

    private float m_AccumulatedTime;

    private OrthographicCamera m_Camera;
    private SpriteRenderer m_SpriteRenderer;

    private IGpuMesh m_QuadMesh;
    private IGpuShader m_UnlitMaterial;

    private ILocator m_Locator;
    private IInput m_Input;
    private IGpuFramebuffer m_WindowFramebuffer;
    
    public Game(IApplication app)
    {
        m_Locator = app.Locator;
        m_Input = app.Input;
        m_WindowFramebuffer = app.Window.Framebuffer;

        var gpuMeshAssetLoader = m_Locator.LocateOrThrow<IAssetLoader<IGpuMesh>>();
        var gpuShaderAssetLoader = m_Locator.LocateOrThrow<IAssetLoader<IGpuShader>>();
        
        m_QuadMesh = gpuMeshAssetLoader.Load("Assets/quad.mesh");
        m_UnlitMaterial = gpuShaderAssetLoader.Load("Assets/sprite.shader");
        m_UnlitMaterial.EnableBackfaceCulling = false;

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

    public void Update()
    {
        m_Clock.Tick();
        m_AccumulatedTime += m_Clock.DeltaTime;
        if (m_AccumulatedTime >= 1f)
        {
            m_AccumulatedTime = 0f;
            MoveSnake();
        }

        using (var framebuffer = m_WindowFramebuffer.Use())
        {
            framebuffer.Clear(0f, 0.3f, 0f, 1f);
            m_SpriteRenderer.Render(m_Camera, m_UnlitMaterial, m_QuadMesh, m_Snake);
        }

        if (m_Input.Keyboard.WasKeyPressedThisFrame(KeyboardKey.A))
        {
            m_SnakeDirection = new Direction(-1, 0);
        }
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
}
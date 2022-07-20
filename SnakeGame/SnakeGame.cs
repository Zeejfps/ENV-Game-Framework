using System.Numerics;
using Framework;
using Framework.Common;
using Framework.Common.Cameras;

namespace SnakeGame;

public class Game
{
    private Direction m_SnakeDirection = Direction.North;
    private readonly LinkedList<ITransform3D> m_Snake;
    private readonly IContext m_Context;
    private readonly IClock m_Clock;

    private float m_AccumulatedTime;

    private OrthographicCamera m_Camera;
    private SpriteRenderer m_SpriteRenderer;

    private IMesh m_QuadMesh;
    private IMaterial m_UnlitMaterial;

    public Game(IContext context)
    {
        m_QuadMesh = context.AssetDatabase.LoadAsset<IMesh>("Assets/quad.mesh");
        m_UnlitMaterial = context.AssetDatabase.LoadAsset<IMaterial>("Assets/sprite.material");
        m_UnlitMaterial.EnableBackfaceCulling = false;

        m_Context = context;
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

        using (var framebuffer = m_Context.Window.Framebuffer.Use())
        {
            framebuffer.Clear(0f, 0.3f, 0f, 1f);
            m_SpriteRenderer.Render(m_Camera, m_UnlitMaterial, m_QuadMesh, m_Snake);
        }
    }

    private void MoveSnake()
    {
        var first = m_Snake.First!.Value;

        var tail = m_Snake.Last.Value;
        m_Snake.RemoveLast();

        tail.WorldPosition = new Vector3(first.WorldPosition.X + m_SnakeDirection.Dx,
            first.WorldPosition.Y + m_SnakeDirection.Dy, 0f);
        m_Snake.AddFirst(tail);
    }
}
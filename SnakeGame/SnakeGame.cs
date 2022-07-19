using Framework;
using Framework.Common.Cameras;

namespace SnakeGame;

public class Game
{
    private Direction m_SnakeDirection = Direction.North;
    private readonly LinkedList<Point> m_Snake;
    private readonly IContext m_Context;
    private readonly IClock m_Clock;

    private float m_AccumulatedTime;

    private OrthographicCamera m_Camera;
    private SpriteRenderer m_SpriteRenderer;
    
    public Game(IContext context)
    {
        var quadMesh = context.AssetDatabase.LoadAsset<IMesh>("Assets/quad.mesh");
        var material = context.AssetDatabase.LoadAsset<IMaterial>("Assets/sprite_material.mat");
        
        m_Context = context;
        m_Clock = new Clock();
        m_Camera = new OrthographicCamera(20, 20, 0.1f, 10);
        m_SpriteRenderer = new SpriteRenderer(quadMesh, material);
        
        var width = 20;
        var height = 20;
        
        m_Snake = new LinkedList<Point>();
        m_Snake.AddFirst(new Point(width / 2, height / 2));
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
            framebuffer.Clear(1f, 0f, 1f, 1f);
            m_SpriteRenderer.Render(m_Camera);
        }
    }

    private void MoveSnake()
    {
        var first = m_Snake.First!.Value;
        var next = new Point(first.X + m_SnakeDirection.Dx, first.Y + m_SnakeDirection.Dy);
        
        m_Snake.RemoveLast();
        m_Snake.AddFirst(next);
    }
}
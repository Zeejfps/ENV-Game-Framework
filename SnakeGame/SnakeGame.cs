using Framework;

namespace SnakeGame;

public class Game
{
    private Direction m_SnakeDirection = Direction.North;
    private readonly LinkedList<Point> m_Snake;
    private readonly IContext m_Context;
    private readonly IClock m_Clock;

    private float m_AccumulatedTime;
    
    public Game(IContext context)
    {
        m_Context = context;
        m_Clock = new Clock();
        
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
    }

    private void MoveSnake()
    {
        var first = m_Snake.First!.Value;
        var next = new Point(first.X + m_SnakeDirection.Dx, first.Y + m_SnakeDirection.Dy);
        
        m_Snake.RemoveLast();
        m_Snake.AddFirst(next);
    }
}
using System.Numerics;
using System.Runtime.InteropServices;
using EasyGameFramework.Api;

namespace SampleGames;

public class Snake
{
    public float Speed { get; set; }
    public ReadOnlySpan<Vector2> Segments => CollectionsMarshal.AsSpan(m_Segments);
    
    private Direction Heading { get; set; }

    private int HeadIndex { get; set; }
    public Vector2 Head => Segments[HeadIndex];

    private int TailIndex { get; set; }
    private Vector2 Tail => Segments[TailIndex];
    private Direction DesiredHeading { get; set; }

    private readonly List<Vector2> m_Segments = new();

    private float m_AccumulatedTime;
    
    private ILogger Logger { get; }
    private Grid Grid { get; }
    
    public Snake(ILogger logger, Grid grid)
    {
        Logger = logger;
        Grid = grid;
    }

    public void Reset()
    {
        var centerPositionX = Grid.Width / 2;
        var centerPositionY = Grid.Height / 2;
        
        m_Segments.Clear();
        m_Segments.Add(new Vector2(centerPositionX, centerPositionY));
        m_Segments.Add(new Vector2(centerPositionX, centerPositionY-1f));
        m_Segments.Add(new Vector2(centerPositionX, centerPositionY-2f));
        m_Segments.Add(new Vector2(centerPositionX, centerPositionY-3f));

        Speed = 3f;
        
        HeadIndex = 0;
        TailIndex = Segments.Length - 1;
        
        Heading = Direction.North;
        DesiredHeading = Heading;
    }

    public void Update(float dt)
    {
        m_AccumulatedTime += dt;
        if (m_AccumulatedTime >= 1f / Speed)
        {
            m_AccumulatedTime = 0f;
            Move();
        }
    }

    private void Move()
    {
        Heading = DesiredHeading;

        var head = Segments[HeadIndex];

        var xPos= head.X + Heading.Dx;
        if (xPos < 0)
            xPos = Grid.Width - 1;
        else if (xPos >= Grid.Width)
            xPos = 0;
        
        var yPos = head.Y + Heading.Dy;
        if (yPos < 0)
            yPos = Grid.Height - 1;
        else if (yPos >= Grid.Height)
            yPos = 0;
        
        m_Segments[TailIndex] = new Vector2(xPos, yPos);

        HeadIndex = TailIndex;
        TailIndex--;
        if (TailIndex < 0)
            TailIndex = Segments.Length - 1;
        
        //Logger.Trace($"HeadIndex: {HeadIndex}, TailIndex: {TailIndex}");
    }

    public void TurnWest()
    {
        if (Heading == Direction.East)
            return;
        
        DesiredHeading = Direction.West;
    }

    public void TurnEast()
    {
        if (Heading == Direction.West)
            return;
        
        DesiredHeading = Direction.East;
    }

    public void TurnNorth()
    {
        if (Heading == Direction.South)
            return;

        DesiredHeading = Direction.North;
    }
    
    public void TurnSouth()
    {
        if (Heading == Direction.North)
            return;

        DesiredHeading = Direction.South;
    }

    public void Grow()
    {
        m_Segments.Add(Head);
    }
}
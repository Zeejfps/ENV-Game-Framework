using System.Numerics;
using System.Runtime.InteropServices;
using EasyGameFramework.Api;

namespace SampleGames;

public class Snake
{
    public ReadOnlySpan<Vector2> Segments => CollectionsMarshal.AsSpan(m_Segments);
    
    private Direction Heading { get; set; }

    private int HeadIndex { get; set; }
    public Vector2 Head => Segments[HeadIndex];

    private int TailIndex { get; set; }
    private Direction DesiredHeading { get; set; }

    private readonly List<Vector2> m_Segments = new();
    
    private ILogger Logger { get; }
    private Grid Grid { get; }
    
    private bool DidGrowThisFrame { get; set; }
    public bool IsSelfIntersecting { get; private set; }

    public Snake(Grid grid, ILogger logger)
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
        
        HeadIndex = 0;
        TailIndex = Segments.Length - 1;
        
        Heading = Direction.North;
        DesiredHeading = Heading;
        IsSelfIntersecting = false;
        DidGrowThisFrame = false;
    }

    public void Move()
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

        IsSelfIntersecting = CheckForSelfCollision();
        DidGrowThisFrame = false;
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
        m_Segments.Insert(HeadIndex, Head);
        TailIndex++;
        DidGrowThisFrame = true;
    }

    private bool CheckForSelfCollision()
    {
        if (DidGrowThisFrame)
            return false;
        
        var headPosition = Head;
        for (var i = 0; i < m_Segments.Count; i++)
        {
            if (i == HeadIndex)
                continue;

            var segmentPosition = Segments[i];
            if (headPosition == segmentPosition)
                return true;
        }

        return false;
    }
}
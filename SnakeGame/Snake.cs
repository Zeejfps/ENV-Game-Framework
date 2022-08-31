using System.Numerics;
using EasyGameFramework.Api;

namespace Core;

public class Snake
{
    public float Speed { get; set; }
    public IReadOnlyList<ITransform3D> Segments => m_Segments;
    
    private Direction Heading { get; set; }

    private int HeadIndex { get; set; }
    private ITransform3D Head => Segments[HeadIndex];

    private int TailIndex { get; set; }
    private ITransform3D Tail => Segments[TailIndex];
    private Direction DesiredHeading { get; set; }

    private readonly List<ITransform3D> m_Segments = new();

    private float m_AccumulatedTime;
    
    private ILogger Logger { get; }
    
    public Snake(ILogger logger)
    {
        Logger = logger;
    }

    public void Reset()
    {
        m_Segments.Clear();
        m_Segments.Add(new Transform3D
        {
            WorldPosition = new Vector3(0f, -5f, 0f),
        });
        m_Segments.Add(new Transform3D
        {
            WorldPosition = new Vector3(0f, -7f, 0f),
        });
        m_Segments.Add(new Transform3D
        {
            WorldPosition = new Vector3(0f, -9f, 0f),
        });
        m_Segments.Add(new Transform3D
        {
            WorldPosition = new Vector3(0f, -11f, 0f),
        });

        Speed = 3f;
        
        HeadIndex = 0;
        TailIndex = Segments.Count - 1;
        
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
        var tail = Segments[TailIndex];

        var xPos= head.WorldPosition.X + Heading.Dx * 2f;
        var yPos = head.WorldPosition.Y + Heading.Dy * 2f;
        
        tail.WorldPosition = new Vector3(xPos, yPos, 0f);

        HeadIndex = TailIndex;
        TailIndex--;
        if (TailIndex < 0)
            TailIndex = Segments.Count - 1;
        
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
}
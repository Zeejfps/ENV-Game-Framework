using System.Numerics;
using EasyGameFramework.Api;

namespace Pong;

public interface IBody
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; }
}

struct PositionUpdateSystemInput
{
    public Vector2 Position;
    public Vector2 Velocity;
}

public sealed class PositionUpdateSystem
{
    private ILogger Logger { get; }
    private int m_Size;
    private readonly IBody[] m_Bodies = new IBody[3];
    private readonly PositionUpdateSystemInput[] m_Data = new PositionUpdateSystemInput[3];

    public PositionUpdateSystem(ILogger logger)
    {
        Logger = logger;
    }

    public void Clear()
    {
        m_Size = 0;
    }

    public void Add(IBody body)
    {
        m_Data[m_Size] = new PositionUpdateSystemInput
        {
            Position = body.Position,
            Velocity = body.Velocity
        };
        m_Bodies[m_Size] = body;
        m_Size++;
    }

    public void Update(float dt)
    {
        var data = m_Data.AsSpan();
        var dataLength = data.Length;
        
        for (var i = 0; i < dataLength; i++)
        {
            var body = m_Bodies[i];
            data[i] = new PositionUpdateSystemInput
            {
                Position = body.Position,
                Velocity = body.Velocity
            };
        }
        
        for (var i = 0; i < dataLength; i++)
        {
            ref var entity = ref data[i];
            entity.Position += entity.Velocity * dt;
        }

        for (var i = 0; i < dataLength; i++)
        {
            m_Bodies[i].Position =  data[i].Position;
        }
    }
}
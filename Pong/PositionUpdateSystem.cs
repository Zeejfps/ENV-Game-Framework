using EasyGameFramework.Api;

namespace Pong;

public sealed class PositionUpdateSystem
{
    private ILogger Logger { get; }
    private int m_Size;
    private readonly IPhysicsEntity[] m_Bodies = new IPhysicsEntity[3];
    private readonly PhysicsEntity[] m_Entities = new PhysicsEntity[3];

    public PositionUpdateSystem(ILogger logger)
    {
        Logger = logger;
    }

    public void Clear()
    {
        m_Size = 0;
    }

    public void Add(IPhysicsEntity body)
    {
        m_Bodies[m_Size] = body;
        m_Size++;
    }

    public void Update(float dt)
    {
        var data = m_Entities.AsSpan();
        var dataLength = data.Length;
        
        for (var i = 0; i < dataLength; i++)
        {
            var body = m_Bodies[i];
            data[i] = body.Save();
        }
        
        for (var i = 0; i < dataLength; i++)
        {
            ref var entity = ref data[i];
            entity.Position += entity.Velocity * dt;
        }

        for (var i = 0; i < dataLength; i++)
        {
            m_Bodies[i].Load(data[i]);
        }
    }
}
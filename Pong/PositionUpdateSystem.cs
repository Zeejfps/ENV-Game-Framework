using EasyGameFramework.Api;

namespace Pong;

public sealed class PositionUpdateSystem
{
    private ILogger Logger { get; }
    private readonly List<IPhysicsEntity> m_Bodies = new();
    private readonly PhysicsEntity[] m_Entities = new PhysicsEntity[32];

    public PositionUpdateSystem(ILogger logger)
    {
        Logger = logger;
    }
    
    public void Add(IPhysicsEntity body)
    {
        m_Bodies.Add(body);
    }

    public void Update(float dt)
    {
        var data = m_Entities.AsSpan();
        var dataLength = m_Bodies.Count;
        
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
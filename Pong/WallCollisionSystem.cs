using EasyGameFramework.Api.Physics;

namespace Pong;

public class WallCollisionSystem
{
    private readonly List<IPhysicsEntity> m_Bodies = new();
    private readonly PhysicsEntity[] m_PhysicsEntities = new PhysicsEntity[32];
    private Rect Bounds { get; }

    public WallCollisionSystem(Rect bounds)
    {
        Bounds = bounds;
    }

    public void Add(IPhysicsEntity entity)
    {
        m_Bodies.Add(entity);
    }
    
    public void Update(float dt)
    {
        var physicsEntities = m_PhysicsEntities.AsSpan();
        var entityCount = m_Bodies.Count;

        for (var i = 0; i < entityCount; i++)
            m_PhysicsEntities[i] = m_Bodies[i].Save();

        var bounds = Bounds;
        for (var i = 0; i < physicsEntities.Length; i++)
        {
            ref var entity = ref physicsEntities[i];
            var position = entity.Position + entity.Velocity * dt;
            if (position.X < bounds.Left || position.X > bounds.Right)
                entity.Velocity = entity.Velocity with { X = -entity.Velocity.X };
            if (position.Y < bounds.Bottom || position.Y > bounds.Top)
                entity.Velocity = entity.Velocity with { Y = -entity.Velocity.Y };
        }
        
        for (var i = 0; i < entityCount; i++)
            m_Bodies[i].Load(m_PhysicsEntities[i]);
    }
}
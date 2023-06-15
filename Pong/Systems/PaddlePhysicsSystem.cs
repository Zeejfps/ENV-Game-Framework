using EasyGameFramework.Api.Physics;

namespace Pong;

public struct PhysicsEntityWithColliderState
{
    public PhysicsEntity PhysicsEntityState;
    public Rect ColliderState;
}

public class PaddlePhysicsSystem
{
    private readonly List<IPhysicsEntityWithCollider> m_Entities = new();
    private readonly PhysicsEntityWithColliderState[] m_States = new PhysicsEntityWithColliderState[5000];
    private Rect Bounds { get; }

    public PaddlePhysicsSystem(Rect bounds)
    {
        Bounds = bounds;
    }

    public void Add(IPhysicsEntityWithCollider entity)
    {
        m_Entities.Add(entity);
    }
    
    public void Update(float dt)
    {
        var states = m_States.AsSpan();
        var entityCount = m_Entities.Count;

        for (var i = 0; i < entityCount; i++)
            m_States[i] = m_Entities[i].Save();

        var bounds = Bounds;
        for (var i = 0; i < states.Length; i++)
        {
            ref var state = ref states[i];
            ref var colliderState = ref state.ColliderState;
            ref var physicsEntityState = ref state.PhysicsEntityState;
            if (colliderState.Left < bounds.Left)
            {
                physicsEntityState.Position = physicsEntityState.Position with{ X = bounds.Left + colliderState.HalfWidth};
                physicsEntityState.Velocity = physicsEntityState.Velocity with { X = 0f };
            }
            else if (colliderState.Right > bounds.Right)
            {
                physicsEntityState.Position = physicsEntityState.Position with{ X = bounds.Right - colliderState.HalfWidth};
                physicsEntityState.Velocity = physicsEntityState.Velocity with { X = 0f };
            }
            else
            {
                physicsEntityState.Position += physicsEntityState.Velocity * dt;
            }
        }
        
        for (var i = 0; i < entityCount; i++)
            m_Entities[i].Load(m_States[i]);
    }
}
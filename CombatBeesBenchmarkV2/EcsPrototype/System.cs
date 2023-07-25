using System.Runtime.CompilerServices;

namespace CombatBeesBenchmarkV2.EcsPrototype;

public abstract class System<TComponent> where TComponent : struct
{
    private readonly IWorld m_World;
    private readonly TComponent[] m_Components;
    
    private IReadOnlyList<IEntity<TComponent>>? m_Entities;

    protected System(IWorld world, int size)
    {
        m_World = world;
        m_Components = new TComponent[size];
    }
    
    public void Update(float dt)
    {
        OnPreUpdate();
        var components = m_Components.AsSpan(0, m_Entities.Count);
        OnUpdate(dt, ref components);
        OnPostUpdate();
    }

    private void OnPreUpdate()
    {
        m_Entities = m_World.Query<TComponent>().ToArray();
        var entities = m_Entities;
        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            ref var component = ref m_Components[i];
            entity.Into(ref component);
        }
    }

    private void OnPostUpdate()
    {
        for (var i = 0; i < m_Entities.Count; i++)
        {
            ref var component = ref m_Components[i];
            var entity = m_Entities[i];
            entity.From(ref component);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void OnUpdate(float dt, ref Span<TComponent> component);
}
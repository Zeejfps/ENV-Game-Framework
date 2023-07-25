using System.Runtime.CompilerServices;

namespace CombatBeesBenchmarkV2.EcsPrototype;

public abstract class System<TComponent> : ISystem where TComponent : struct
{
    private readonly IWorld m_World;
    private readonly TComponent[] m_Components;
    
    private IReadOnlyList<IEntity<TComponent>> m_Entities;
    private int ComponentCount => m_Entities.Count;

    protected System(IWorld world, int size)
    {
        m_World = world;
        m_Components = new TComponent[size];
    }
    
    public void Update(float dt)
    {
        OnPreUpdate();
        var components = m_Components.AsSpan(0, ComponentCount);
        OnUpdate(dt, ref components);
        OnPostUpdate();
    }

    private void OnPreUpdate()
    {
        m_Entities = m_World.Query<TComponent>();

        var entities = m_Entities;
        Parallel.For(0, entities.Count, (i) =>
        {
            var entity = entities[i];
            ref var component = ref m_Components[i];
            entity.Into(ref component);
        });
    }

    private void OnPostUpdate()
    {
        var entities = m_Entities;
        Parallel.For(0, entities.Count, (i) =>
        {
            var entity = entities[i];
            ref var component = ref m_Components[i];
            entity.From(ref component);
        });
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void OnUpdate(float dt, ref Span<TComponent> components);
}
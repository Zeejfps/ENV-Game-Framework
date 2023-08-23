using System.Runtime.CompilerServices;

namespace CombatBeesBenchmarkV2.EcsPrototype;

public abstract class System<TComponent> : ISystem where TComponent : struct
{
    private readonly IWorld m_World;
    private readonly TComponent[] m_Components;
    private readonly IEntity<TComponent>[] m_Entities;
    private int ComponentCount { get; set; }

    protected System(IWorld world, int size)
    {
        m_World = world;
        m_Components = new TComponent[size];
        m_Entities = new IEntity<TComponent>[size];
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
        ComponentCount = m_World.Query<TComponent>(m_Entities);
        Parallel.For(0, ComponentCount, (i) =>
        {
            var entity = m_Entities[i];
            ref var component = ref m_Components[i];
            entity.Into(out component);
        });
    }

    private void OnPostUpdate()
    {
        Parallel.For(0, ComponentCount, (i) =>
        {
            var entity = m_Entities[i];
            ref var component = ref m_Components[i];
            entity.From(ref component);
        });
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void OnUpdate(float dt, ref Span<TComponent> components);
}
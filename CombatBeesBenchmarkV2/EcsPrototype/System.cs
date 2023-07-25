using System.Runtime.CompilerServices;

namespace CombatBeesBenchmarkV2.EcsPrototype;

public abstract class System<TComponent> where TComponent : struct
{
    private readonly IWorld m_World;
    private readonly TComponent[] m_Components;
    
    private IEnumerable<IEntity<TComponent>>? m_Entities;
    private int m_ComponentCount;

    protected System(IWorld world, int size)
    {
        m_World = world;
        m_Components = new TComponent[size];
    }
    
    public void Update(float dt)
    {
        OnPreUpdate();
        var components = m_Components.AsSpan(0, m_ComponentCount);
        OnUpdate(dt, ref components);
        OnPostUpdate();
    }

    private void OnPreUpdate()
    {
        m_Entities = m_World.Query<TComponent>();
        var i = 0;
        foreach (var entity in m_Entities)
        {
            ref var component = ref m_Components[i];
            entity.Into(ref component);
            i++;
        }

        m_ComponentCount = i;
    }

    private void OnPostUpdate()
    {
        var i = 0;
        foreach (var entity in m_Entities)
        {
            ref var component = ref m_Components[i];
            entity.From(ref component);
            i++;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void OnUpdate(float dt, ref Span<TComponent> component);
}
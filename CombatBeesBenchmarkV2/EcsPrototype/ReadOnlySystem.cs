using System.Runtime.CompilerServices;

namespace CombatBeesBenchmarkV2.EcsPrototype;

public abstract class ReadOnlySystem<TComponent> : ISystem where TComponent : struct
{
    private readonly IWorld m_World;
    protected readonly TComponent[] m_Components;
    protected readonly IEntity<TComponent>[] m_Entities;
    protected int ComponentCount { get; set; }

    protected ReadOnlySystem(IWorld world, int size)
    {
        m_World = world;
        m_Components = new TComponent[size];
        m_Entities = new IEntity<TComponent>[size];
    }

    protected IEntity<TComponent> GetEntity(int index)
    {
        return m_Entities[index];
    }

    public virtual void Update(float dt)
    {
        OnPreUpdate();
        var components = m_Components.AsSpan(0, ComponentCount);
        OnUpdate(dt, ref components);
    }

    private void OnPreUpdate()
    {
        ComponentCount = m_World.Query<TComponent>(m_Entities);
        Parallel.For(0, ComponentCount, (i) =>
        {
            var entity = m_Entities[i];
            ref var component = ref m_Components[i];
            entity.Into(ref component);
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void OnUpdate(float dt, ref Span<TComponent> components);
}
using System.Runtime.CompilerServices;

namespace CombatBeesBenchmarkV2.EcsPrototype;

public abstract class System<TArchetype> : ISystem where TArchetype : struct
{
    private readonly IWorld m_World;
    private readonly TArchetype[] m_Components;
    private readonly IEntity<TArchetype>[] m_Entities;

    private int ComponentCount { get; set; }
    protected IWorld World => m_World;
    protected IEntity<TArchetype>[] Entities => m_Entities;

    protected System(IWorld world, int size)
    {
        m_World = world;
        m_Components = new TArchetype[size];
        m_Entities = new IEntity<TArchetype>[size];
    }
    
    public void Tick(float dt)
    {
        Read();
        Update(dt);
        Write();
    }

    private void Update(float dt)
    {
        var components = m_Components.AsSpan(0, ComponentCount);
        OnUpdate(dt, ref components);
    }

    private void Read()
    {
        ComponentCount = m_World.Query<TArchetype>(m_Entities);
        for (var i = 0; i < ComponentCount; i++)
        {
            var entity = m_Entities[i];
            ref var component = ref m_Components[i];
            entity.Into(out component);
        }
        // Parallel.For(0, ComponentCount, (i) =>
        // {
        //     var entity = m_Entities[i];
        //     ref var component = ref m_Components[i];
        //     entity.Into(out component);
        // });
    }

    private void Write()
    {
        for (var i = 0; i < ComponentCount; i++)
        {
            var entity = m_Entities[i];
            ref var component = ref m_Components[i];
            entity.From(ref component);
        }
        
        // Parallel.For(0, ComponentCount, (i) =>
        // {
        //     var entity = m_Entities[i];
        //     ref var component = ref m_Components[i];
        //     entity.From(ref component);
        // });
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void OnUpdate(float dt, ref Span<TArchetype> components);
}
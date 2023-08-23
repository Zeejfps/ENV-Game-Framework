namespace CombatBeesBenchmarkV2.EcsPrototype;

public abstract class System<TComponent> : ReadOnlySystem<TComponent> where TComponent : struct
{
    protected System(IWorld world, int size) : base(world, size)
    {
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        OnPostUpdate();
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
}
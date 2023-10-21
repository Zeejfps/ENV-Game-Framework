using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;
using CombatBeesBenchmarkV3.Systems;
using EasyGameFramework.Api;

namespace CombatBeesBenchmarkV3;

public sealed class CombatBeesBenchmarkGame : Game
{
    private readonly World<Entity> m_World;
    
    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        var random = new Random();

        m_World = new World<Entity>();
        m_World.RegisterSystem(new BeeSpawningSystem(m_World, 100, random));

        for (var i = 0; i < 50; i++)
        {
            var entity = new Entity
            {
                TeamIndex = 0
            };
            m_World.AddEntity<SpawnableBee>(entity);
        }

        for (var i = 0; i < 50; i++)
        {
            var entity = new Entity
            {
                TeamIndex = 1
            };
            m_World.AddEntity<SpawnableBee>(entity);
        }
    }

    protected override void OnStartup()
    {
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        m_World.Tick(Time.UpdateDeltaTime);
    }

    protected override void OnShutdown()
    {
    }
}
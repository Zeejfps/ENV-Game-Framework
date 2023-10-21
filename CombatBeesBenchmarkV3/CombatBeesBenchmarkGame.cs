using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;
using CombatBeesBenchmarkV3.Systems;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;

namespace CombatBeesBenchmarkV3;

public sealed class CombatBeesBenchmarkGame : Game
{
    private readonly ICamera m_Camera;
    private readonly World<Entity> m_World;
    
    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        var random = new Random();
        
        m_Camera = new PerspectiveCamera(65f, 0.777f);
        
        m_World = new World<Entity>();
        m_World.RegisterSystem(new BeeSpawningSystem(m_World, 100, random));
        m_World.RegisterSystem(new BeeRenderingSystem(m_World, 100, context.Window.Gpu, m_Camera));

        for (var i = 0; i < 50; i++)
        {
            var entity = new Entity
            {
                TeamIndex = 0
            };
            m_World.AddEntity<SpawnableBee>(entity);
            m_World.AddEntity<RenderableBee>(entity);
        }

        for (var i = 0; i < 50; i++)
        {
            var entity = new Entity
            {
                TeamIndex = 1
            };
            m_World.AddEntity<SpawnableBee>(entity);
            m_World.AddEntity<RenderableBee>(entity);
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
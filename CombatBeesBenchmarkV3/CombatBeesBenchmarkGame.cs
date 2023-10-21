using System.Numerics;
using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;
using CombatBeesBenchmarkV3.Systems;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using Framework;

namespace CombatBeesBenchmarkV3;

public sealed class CombatBeesBenchmarkGame : Game
{
    private readonly World<Entity> m_World;
    private CameraRigController m_RigController;
    
    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        var random = new Random();
        
        var camera = new PerspectiveCamera(60f, 1.7777f)
        {
            Transform =
            {
                WorldPosition = new Vector3(0f, 0f, 75f)
            }
        };
        
        m_RigController = new CameraRigController(new CameraRig(camera), Window, Input);

        m_World = new World<Entity>();
        m_World.RegisterSystem(new BeeSpawningSystem(m_World, 100, random));
        m_World.RegisterSystem(new BeeRenderingSystem(m_World, 100, context.Window.Gpu, camera));
        m_World.RegisterSystem(new AliveBeeMovementSystem(m_World, 100));
        
        for (var i = 0; i < 50; i++)
        {
            var entity = new Entity
            {
                TeamIndex = 0,
                Color = new Vector4(1f, 0f, 0f, 1f)
            };
            m_World.AddEntity<SpawnableBee>(entity);
            m_World.AddEntity<RenderableBee>(entity);
        }

        for (var i = 0; i < 50; i++)
        {
            var entity = new Entity
            {
                TeamIndex = 1,
                Color = new Vector4(0f, 0f, 1f, 1f)
            };
            m_World.AddEntity<SpawnableBee>(entity);
            m_World.AddEntity<RenderableBee>(entity);
        }
    }

    protected override void OnStartup()
    {
        var window = Window;
        window.Title = "Combat Bees Benchmark";
        window.SetScreenSize(1280, 720);
        window.IsVsyncEnabled = false;
        
        m_RigController.Enable();
    }

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnUpdate()
    {
        var dt = Time.UpdateDeltaTime;
        m_RigController.Update(dt);

        var gpu = Context.Window.Gpu;
        var framebufferController = gpu.FramebufferController;
        framebufferController.BindToWindow();
        framebufferController.ClearColorBuffers(0f, 0.1f, 0.1f, 1f);

        m_World.Tick(dt);
    }

    protected override void OnShutdown()
    {
        m_RigController.Disable();
    }
}
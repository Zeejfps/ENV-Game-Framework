using System.Numerics;
using CombatBeesBenchmarkV2.Systems;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using Framework;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    public const int MaxBeeCount = 100_000;
    
    private World World { get; }
    private NewAliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private NewDeadBeeMovementSystem DeadBeeMovementSystem { get; }
    private NewBeeRenderingSystem BeeRenderingSystem { get; }
    private NewBeeCollisionSystem BeeCollisionSystem { get; }
    private AttractRepelSystem AttractRepelSystem { get; }
    private BeeSpawningSystem BeeSpawningSystem { get; }
    private KillSystem KillSystem { get; }
    private BeeSpawnedEventHandlerSystem BeeSpawnedEventHandlerSystem { get; }
    
    private TargetAssigningSystem TargetAssigningSystem { get; }

    private CameraRig m_CameraRig;
    private CameraRigController m_RigController;

    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        var camera = new PerspectiveCamera(60f, 1.7777f)
        {
            Transform =
            {
                WorldPosition = new Vector3(0f, 0f, 75f)
            }
        };
        var random = new Random();

        m_CameraRig = new CameraRig(camera);
        m_RigController = new CameraRigController(m_CameraRig, Window, Input);

        var numberOfTeams = 2;
        var numberOfBeesPerTeam = MaxBeeCount / numberOfTeams;
        //Logger.Trace($"Number of bees per team: {numberOfBeesPerTeam}");

        var aliveBeePool = new BeePool<Bee?>(random, numberOfTeams, numberOfBeesPerTeam, Logger);

        World = new World(Logger);

        BeeSpawnedEventHandlerSystem = new BeeSpawnedEventHandlerSystem(World, MaxBeeCount, aliveBeePool);
        BeeSpawningSystem = new BeeSpawningSystem(World, MaxBeeCount, random);
        AttractRepelSystem = new AttractRepelSystem(World, MaxBeeCount, aliveBeePool, random);
        DeadBeeMovementSystem = new NewDeadBeeMovementSystem(World, MaxBeeCount);
        AliveBeeMovementSystem = new NewAliveBeeMovementSystem(World, MaxBeeCount);
        BeeRenderingSystem = new NewBeeRenderingSystem(World, MaxBeeCount, Gpu, camera);
        BeeCollisionSystem = new NewBeeCollisionSystem(World, MaxBeeCount);
        TargetAssigningSystem = new TargetAssigningSystem(World, MaxBeeCount, aliveBeePool);
        KillSystem = new KillSystem(World, MaxBeeCount, aliveBeePool);
        
        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            //Logger.Trace($"Team Index: {teamIndex}");
            for (var j = 0; j < numberOfBeesPerTeam; j++)
            {
                //Logger.Trace($"J: {j}");
                var bee = new Bee(teamIndex, World);
                bee.Spawn();
            }
        }
    }

    protected override void OnStartup()
    {
        var time = Time;
        time.SetFixedUpdateDeltaTime(1f / 30f);

        var window = Window;
        window.Title = "Combat Bees Benchmark";
        window.SetScreenSize(1280, 720);
        window.IsVsyncEnabled = false;
        //window.IsFullscreen = true;
        m_RigController.Enable();
    }

    protected override void OnBeginFrame()
    {
        World.BeginFrame();
    }

    protected override void OnEndFrame()
    {
        World.EndFrame();
    }

    protected override void OnFixedUpdate() {}

    protected override void OnUpdate()
    {
        var dt = Time.UpdateDeltaTime;
        
        BeeSpawningSystem.Tick(dt);
        BeeSpawnedEventHandlerSystem.Tick(dt);
        TargetAssigningSystem.Tick(dt);
        AttractRepelSystem.Tick(dt);
        AliveBeeMovementSystem.Tick(dt);
        DeadBeeMovementSystem.Tick(dt);
        BeeCollisionSystem.Tick(dt);
        KillSystem.Tick(dt);
        World.Update(0f);
        
        m_RigController.Update(dt);

        var gpu = Context.Window.Gpu;
        var framebufferController = gpu.FramebufferController;
        framebufferController.BindToWindow();
        framebufferController.ClearColorBuffers(0f, 0.1f, 0.1f, 1f);
        
        BeeRenderingSystem.Tick(dt);
    }

    protected override void OnShutdown()
    {
    }
}
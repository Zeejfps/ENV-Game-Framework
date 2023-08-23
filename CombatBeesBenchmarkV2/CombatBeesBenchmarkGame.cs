using System.Numerics;
using CombatBeesBenchmarkV2.Systems;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using Framework;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    public const int MaxBeeCount = 10000;
    
    private World World { get; }
    private NewAliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private NewDeadBeeMovementSystem DeadBeeMovementSystem { get; }
    private NewBeeRenderingSystem BeeRenderingSystem { get; }
    private NewBeeCollisionSystem BeeCollisionSystem { get; }

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

        var aliveBeePool = new BeePool<Bee>(random, numberOfTeams, numberOfBeesPerTeam, Logger);

        World = new World(
            numberOfTeams,
            numberOfBeesPerTeam,
            aliveBeePool,
            Logger,
            random);
        
        DeadBeeMovementSystem = new NewDeadBeeMovementSystem(World, MaxBeeCount);
        AliveBeeMovementSystem = new NewAliveBeeMovementSystem(World, MaxBeeCount);
        BeeRenderingSystem = new NewBeeRenderingSystem(World, MaxBeeCount, Gpu, camera);
        BeeCollisionSystem = new NewBeeCollisionSystem(World, MaxBeeCount);
    }

    protected override void OnStartup()
    {
        var time = Time;
        time.SetTargetUpdateDeltaTime(1f / 30f);

        var window = Window;
        window.Title = "Combat Bees Benchmark";
        window.SetScreenSize(1280, 720);
        window.IsVsyncEnabled = false;
        //window.IsFullscreen = true;
        m_RigController.Enable();
    }
    
    protected override void OnFixedUpdate()
    {
        var dt = Time.UpdateDeltaTime;
        World.Update(dt);
        AliveBeeMovementSystem.Update(dt);
        DeadBeeMovementSystem.Update(dt);
        BeeCollisionSystem.Update(dt);
    }

    protected override void OnUpdate()
    {
        m_RigController.Update(Time.FrameDeltaTime);

        var gpu = Context.Window.Gpu;
        var framebufferController = gpu.FramebufferController;
        framebufferController.BindToWindow();
        framebufferController.ClearColorBuffers(0f, 0.1f, 0.1f, 1f);
        
        BeeRenderingSystem.Update(Time.FrameDeltaTime);
    }

    protected override void OnShutdown()
    {
    }
}
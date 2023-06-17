using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using Framework;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    public const int MaxBeeCount = 200;
    
    private World World { get; }
    private AliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private DeadBeeMovementSystem DeadBeeMovementSystem { get; }
    private BeeRenderingSystem BeeRenderingSystem { get; }
    private BeeCollisionSystem BeeCollisionSystem { get; }

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
        AliveBeeMovementSystem = new AliveBeeMovementSystem(MaxBeeCount, Logger, random);
        DeadBeeMovementSystem = new DeadBeeMovementSystem(MaxBeeCount, Logger);
        BeeRenderingSystem = new BeeRenderingSystem(MaxBeeCount, Gpu, camera, Logger);
        BeeCollisionSystem = new BeeCollisionSystem(MaxBeeCount, Logger);

        var numberOfTeams = 2;
        var numberOfBeesPerTeam = MaxBeeCount / numberOfTeams;
        //Logger.Trace($"Number of bees per team: {numberOfBeesPerTeam}");

        var aliveBeePool = new BeePool<Bee>(random, numberOfTeams, numberOfBeesPerTeam, Logger);
        var deadBeePool = new BeePool<Bee>(random, numberOfTeams, numberOfBeesPerTeam, Logger);
        World = new World(
            numberOfTeams,
            numberOfBeesPerTeam,
            aliveBeePool,
            deadBeePool,
            AliveBeeMovementSystem, 
            DeadBeeMovementSystem, 
            BeeCollisionSystem,
            BeeRenderingSystem,
            Logger,
            random);
    }

    protected override void OnStartup()
    {
        var window = Window;
        window.Title = "Combat Bees Benchmark";
        window.SetScreenSize(1280, 720);
        window.IsVsyncEnabled = false;
        //window.IsFullscreen = true;
        m_RigController.Enable();
    }

    protected override void OnUpdate()
    {
        var dt = Time.UpdateDeltaTime;
        World.Update(dt);
        AliveBeeMovementSystem.Update(dt);
        DeadBeeMovementSystem.Update(dt);
        BeeCollisionSystem.Update(dt);
        m_RigController.Update(dt);
    }

    protected override void OnRender()
    {
        var gpu = Context.Window.Gpu;
        var framebufferController = gpu.FramebufferController;
        framebufferController.BindToWindow();
        framebufferController.ClearColorBuffers(0f, 0.1f, 0.1f, 1f);
        
        BeeRenderingSystem.Render();
    }

    protected override void OnShutdown()
    {
    }
}
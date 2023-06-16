using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using Framework;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    public const int MaxBeeCount = 100;
    
    private World World { get; }
    private AliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private DeadBeeMovementSystem DeadBeeMovementSystem { get; }
    private BeeRenderingSystem BeeRenderingSystem { get; }

    private CameraRig m_CameraRig;
    private CameraRigController m_RigController;

    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        var camera = new PerspectiveCamera(60f, 1.7777f)
        {
            Transform =
            {
                WorldPosition = new Vector3(0f, 0f, 60f)
            }
        };
        var random = new Random();

        m_CameraRig = new CameraRig(camera);
        m_RigController = new CameraRigController(m_CameraRig, Window, Input);
        AliveBeeMovementSystem = new AliveBeeMovementSystem(MaxBeeCount, Logger, random);
        DeadBeeMovementSystem = new DeadBeeMovementSystem(MaxBeeCount);
        BeeRenderingSystem = new BeeRenderingSystem(MaxBeeCount, Gpu, camera, Logger);
        
        var numberOfTeams = 2;
        var numberOfBeesPerTeam = MaxBeeCount / numberOfTeams;
        Logger.Trace($"Number of bees per team: {numberOfBeesPerTeam}");

        var aliveBeePool = new BeePool<IAliveBee>(random, numberOfTeams, numberOfBeesPerTeam);
        var deadBeePool = new BeePool<IDeadBee>(random, numberOfTeams, numberOfBeesPerTeam);
        World = new World(
            aliveBeePool,
            deadBeePool,
            AliveBeeMovementSystem, 
            DeadBeeMovementSystem, 
            BeeRenderingSystem,
            Logger,
            random);

        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            for (var j = 0; j < numberOfBeesPerTeam; j++)
            {
                var bee = new DeadBee(teamIndex, World);
                World.Spawn(bee);
            }
        }
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
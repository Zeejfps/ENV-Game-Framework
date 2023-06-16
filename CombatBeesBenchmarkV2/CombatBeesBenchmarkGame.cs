using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    public const int MaxBeeCount = 1000;
    
    private World World { get; }
    private AliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private DeadBeeMovementSystem DeadBeeMovementSystem { get; }
    private BeeRenderingSystem BeeRenderingSystem { get; }

    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        // BeeSystem = new BeeSystem(Context, Field, new BeeSystemConfig
        // {
        //     MaxBeeCount = MaxBeeCount,
        //     MinBeeSize = 0.25f,
        //     MaxBeeSize = 0.5f,
        //     FlightJitter = 200f,
        //     Damping = 0.9f,
        //     TeamAttraction = 5f,
        //     TeamRepulsion = 4f,
        //     AttackDistance = 4f,
        //     ChaseForce = 50f,
        //     AttackForce = 500f,
        //     HitDistance = 0.5f
        // });

        AliveBeeMovementSystem = new AliveBeeMovementSystem(MaxBeeCount, Logger);
        DeadBeeMovementSystem = new DeadBeeMovementSystem(MaxBeeCount);
        BeeRenderingSystem = new BeeRenderingSystem(MaxBeeCount);
        
        var numberOfTeams = 2;
        var numberOfBeesPerTeam = MaxBeeCount / numberOfTeams;
        Logger.Trace($"Number of bees per team: {numberOfBeesPerTeam}");
        var random = new Random();

        var aliveBeePool = new BeePool<IAliveBee>(random, numberOfTeams, numberOfBeesPerTeam);
        var deadBeePool = new BeePool<IDeadBee>(random, numberOfTeams, numberOfBeesPerTeam);
        World = new World(
            aliveBeePool,
            deadBeePool,
            AliveBeeMovementSystem, 
            DeadBeeMovementSystem, 
            BeeRenderingSystem);

        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            for (var j = 0; j < numberOfBeesPerTeam; j++)
            {
                var bee = new DeadBee(teamIndex);
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
    }

    protected override void OnUpdate()
    {
        var dt = Time.UpdateDeltaTime;
        AliveBeeMovementSystem.Update(dt);
        DeadBeeMovementSystem.Update(dt);
        World.Update(dt);
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
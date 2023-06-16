using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    public const int MaxBeeCount = 1000;
    
    private AliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private DeadBeeMovementSystem DeadBeeMovementSystem { get; }

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

        AliveBeeMovementSystem = new AliveBeeMovementSystem(MaxBeeCount);
        DeadBeeMovementSystem = new DeadBeeMovementSystem(MaxBeeCount);
        
        var numberOfTeams = 2;
        var numberOfBeesPerTeam = MaxBeeCount / numberOfTeams;
        var random = new Random();

        var aliveBeePool = new BeePool<IAliveBee>(random);
        var deadBeePool = new BeePool<IDeadBee>(random);
        var world = new World(
            aliveBeePool,
            deadBeePool,
            AliveBeeMovementSystem, 
            DeadBeeMovementSystem);

        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            for (var j = 0; j < numberOfBeesPerTeam; j++)
            {
                var bee = new DeadBee(teamIndex);
                world.Spawn(bee);
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
    }

    protected override void OnRender()
    {
        var gpu = Context.Window.Gpu;
        var activeFramebuffer = gpu.FramebufferController;
        activeFramebuffer.BindToWindow();
        activeFramebuffer.ClearColorBuffers(0f, 0.1f, 0.1f, 1f);
    }

    protected override void OnShutdown()
    {
    }
}
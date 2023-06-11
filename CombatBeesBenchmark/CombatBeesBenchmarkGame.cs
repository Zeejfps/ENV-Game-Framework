using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    private const int StartBeeCount = 100;
    
    private Field Field { get; }
    private BeeSystem BeeSystem { get; }
    private BeeSpawner BeeSpawner { get; }
    private ICamera Camera { get; }
    
    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        Field = new Field();
        BeeSystem = new BeeSystem(Context, Field, new BeeSystemConfig
        {
            MaxBeeCount = StartBeeCount,
            MinBeeSize = 0.25f,
            MaxBeeSize = 0.5f,
            FlightJitter = 200f,
            Damping = 0.9f,
            TeamAttraction = 5f,
            TeamRepulsion = 4f,
            AttackDistance = 4f,
            ChaseForce = 50f,
            AttackForce = 500f,
            HitDistance = 0.5f
        });
        BeeSpawner = new BeeSpawner(BeeSystem, StartBeeCount, Context);
        Camera = new PerspectiveCamera(60f, 1.7777f)
        {
            Transform =
            {
                WorldPosition = new Vector3(0f, 0f, 80f)
            }
        };
    }

    protected override void Configure()
    {
        var window = Window;
        window.Title = "Combat Bees Benchmark";
        window.SetScreenSize(1280, 720);
        window.IsVsyncEnabled = false;
    }

    protected override void OnStart()
    {
        BeeSystem.LoadResources();
    }

    protected override void OnUpdate()
    {
        var dt = Time.UpdateDeltaTime;
        BeeSpawner.Update(dt);
        BeeSystem.Update(dt);
    }

    protected override void OnRender()
    {
        var gpu = Context.Window.Gpu;
        var activeFramebuffer = gpu.Renderbuffer;
        activeFramebuffer.BindToWindow();
        activeFramebuffer.ClearColorBuffers(0f, 0.1f, 0.1f, 1f);
        
        BeeSystem.Render(Camera);
    }

    protected override void OnStop()
    {
    }
}
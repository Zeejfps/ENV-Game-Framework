using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    private const int StartBeeCount = 1000;
    
    private Field Field { get; }
    private BeeSystem BeeSystem { get; }
    private BeeSpawner BeeSpawner { get; }
    private OrthographicCamera Camera { get; }
    
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
        });
        BeeSpawner = new BeeSpawner(BeeSystem, StartBeeCount);
        Camera = new OrthographicCamera(10f, 0.1f, 100f);
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
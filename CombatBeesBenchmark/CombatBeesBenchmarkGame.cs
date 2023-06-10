using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    private const int StartBeeCount = 1000;
    
    private BeeManager BeeManager { get; }
    private BeeSpawner BeeSpawner { get; }
    
    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        BeeManager = new BeeManager();
        BeeSpawner = new BeeSpawner(BeeManager, StartBeeCount);
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
    }

    protected override void OnUpdate()
    {
        var dt = Time.UpdateDeltaTime;
        BeeSpawner.Update(dt);
    }

    protected override void OnRender()
    {
    }

    protected override void OnStop()
    {
    }
}
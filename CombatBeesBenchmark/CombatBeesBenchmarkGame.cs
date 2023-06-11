using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using Framework;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    private const int MaxBeeCount = 100000;
    
    private Field Field { get; }
    private BeeSystem BeeSystem { get; }
    private BeeSpawner BeeSpawner { get; }
    
    private BeeSpawningSystem BeeSpawningSystem { get; }
    private BeeMovementSystem MovementSystem { get; }
    private BeePhysicsSystem BeePhysicsSystem { get; }
    private BeeTransformSystem BeeTransformSystem { get; }
    private BeeRenderingSystem BeeRenderingSystem { get; }

    private ICamera Camera { get; }
    private CameraRig CameraRig { get; }
    private CameraRigController CameraRigController { get; }
    
    public CombatBeesBenchmarkGame(IContext context) : base(context)
    {
        Field = new Field();
        BeeSystem = new BeeSystem(Context, Field, new BeeSystemConfig
        {
            MaxBeeCount = MaxBeeCount,
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
        BeeSpawner = new BeeSpawner(BeeSystem, MaxBeeCount, Context);
        Camera = new PerspectiveCamera(60f, 1.7777f)
        {
            Transform =
            {
                WorldPosition = new Vector3(0f, 0f, 80f)
            }
        };
        CameraRig = new CameraRig(Camera);
        CameraRigController = new CameraRigController(CameraRig, Window, Input);

        var random = new Random();
        BeeSpawningSystem = new BeeSpawningSystem(random);
        MovementSystem = new BeeMovementSystem(random);
        BeePhysicsSystem = new BeePhysicsSystem();
        BeeTransformSystem = new BeeTransformSystem();
        BeeRenderingSystem = new BeeRenderingSystem(Context.Window.Gpu, Camera);
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
        //BeeSystem.LoadResources();
        BeeRenderingSystem.LoadResources();
        CameraRigController.Enable();
    }

    protected override void OnUpdate()
    {
        var dt = Time.UpdateDeltaTime;

        BeeSpawningSystem.Update();
        MovementSystem.Update(dt);
        BeePhysicsSystem.Update(dt);
        BeeTransformSystem.Update();
        
        CameraRigController.Update(dt);
    }

    protected override void OnRender()
    {
        var gpu = Context.Window.Gpu;
        var activeFramebuffer = gpu.Renderbuffer;
        activeFramebuffer.BindToWindow();
        activeFramebuffer.ClearColorBuffers(0f, 0.1f, 0.1f, 1f);

        BeeRenderingSystem.Render();
    }

    protected override void OnStop()
    {
    }
}
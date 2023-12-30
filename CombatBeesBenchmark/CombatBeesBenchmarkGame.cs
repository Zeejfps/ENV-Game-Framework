using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using Framework;

namespace CombatBeesBenchmark;

public class CombatBeesBenchmarkGame : Game
{
    private BeeSpawningSystem BeeSpawningSystem { get; }
    private BeeMovementSystem MovementSystem { get; }
    private BeePhysicsSystem BeePhysicsSystem { get; }
    private BeeTransformSystem BeeTransformSystem { get; }
    private BeeRenderingSystem BeeRenderingSystem { get; }

    private ICamera Camera { get; }
    private CameraRig CameraRig { get; }
    private CameraRigController CameraRigController { get; }
    
    public CombatBeesBenchmarkGame(IGameContext gameContext) : base(gameContext)
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
        BeeSpawningSystem = new BeeSpawningSystem(Context, random);
        MovementSystem = new BeeMovementSystem(random, Context.Logger);
        BeePhysicsSystem = new BeePhysicsSystem(Context.Logger);
        BeeTransformSystem = new BeeTransformSystem(Context.Logger);
        BeeRenderingSystem = new BeeRenderingSystem(Context.Logger, Context.Window.Gpu, Camera);
    }

    protected override void OnStartup()
    {
        var window = Window;
        window.Title = "Combat Bees Benchmark";
        window.SetScreenSize(1280, 720);
        window.IsVsyncEnabled = false;
        //window.IsFullscreen = true;
        
        BeeRenderingSystem.LoadResources();
        CameraRigController.Enable();
    }

    protected override void OnFixedUpdate()
    {
        var dt = Time.FixedUpdateDeltaTime;

        BeeSpawningSystem.Update();
        MovementSystem.Update(dt);
        BeePhysicsSystem.Update(dt);
        BeeTransformSystem.Update();
        
        CameraRigController.Update(dt);
    }

    protected override void OnUpdate()
    {
        var gpu = Context.Window.Gpu;
        var activeFramebuffer = gpu.FramebufferController;
        activeFramebuffer.BindToWindow();
        activeFramebuffer.ClearColorBuffers(0f, 0.1f, 0.1f, 1f);

        BeeRenderingSystem.Render();
    }

    protected override void OnShutdown()
    {
    }
}
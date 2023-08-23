using System.Numerics;
using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmark;

public sealed class Bee : IBee,
    IEntity<MovementComponent>,
    IEntity<CollisionComponent>,
    IEntity<BeeRenderComponent>,
    IEntity<AliveBeeComponent>,
    IEntity<DeadBeeComponent>
{
    public bool IsAlive { get; set; }
    public int TeamIndex { get; }

    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Size { get; set; }
    public Vector3 LookDirection { get; set; }
    public float DeathTimer { get; set; }
    public Vector4 Color { get; set; }
    private World World { get; }
    private Bee? Target { get; set; }
    private Random Random { get; }

    public Bee(int teamIndex, World world, Random random)
    {
        TeamIndex = teamIndex;
        World = world;
        Random = random;
        Color = teamIndex == 0 ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(0f, 0f, 1f, 1f);
    }
    
    public void Into(ref CollisionComponent component)
    {
        component.MovementState.Position = Position;
        component.MovementState.Velocity = Velocity;
    }

    public void From(ref CollisionComponent component)
    {
        Position = component.MovementState.Position;
        Velocity = component.MovementState.Velocity;
    }

    public void Into(ref BeeRenderComponent component)
    {
        component.Color = Color;

        var size = Size;
        component.ModelMatrix = Matrix4x4.CreateScale(size, size, size)
                                * Matrix4x4.CreateLookAt(Vector3.Zero, LookDirection, Vector3.UnitY)
                                * Matrix4x4.CreateTranslation(Position);
    }

    public void From(ref BeeRenderComponent component)
    {
        
    }

    public void Into(ref AliveBeeComponent component)
    {
        if (Target == null || !Target.IsAlive)
        {
            Target = World.GetRandomDeadEnemyBee(TeamIndex);
        }
        
        Into(ref component.Movement);
        component.TargetPosition = Target.Position;
        component.LookDirection = LookDirection;
        component.MoveDirection = Random.RandomInsideUnitSphere();
        component.AttractionPoint = World.GetRandomAliveAllyBee(this).Position;
        component.RepellentPoint = World.GetRandomAliveAllyBee(this).Position;
        component.IsTargetKilled = false;
        component.TeamIndex = TeamIndex;
    }

    public void From(ref AliveBeeComponent component)
    {
        var target = Target;
        From(ref component.Movement);
        LookDirection = component.LookDirection;
        if (component.IsTargetKilled && target != null && target.IsAlive)
        {
            World.Kill(target);
            Target = World.GetRandomDeadEnemyBee(TeamIndex);
        }
    }

    public void Into(ref DeadBeeComponent component)
    {
        Into(ref component.Movement);
        component.DeathTimer = DeathTimer;
    }

    public void From(ref DeadBeeComponent component)
    {
        From(ref component.Movement);
        DeathTimer = component.DeathTimer;
        if (DeathTimer <= 0f)
            World.Spawn(this);
    }

    public void Into(ref MovementComponent component)
    {
        component.Position = Position;
        component.Velocity = Velocity;
    }

    public void From(ref MovementComponent component)
    {
        Position = component.Position;
        Velocity = component.Velocity;
    }
}
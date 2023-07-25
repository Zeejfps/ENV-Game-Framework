using System.Numerics;
using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmark;

public sealed class Bee : IDeadBee, 
    IEntity<CollisionComponent>,
    IEntity<BeeRenderComponent>,
    IEntity<AliveBeeComponent>,
    IEntity<DeadBeeComponent>
{
    public bool IsAlive { get; set; }
    public int TeamIndex { get; }

    private Vector3 m_Position;
    public Vector3 Position
    {
        get => m_Position;
        set => m_Position = value;
    }

    private Vector3 m_Velocity;
    public Vector3 Velocity
    {
        get => m_Velocity;
        set => m_Velocity = value;
    }

    public float Size { get; set; }

    public Vector3 LookDirection { get; set; }
    public float DeathTimer { get; set; }
    
    public Vector4 Color { get; set; }
    private World World { get; }
    private Bee? Target { get; set; }
    
    private Random Random { get; }
    private BeePool<Bee> AliveBees { get; }

    public Bee(int teamIndex, World world, Random random, BeePool<Bee> aliveBees)
    {
        TeamIndex = teamIndex;
        World = world;
        Random = random;
        AliveBees = aliveBees;
        Color = teamIndex == 0 ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(0f, 0f, 1f, 1f);
    }

    public void Load(DeadBeeComponent state)
    {
        m_Position = state.Movement.Position;
        m_Velocity = state.Movement.Velocity;
        DeathTimer = state.DeathTimer;
        if (DeathTimer <= 0f)
            World.Spawn(this);
    }

    DeadBeeComponent IDeadBee.Save()
    {
        return new DeadBeeComponent
        {
            Movement =
            {
                Position = m_Position,
                Velocity = m_Velocity,
            },
            DeathTimer = DeathTimer
        };
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
            Target = World.GetRandomEnemy(TeamIndex);
        }
        
        component.Movement = new MovementComponent
        {
            Position = m_Position,
            Velocity = m_Velocity
        };
        component.TargetPosition = Target.Position;
        component.LookDirection = LookDirection;
        component.MoveDirection = Random.RandomInsideUnitSphere();
        component.AttractionPoint = AliveBees.GetRandomAllyBee(this).Position;
        component.RepellentPoint = AliveBees.GetRandomAllyBee(this).Position;
        component.IsTargetKilled = false;
    }

    public void From(ref AliveBeeComponent component)
    {
        var target = Target;
        m_Position = component.Movement.Position;
        m_Velocity = component.Movement.Velocity;
        LookDirection = component.LookDirection;
        if (component.IsTargetKilled && target != null && target.IsAlive)
        {
            World.Kill(target);
            Target = World.GetRandomEnemy(TeamIndex);
        }
    }

    public void Into(ref DeadBeeComponent component)
    {
        component.Movement = new MovementComponent
        {
            Position = m_Position,
            Velocity = m_Velocity,
        };
        component.DeathTimer = DeathTimer;
    }

    public void From(ref DeadBeeComponent component)
    {
        m_Position = component.Movement.Position;
        m_Velocity = component.Movement.Velocity;
        DeathTimer = component.DeathTimer;
        if (DeathTimer <= 0f)
            World.Spawn(this);
    }
}
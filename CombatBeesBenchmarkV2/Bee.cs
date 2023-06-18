using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class Bee : IAliveBee, IDeadBee
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

    public Bee(int teamIndex, World world)
    {
        TeamIndex = teamIndex;
        World = world;
        Color = teamIndex == 0 ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(0f, 0f, 1f, 1f);
    }

    public AliveBeeState Save()
    {
        if (Target == null || !Target.IsAlive)
        {
            Target = World.GetRandomEnemy(TeamIndex);
        }
        var target = Target;
        return new AliveBeeState
        {
            Position = m_Position,
            Velocity = m_Velocity,
            TargetPosition = target.Position,
            LookDirection = LookDirection,
        };
    }

    public void Load(DeadBeeState state)
    {
        m_Position = state.Position;
        m_Velocity = state.Velocity;
        DeathTimer = state.DeathTimer;
        if (DeathTimer <= 0f)
            World.Spawn(this);
    }

    public void Load(AliveBeeState state)
    {
        var target = Target;
        m_Position = state.Position;
        m_Velocity = state.Velocity;
        LookDirection = state.LookDirection;
        if (state.IsTargetKilled && target.IsAlive)
        {
            World.Kill(target);
            Target = World.GetRandomEnemy(TeamIndex);
        }
    }

    public MovementState SaveMovementState()
    {
        return new MovementState
        {
            Position = m_Position,
            Velocity = m_Velocity
        };
    }

    public void Load(MovementState state)
    {
        m_Position = state.Position;
        m_Velocity = state.Velocity;
    }

    DeadBeeState IDeadBee.Save()
    {
        return new DeadBeeState
        {
            Position = m_Position,
            Velocity = m_Velocity,
            DeathTimer = DeathTimer
        };
    }
}
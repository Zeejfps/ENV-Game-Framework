using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class Bee : IAliveBee, IDeadBee
{
    public bool IsAlive { get; set; }
    public int TeamIndex { get; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public Vector3 LookDirection { get; set; }
    public float DeathTimer { get; set; }
    
    public Vector4 Color { get; }
    private World World { get; }

    public Bee(int teamIndex, World world)
    {
        TeamIndex = teamIndex;
        World = world;
        Color = teamIndex == 0 ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(0f, 0f, 1f, 1f);
    }

    public AliveBeeState Save()
    {
        var target = World.GetTarget(this);
        return new AliveBeeState
        {
            Position = Position,
            Velocity = Velocity,
            TargetPosition = target.Position,
        };
    }

    public void Load(DeadBeeState state)
    {
        Position = state.Position;
        Velocity = state.Velocity;
        DeathTimer = state.DeathTimer;
        if (DeathTimer <= 0f)
            World.Spawn(this);
    }

    public void Load(AliveBeeState state)
    {
        var target = World.GetTarget(this);
        Position = state.Position;
        Velocity = state.Velocity;
        if (state.IsTargetKilled)
        {
            World.Kill(target);
            World.AssignNewTarget(this);
        }
    }

    public BeeRenderState SaveRenderState()
    {
        return new BeeRenderState
        {
            Color = Color,
            ModelMatrix = Matrix4x4.CreateScale(0.25f, 0.25f, 0.25f)
                          //* Matrix4x4.CreateLookAt(Vector3.Zero, LookDirection, Vector3.UnitY)
                          * Matrix4x4.CreateTranslation(Position),
        };
    }


    public MovementState SaveMovementState()
    {
        return new MovementState
        {
            Position = Position,
            Velocity = Velocity
        };
    }

    public void Load(MovementState state)
    {
        Position = state.Position;
        Velocity = state.Velocity;
    }

    DeadBeeState IDeadBee.Save()
    {
        return new DeadBeeState
        {
            Position = Position,
            Velocity = Velocity,
            DeathTimer = DeathTimer
        };
    }
}
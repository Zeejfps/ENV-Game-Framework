using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class AliveBee : IAliveBee
{
    public int TeamIndex { get; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public Vector3 LookDirection { get; set; }
    public Vector4 Color { get; }
    private World World { get; }

    public AliveBee(int teamIndex, World world)
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
            TargetVelocity = target.Velocity,
        };
    }

    public void Load(AliveBeeState state)
    {
        var target = World.GetTarget(this);
        Position = state.Position;
        Velocity = state.Velocity;
        target.Velocity = state.Velocity;
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
}
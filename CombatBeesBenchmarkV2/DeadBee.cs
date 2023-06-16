using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class DeadBee : IDeadBee
{
    public int TeamIndex { get; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { set; get; }
    public Vector3 LookDirection { get; set; }
    public float DeathTimer { get; set; }
    
    private World World { get; }

    public DeadBee(int teamIndex, World world)
    {
        TeamIndex = teamIndex;
        World = world;
    }

    public DeadBeeState Save()
    {
        return new DeadBeeState
        {
            Position = Position,
            Velocity = Velocity,
            DeathTimer = DeathTimer
        };
    }

    public void Load(DeadBeeState state)
    {
        Position = state.Position;
        Velocity = state.Velocity;
        DeathTimer = state.DeathTimer;
        if (DeathTimer < 0)
            World.Spawn(this);
    }
    
    public BeeRenderState SaveRenderState()
    {
        return new BeeRenderState
        {
            Color = new Vector4(0.5f, 1f, 1f, 1f),
            // ModelMatrix = Matrix4x4.CreateScale(1f, 1f, 1f)
            //               * Matrix4x4.CreateLookAt(Vector3.Zero, LookDirection, Vector3.UnitY)
            //               * Matrix4x4.CreateTranslation(Position),
            ModelMatrix = Matrix4x4.CreateScale(3f, 3f, 3f)
                        * Matrix4x4.CreateTranslation(Position),
        };
    }
}
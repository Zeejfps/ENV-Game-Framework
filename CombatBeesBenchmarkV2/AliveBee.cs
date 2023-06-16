using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class AliveBee : IAliveBee
{
    public int TeamIndex { get; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    private World World { get; }

    public AliveBee(int teamIndex, World world)
    {
        TeamIndex = teamIndex;
        World = world;
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
        if (state.IsTargetKilled)
        {
            World.Kill(target);
        }
    }
}
using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class AliveBee : IAliveBee
{
    public int TeamIndex { get; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public IAliveBee Target { get; set; }
    private World World { get; }

    public AliveBee(int teamIndex, World world)
    {
        TeamIndex = teamIndex;
        World = world;
    }

    public void AcquireTarget()
    {
        Target = World.GetRandomEnemyBee(TeamIndex);
    }

    public AliveBeeState Save()
    {
        return new AliveBeeState
        {
            Position = Position,
            Velocity = Velocity,
            TargetPosition = Target.Position,
            TargetVelocity = Target.Velocity,
        };
    }

    public void Load(AliveBeeState state)
    {
        Position = state.Position;
        Velocity = state.Velocity;
        if (state.IsTargetKilled)
        {
            World.Kill(Target);
            AcquireTarget();
        }
    }
}
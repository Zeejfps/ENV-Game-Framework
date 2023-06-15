using System.Numerics;

namespace CombatBeesBenchmark;

public interface IBee
{
    BeeState Save();
    void Load(BeeState state);
}

public sealed class Bee : IBee, IAttack
{
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public IBee? Target { get; set; }
    
    AttackState IAttack.Save()
    {
        return new AttackState
        {
            Bee = ((IBee)this).Save(),
            Target = Target.Save()
        };
    }

    BeeState IBee.Save()
    {
        return new BeeState
        {
            Position = Position,
            Velocity = Velocity
        };
    }

    public void Load(BeeState state)
    {
        Position = state.Position;
        Velocity = state.Velocity;
    }

    public void Load(AttackState attackState)
    {
        Load(attackState.Bee);
        Target!.Load(attackState.Target);
        if (attackState.IsTargetKilled)
        {
            Target = null;
        }
    }
}
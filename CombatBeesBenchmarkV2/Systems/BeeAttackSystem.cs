using System.Numerics;

namespace CombatBeesBenchmark;

public interface IAttack
{
    AttackState Save();
    void Load(AttackState attackState);
}

public struct BeeState
{
    public Vector3 Position;
    public Vector3 Velocity;
}

public struct AttackState
{
    public BeeState Bee;
    public BeeState Target;
    public bool IsTargetKilled;
}

public class BeeAttackSystem
{
    private readonly List<IAttack> m_Bees = new();
    private readonly AttackState[] m_States = new AttackState[500];

    public void Add(IAttack bee)
    {
        m_Bees.Add(bee);
    }

    public void Update(float dt)
    {
        var attackDistanceSqr = 2f * dt;
        var hitDistanceSqrd = 2f * dt;
        var chaseForce = 2f * dt;
        var attackForce = 2f * dt;
        
        var states = m_States.AsSpan();
        var stateCount = m_Bees.Count;
        for (var i = 0; i < stateCount; i++)
            states[i] = m_Bees[i].Save();

        for (var i = 0; i < states.Length; i++)
        {
            ref var state = ref states[i];
            ref var bee = ref state.Bee;
            ref var enemyBee = ref state.Target;
            var delta = enemyBee.Position - bee.Position;
            var sqrDist = delta.LengthSquared();
            if (sqrDist > attackDistanceSqr)
            {
                bee.Velocity += delta * (chaseForce / MathF.Sqrt(sqrDist));
            }
            else
            {
                bee.Velocity += delta * (attackForce / MathF.Sqrt(sqrDist));
                if (sqrDist < hitDistanceSqrd)
                {
                    enemyBee.Velocity *= .5f;
                    state.IsTargetKilled = true;
                }
            }
        }
        
        for (var i = 0; i < stateCount; i++) 
            m_Bees[i].Load(states[i]);
    }
}
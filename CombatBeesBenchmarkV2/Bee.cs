using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class Bee : IBee, IAttack, IAliveBee, IDeadBee
{
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public Bee? Target { get; set; }
    public Bee? AttractiveFriendly { get; set; }
    public Bee? RepellentFriendly { get; set; }
    
    private readonly Random m_Random;
    
    private List<Bee> EnemyBees { get; }
    private AliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private DeadBeeMovementSystem DeadBeeMovementSystem { get; }

    public Bee(Random random, 
        AliveBeeMovementSystem aliveBeeMovementSystem,
        DeadBeeMovementSystem deadBeeMovementSystem)
    {
        m_Random = random;
        AliveBeeMovementSystem = aliveBeeMovementSystem;
        DeadBeeMovementSystem = deadBeeMovementSystem;
    }

    public void Spawn()
    {
        Target = FindNewTarget();
        AliveBeeMovementSystem.Add(this);
        DeadBeeMovementSystem.Remove(this);
    }
    
    AttackState IAttack.Save()
    {
        return new AttackState
        {
            Bee = ((IBee)this).Save(),
            Target = ((IBee)Target).Save()
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

    DeadBeeState IDeadBee.Save()
    {
        return new DeadBeeState
        {
            Bee = new BeeState
            {
                Position = Position,
                Velocity = Velocity
            },
            DeathTimer = 10f,
        };
    }

    public void Load(DeadBeeState state)
    {
        Position = state.Bee.Position;
        Velocity = state.Bee.Velocity;
        if (state.DeathTimer <= 0f)
            Despawn();
    }

    private void Despawn()
    {
        DeadBeeMovementSystem.Remove(this);
    }

    public void Load(BeeState state)
    {
        Position = state.Position;
        Velocity = state.Velocity;
    }

    public void Load(AliveBeeState state)
    {
        Load(state.Bee);
    }

    public void Load(AttackState attackState)
    {
        Load(attackState.Bee);
        Target!.Load(attackState.Target);
        if (attackState.IsTargetKilled)
        {
            Target.Kill();
            Target = FindNewTarget();
        }
    }

    private Bee FindNewTarget()
    {
        var randomIndex = m_Random.Next(0, EnemyBees.Count);
        return EnemyBees[randomIndex];
    }

    private void Kill()
    {
        AliveBeeMovementSystem.Remove(this);
        DeadBeeMovementSystem.Add(this);
    }

    public AliveBeeState Save()
    {
        return new AliveBeeState
        {
            Bee = ((IBee)this).Save(),
            AttractionPoint = AttractiveFriendly.Position,
            RepellentPoint = RepellentFriendly.Position,
            RandomDirection = RandomInsideUnitSphere()
        };
    }
    
    private float RandomFloatInRange(float min, float max)
    {
        return m_Random.NextSingle() * (max - min) + min;
    }

    private Vector3 RandomInsideUnitSphere()
    {
        float theta = RandomFloatInRange(0f, 2f * MathF.PI);
        float phi = RandomFloatInRange(0f, MathF.PI);

        float x = MathF.Sin(phi) * MathF.Cos(theta);
        float y = MathF.Sin(phi) * MathF.Sin(theta);
        float z = MathF.Cos(phi);

        return new Vector3(x, y, z);
    }
}
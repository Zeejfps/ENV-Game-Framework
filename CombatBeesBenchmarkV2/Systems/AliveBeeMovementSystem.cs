using System.Numerics;

namespace CombatBeesBenchmark;

public interface IAliveBee
{
    AliveBeeState Save();
    void Load(AliveBeeState state);
}

public struct AliveBeeState
{
    public BeeState Bee;
    public Vector3 AttractionPoint;
    public Vector3 RepellentPoint;
    public Vector3 RandomDirection;
}

public sealed class AliveBeeMovementSystem
{
    private readonly List<IAliveBee> m_Bees = new();
    private readonly AliveBeeState[] m_States = new AliveBeeState[500];

    public void Add(IAliveBee bee)
    {
        m_Bees.Add(bee);
    }

    public void Remove(IAliveBee bee)
    {
        m_Bees.Remove(bee);
    }
    
    public void Update(float dt)
    {
        var teamAttraction = 0f * dt;
        var teamRepulsion = 0f * dt;
        var flightJitter = 0f * dt;
        var damping = 0f * dt;
        
        var states = m_States.AsSpan();
        var stateCount = m_Bees.Count;
        for (var i = 0; i < stateCount; i++)
            states[i] = m_Bees[i].Save();

        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref states[i];
            ref var bee = ref state.Bee;
            bee.Velocity += state.RandomDirection * flightJitter;
            bee.Velocity *= damping;
       
            var attractionPoint = state.AttractionPoint;
            Vector3 delta = attractionPoint - bee.Position;
            var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                bee.Velocity += delta * (teamAttraction / dist);

            var repellentPoint = state.RepellentPoint;
            delta = repellentPoint - bee.Position;
            dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                bee.Velocity -= delta * (teamRepulsion / dist);
        }
        
        for (var i = 0; i < stateCount; i++)
            m_Bees[i].Load(states[i]);
    }
}
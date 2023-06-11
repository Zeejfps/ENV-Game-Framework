using System.Numerics;

namespace CombatBeesBenchmark;

public struct BeeMovementSystemData
{
    public Memory<BeeData> Bees;
}

public sealed class BeeMovementSystem
{
    private Random Random { get; }

    public BeeMovementSystem(Random random)
    {
        Random = random;
    }

    public void Update(float dt, BeeMovementSystemData data)
    {
        // TODO: Bass via config
        var flightJitter = 200f * dt;
        var damping = 0.9f * dt;
        
        var dataLength = data.Bees.Length;
        var bees = data.Bees.Span;
        for (var i = 0; i < dataLength; i++)
        {
            ref var bee = ref bees[i];
            bee.Velocity += RandomInsideUnitSphere() * flightJitter;
            bee.Velocity *= damping;
        }
    }
    
    private float RandomFloatInRange(float min, float max)
    {
        var random = Random;
        return random.NextSingle() * (max - min) + min;
    }

    private Vector3 RandomInsideUnitSphere()
    {
        var theta = RandomFloatInRange(0f, 2f * MathF.PI);
        var phi = RandomFloatInRange(0f, MathF.PI);

        var sinPhi = MathF.Sin(phi);
        
        var x = sinPhi * MathF.Cos(theta);
        var y = sinPhi * MathF.Sin(theta);
        var z = MathF.Cos(phi);

        return new Vector3(x, y, z);
    }
}
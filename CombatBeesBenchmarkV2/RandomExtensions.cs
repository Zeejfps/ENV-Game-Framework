using System.Numerics;

namespace CombatBeesBenchmark;

public static class RandomExtensions
{
    public static float NextSingleInRange(this Random random, float min, float max)
    {
        return random.NextSingle() * (max - min) + min;
    }
    
    public static Vector3 RandomInsideUnitSphere(this Random random)
    {
        var theta = random.NextSingleInRange(0f, 2f * MathF.PI);
        var phi = random.NextSingleInRange(0f, MathF.PI);

        var sinPhi = MathF.Sin(phi);
        
        var x = sinPhi * MathF.Cos(theta);
        var y = sinPhi * MathF.Sin(theta);
        var z = MathF.Cos(phi);

        return new Vector3(x, y, z);
    }
}
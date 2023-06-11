using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class BeeMovementSystem
{
    private Random Random { get; }

    public BeeMovementSystem(Random random)
    {
        Random = random;
    }

    public void Update(float dt)
    {
        var flightJitter = Data.FlightJitter * dt;
        var damping = 1f - Data.Damping * dt;

        var numberOfTeams = Data.NumberOfBeeTeams;
        var numberOfBeesPerTeam = Data.NumberOfBeesPerTeam;
        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            var startIndex = teamIndex * numberOfBeesPerTeam;
            var beeCount = Data.AliveBeeCountPerTeam[teamIndex];
            var bees = new Span<BeeData>(Data.AliveBees, startIndex, beeCount);
            for (var i = 0; i < beeCount; i++)
            {
                ref var bee = ref bees[i];
                bee.Velocity += RandomInsideUnitSphere() * flightJitter;
                bee.Velocity *= damping;
            }
        }
    }

    private Vector3 RandomInsideUnitSphere()
    {
        var random = Random;
        var theta = random.NextSingleInRange(0f, 2f * MathF.PI);
        var phi = random.NextSingleInRange(0f, MathF.PI);

        var sinPhi = MathF.Sin(phi);
        
        var x = sinPhi * MathF.Cos(theta);
        var y = sinPhi * MathF.Sin(theta);
        var z = MathF.Cos(phi);

        return new Vector3(x, y, z);
    }
}
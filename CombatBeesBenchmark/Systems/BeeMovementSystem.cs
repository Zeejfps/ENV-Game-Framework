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
        var random = Random;

        var flightJitter = Data.FlightJitter * dt;
        var damping = 1f - Data.Damping * dt;
        var numberOfTeams = Data.NumberOfBeeTeams;
        var numberOfBeesPerTeam = Data.NumberOfBeesPerTeam;
        var teamAttraction = Data.TeamAttraction * dt;
        var teamRepulsion = Data.TeamRepulsion * dt;
        
        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            var startIndex = teamIndex * numberOfBeesPerTeam;
            var aliveBeeCount = Data.AliveBeeCountPerTeam[teamIndex];
            var bees = new Span<BeeData>(Data.AliveBees, startIndex, aliveBeeCount);
            for (var i = 0; i < aliveBeeCount; i++)
            {
                var randomAllyOneIndex = random.Next(0, aliveBeeCount);
                var randomAllyTwoIndex = random.Next(0, aliveBeeCount);

                ref var bee = ref bees[i];
                ref var attractiveFriend = ref bees[randomAllyOneIndex];
                ref var repellentFriend = ref bees[randomAllyTwoIndex];

                bee.Velocity += RandomInsideUnitSphere() * flightJitter;
                bee.Velocity *= damping;

                var delta = attractiveFriend.Position - bee.Position;
                var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
                dist = MathF.Max(0.01f, dist);
                bee.Velocity += delta * (teamAttraction / dist);
                
                delta = repellentFriend.Position - bee.Position;
                dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
                dist = MathF.Max(0.01f, dist);
                bee.Velocity -= delta * (teamRepulsion / dist);
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
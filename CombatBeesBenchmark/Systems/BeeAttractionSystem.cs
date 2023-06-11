namespace CombatBeesBenchmark;

public sealed class BeeAttractionSystem
{
    private Random Random { get; }

    public BeeAttractionSystem(Random random)
    {
        Random = random;
    }

    public void Update(float dt)
    {
        var random = Random;
        var teamAttraction = Data.TeamAttraction * dt;
        var numberOfBeeTeams = Data.NumberOfBeeTeams;
        var numberOfBeesPerTeam = Data.NumberOfBeesPerTeam;
        for (var teamIndex = 0; teamIndex < numberOfBeeTeams; teamIndex++)
        {
            var startIndex = teamIndex * numberOfBeesPerTeam;
            var aliveBeeCount = Data.AliveBeeCountPerTeam[teamIndex];
            var bees = new Span<BeeData>(Data.AliveBees, startIndex, aliveBeeCount);
            for (var i = 0; i < aliveBeeCount; i++)
            {
                var randomBeeIndex = random.Next(0, aliveBeeCount);
                
                ref var bee = ref bees[i];
                ref var allyBee = ref bees[randomBeeIndex];
                
                var delta = allyBee.Position - bee.Position;
                var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
                dist = MathF.Max(0.01f, dist);
                bee.Velocity += delta * (teamAttraction / dist);
            }
        }
    }
}
using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class BeeSpawningSystem
{
    private Random Random { get; }

    public BeeSpawningSystem(Random random)
    {
        Random = random;
    }
    
    public void Update()
    {
        var random = Random;
        var numberOfBeeTeams = Data.NumberOfBeeTeams;
        var numberOfBeesPerTeam = Data.NumberOfBeesPerTeam;
        var fieldWidth = Data.FieldWidth;
        var aliveBeeCountPerTeam = Data.AliveBeeCountPerTeam;
        var minBeeSize = Data.MinBeeSize;
        var maxBeeSize = Data.MaxBeeSize;
        for (var teamIndex = 0; teamIndex < numberOfBeeTeams; teamIndex++)
        {
            var aliveBeeCount = aliveBeeCountPerTeam[teamIndex];
            var numberOfBeesToSpawn = numberOfBeesPerTeam - aliveBeeCount;
            var startIndex = teamIndex * numberOfBeesPerTeam + aliveBeeCount;
            var bees = new Span<BeeData>(Data.AliveBees, startIndex, numberOfBeesToSpawn);
            for (var i = 0; i < numberOfBeesToSpawn; i++)
            {
                var spawnPosition = Vector3.UnitX * (-fieldWidth * .4f + fieldWidth * .8f * teamIndex);
                ref var bee = ref bees[i];
                bee.Position = spawnPosition;
                bee.Size = random.NextSingleInRange(minBeeSize, maxBeeSize);
            }
            
            aliveBeeCountPerTeam[teamIndex] += numberOfBeesToSpawn;
        }
    }
}
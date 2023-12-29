using System.Numerics;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public sealed class BeeSpawningSystem
{
    private IGameContext GameContext { get; }
    private Random Random { get; }

    public BeeSpawningSystem(IGameContext gameContext, Random random)
    {
        GameContext = gameContext;
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
            if (numberOfBeesToSpawn == 0)
                continue;
            
            //Context.Logger.Trace($"Spawning {numberOfBeesToSpawn} for team {teamIndex}");
            var startIndex = teamIndex * numberOfBeesPerTeam + aliveBeeCount;
            var bees = new Span<BeeData>(Data.AliveBees, startIndex, numberOfBeesToSpawn);
            for (var i = 0; i < numberOfBeesToSpawn; i++)
            {
                var spawnPosition = Vector3.UnitX * (-fieldWidth * .4f + fieldWidth * .8f * teamIndex);
                //Context.Logger.Trace($"Spawn Position: {spawnPosition}");
                ref var bee = ref bees[i];
                bee.Position = spawnPosition;
                bee.Size = random.NextSingleInRange(minBeeSize, maxBeeSize);
            }

            var beeColor = Data.TeamColors[teamIndex];
            GameContext.Logger.Trace($"Filling {startIndex} to {startIndex + numberOfBeesToSpawn} with {beeColor} color");
            Array.Fill(Data.AliveBeeColors, beeColor, startIndex, numberOfBeesToSpawn);
            
            aliveBeeCountPerTeam[teamIndex] += numberOfBeesToSpawn;
        }
    }
}
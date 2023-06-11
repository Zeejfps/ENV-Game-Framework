using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public sealed class BeeSpawner
{
    private int StartBeeCount { get; }
    private BeeSystem BeeSystem { get; }
    private IContext Context { get; }

    public BeeSpawner(BeeSystem beeSystem, int startBeeCount, IContext context)
    {
        BeeSystem = beeSystem;
        StartBeeCount = startBeeCount;
        Context = context;
    }

    public void Update()
    {
        var startBeeCount = StartBeeCount;
        var numberOfBeeTeams = BeeSystem.NumberOfBeeTeams;
        var targetBeeCountPerTeam = startBeeCount / numberOfBeeTeams;
        //Context.Logger.Trace($"Bees Per Team: {targetBeeCountPerTeam}");
        
        for (var teamIndex = 0; teamIndex < numberOfBeeTeams; teamIndex++)
        {
            var numberOfBeesToSpawn = targetBeeCountPerTeam - BeeSystem.GetBeeCountForTeam(teamIndex);
            BeeSystem.SpawnBees(teamIndex, numberOfBeesToSpawn);
        }
    }
}
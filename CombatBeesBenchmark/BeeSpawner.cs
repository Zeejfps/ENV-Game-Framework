namespace CombatBeesBenchmark;

public sealed class BeeSpawner
{
    private int StartBeeCount { get; }
    private BeeSystem BeeSystem { get; }

    public BeeSpawner(BeeSystem beeSystem, int startBeeCount)
    {
        BeeSystem = beeSystem;
        StartBeeCount = startBeeCount;
    }

    public void Update(float dt)
    {
        var startBeeCount = StartBeeCount;
        var numberOfBeeTeams = BeeSystem.NumberOfBeeTeams;
        var targetBeeCountPerTeam = startBeeCount / numberOfBeeTeams;

        for (var teamIndex = 0; teamIndex < numberOfBeeTeams; teamIndex++)
        {
            var numberOfBeesToSpawn = targetBeeCountPerTeam - BeeSystem.GetBeeCountForTeam(teamIndex);
            BeeSystem.SpawnBees(teamIndex, numberOfBeesToSpawn);
        }
    }
}
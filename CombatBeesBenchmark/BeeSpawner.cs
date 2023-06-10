namespace CombatBeesBenchmark;

public sealed class BeeSpawner
{
    private int StartBeeCount { get; }
    private BeeManager BeeManager { get; }

    public BeeSpawner(BeeManager beeManager, int startBeeCount)
    {
        BeeManager = beeManager;
        StartBeeCount = startBeeCount;
    }

    public void Update(float dt)
    {
        var startBeeCount = StartBeeCount;
        var halfStartBeeCount = (int)(startBeeCount * 0.5f);
        var teamOneBeeCount = BeeManager.TeamOneBeeCount;
        var teamTwoBeeCount = BeeManager.TeamTwoBeeCount;

        var numberOfBeesToSpawnForTeamOne = halfStartBeeCount - teamOneBeeCount;
        var numberOfBeesToSpawnForTeamTwo = halfStartBeeCount - teamTwoBeeCount;
        
        BeeManager.SpawnBeesForTeamOne(numberOfBeesToSpawnForTeamOne);
        BeeManager.SpawnBeesForTeamTwo(numberOfBeesToSpawnForTeamTwo);
    }
}
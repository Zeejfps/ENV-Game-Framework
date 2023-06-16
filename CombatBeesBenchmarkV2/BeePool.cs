namespace CombatBeesBenchmark;

public sealed class BeePool<TBee> where TBee : IBee
{
    private readonly Random m_Random;
    private readonly List<TBee>[] m_Teams;

    public BeePool(Random random, int numberOfTeams, int numberOfBeesPerTeam)
    {
        m_Random = random;
        m_Teams = new List<TBee>[numberOfTeams];
        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
            m_Teams[teamIndex] = new List<TBee>(numberOfBeesPerTeam);
    }

    public TBee GetRandomEnemyBee(int teamIndex)
    {
        var otherTeam = 1 - teamIndex;
        var team = m_Teams[otherTeam];
        var randIndex = m_Random.Next(0, team.Count);
        return team[randIndex];
    }

    public TBee GetRandomFriendlyBee(int index)
    {
        var team = m_Teams[index];
        return team[0];
    }
    
    public void Add(TBee bee)
    {
        m_Teams[bee.TeamIndex].Add(bee);
    }

    public void Remove(TBee bee)
    {
        m_Teams[bee.TeamIndex].Remove(bee);
    }
}
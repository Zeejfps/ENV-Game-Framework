using System.Collections;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public interface IBeePool<TBee>
{
    int Count { get; }
    TBee? this[int i] { get; }
}

public sealed class BeePool<TBee> : IBeePool<TBee>, IEnumerable<TBee>
    where TBee : IBee
{
    public int Count
    {
        get
        {
            var count = 0;
            foreach (var team in m_Teams)
            {
                count += team.Count;
            }

            return count;
        }
    }
    
    private ILogger Logger { get; }
    private readonly Random m_Random;
    private readonly List<TBee?>[] m_Teams;
    private readonly List<TBee?> m_AllBees = new();

    public BeePool(Random random, int numberOfTeams, int numberOfBeesPerTeam, ILogger logger)
    {
        m_Random = random;
        Logger = logger;
        m_Teams = new List<TBee?>[numberOfTeams];
        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
            m_Teams[teamIndex] = new List<TBee?>(numberOfBeesPerTeam);
    }

    public TBee? GetRandomEnemyBee(int teamIndex)
    {
        var otherTeam = 1 - teamIndex;
        //Logger.Trace($"MyTeam: {teamIndex}: OtherTeam: {otherTeam}");
        var team = m_Teams[otherTeam];
        if (team.Count == 0) return default;
        var randIndex = m_Random.Next(0, team.Count);
        return team[randIndex];
    }

    public TBee? GetRandomAllyBee(int teamIndex)
    {
        var team = m_Teams[teamIndex];
        if (team.Count == 0) return default;
        
        var randIndex = m_Random.Next(0, team.Count);
        var ally = team[randIndex];
        return ally;
    }
    
    public void Add(TBee? bee)
    {
        m_Teams[bee.TeamIndex].Add(bee);
        m_AllBees.Add(bee);
    }

    public void Remove(TBee? bee)
    {
        m_Teams[bee.TeamIndex].Remove(bee);
        m_AllBees.Remove(bee);
    }

    public IEnumerator<TBee?> GetEnumerator()
    {
        return m_AllBees.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public TBee? this[int i]
    {
        get => m_AllBees[i];
    }
}
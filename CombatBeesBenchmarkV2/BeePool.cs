namespace CombatBeesBenchmark;

public sealed class BeePool<TBee> where TBee : IBee
{
    private readonly Random m_Random;
    private readonly List<TBee>[] m_Teams = new List<TBee>[2];

    public BeePool(Random random)
    {
        m_Random = random;
    }

    public TBee GetRandomEnemyBee(int index)
    {
        var team = m_Teams[index];
        return team[0];
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
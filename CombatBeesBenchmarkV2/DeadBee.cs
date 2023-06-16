namespace CombatBeesBenchmark;

public sealed class DeadBee : IDeadBee
{
    public DeadBee(int teamIndex)
    {
        TeamIndex = teamIndex;
    }

    public DeadBeeState Save()
    {
        throw new NotImplementedException();
    }

    public void Load(DeadBeeState state)
    {
        throw new NotImplementedException();
    }

    public int TeamIndex { get; }
}
namespace CombatBeesBenchmark;

public sealed class DeadBee : IDeadBee
{
    public DeadBee(int teamIndex)
    {
        TeamIndex = teamIndex;
    }

    public DeadBeeState Save()
    {
        return new DeadBeeState
        {
            
        };
    }

    public void Load(DeadBeeState state)
    {
        
    }

    public int TeamIndex { get; }
}
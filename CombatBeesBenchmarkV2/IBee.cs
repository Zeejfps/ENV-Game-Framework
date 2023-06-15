namespace CombatBeesBenchmark;

public interface IBee
{
    BeeState Save();
    void Load(BeeState state);
}
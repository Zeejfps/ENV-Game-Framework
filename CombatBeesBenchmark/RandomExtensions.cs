namespace CombatBeesBenchmark;

public static class RandomExtensions
{
    public static float NextSingleInRange(this Random random, float min, float max)
    {
        return random.NextSingle() * (max - min) + min;
    }
}
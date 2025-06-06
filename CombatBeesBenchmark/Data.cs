using System.Numerics;

namespace CombatBeesBenchmark;

public static class Data
{
    public const int MaxBeeCount = 40000;
    public const int NumberOfBeeTeams = 2;
    public const int NumberOfBeesPerTeam = MaxBeeCount / NumberOfBeeTeams;

    public const float FieldWidth = 100f;
    public const float FieldHeight = 20f;
    public const float FieldDepth = 30f;
    public const float MinBeeSize = 0.25f;
    public const float MaxBeeSize = 0.5f;
    public const float FlightJitter = 200f;
    public const float Damping = 0.9f;
    public const float TeamAttraction = 5f;
    public const float TeamRepulsion = 4f;

    public static readonly BeeData[] AliveBees = new BeeData[MaxBeeCount];
    public static readonly Vector4[] AliveBeeColors = new Vector4[MaxBeeCount];
    public static readonly Matrix4x4[] AliveBeenModelMatrices = new Matrix4x4[MaxBeeCount];
    public static readonly int[] AliveBeeCountPerTeam = new int[NumberOfBeeTeams];
    public static readonly Vector4[] TeamColors = {
        new (0.31f, 0.43f, 1f, 1f),
        new (0.95f, 0.95f, 0.59f, 1f),
    };

}
using System.Numerics;

namespace CombatBeesBenchmark;

public struct BeeData
{
    public Vector3 Position;
    public Vector3 Direction;
    public Vector3 Velocity;
    public float Size;
}

public struct BeeTransformSystemData
{
    public Memory<BeeData> Transforms;
    public Memory<Matrix4x4> ModelMatrices;
}

public sealed class BeeTransformSystem
{
    public void Update()
    {
        for (var teamIndex = 0; teamIndex < Data.NumberOfBeeTeams; teamIndex++)
        {
            var startIndex = teamIndex * Data.NumberOfBeesPerTeam;
            var aliveBeeCount = Data.AliveBeeCountPerTeam[teamIndex];
            var aliveBees = new Span<BeeData>(Data.AliveBees, startIndex, aliveBeeCount);
            var modelMatrices = Data.AliveBeenModelMatrices;
            for (var i = 0; i < aliveBeeCount; i++)
            {
                ref var bee = ref aliveBees[i];
                var size = bee.Size;
                var direction = bee.Direction;
                var position = bee.Position;
                modelMatrices[i] = Matrix4x4.CreateScale(size, size, size)
                                   * Matrix4x4.CreateLookAt(Vector3.Zero, direction, Vector3.UnitY)
                                   * Matrix4x4.CreateTranslation(position);
            }
        }
    }
}
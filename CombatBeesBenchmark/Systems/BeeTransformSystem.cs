using System.Numerics;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public struct BeeData
{
    public Vector3 Position;
    public Vector3 Direction;
    public Vector3 Velocity;
    public float Size;
}

public sealed class BeeTransformSystem
{
    public BeeTransformSystem(ILogger logger)
    {
        Logger = logger;
    }

    private ILogger Logger { get; }
    private readonly Task[] m_Tasks = new Task[Data.NumberOfBeeTeams];

    public void Update()
    {
        for (var teamIndex = 0; teamIndex < Data.NumberOfBeeTeams; teamIndex++)
        {
            var index = teamIndex;
            m_Tasks[teamIndex] = Task.Run(() =>
            {
                var startIndex = index * Data.NumberOfBeesPerTeam;
                var aliveBeeCount = Data.AliveBeeCountPerTeam[index];
                var aliveBees = new Span<BeeData>(Data.AliveBees, startIndex, aliveBeeCount);
                var modelMatrices = new Span<Matrix4x4>(Data.AliveBeenModelMatrices, startIndex, aliveBeeCount);
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
            });
        }

        Task.WaitAll(m_Tasks);
    }
}
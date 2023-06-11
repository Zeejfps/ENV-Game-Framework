using System.Numerics;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;


public sealed class BeePhysicsSystem
{
    public BeePhysicsSystem(ILogger logger)
    {
        Logger = logger;
    }

    private ILogger Logger { get; }
    private readonly Task[] m_Tasks = new Task[Data.NumberOfBeeTeams];
    
    public void Update(float dt)
    {
        var fieldHalfWidth = Data.FieldWidth * 0.5f;
        var fieldHalfHeight = Data.FieldHeight * 0.5f;
        var fieldHalfDepth = Data.FieldDepth * 0.5f;

        var numberOfBeeTeams = Data.NumberOfBeeTeams;
        var numberOfBeesPerTeam = Data.NumberOfBeesPerTeam;
        for (var teamIndex = 0; teamIndex < numberOfBeeTeams; teamIndex++)
        {
            var index = teamIndex;
            m_Tasks[teamIndex] = Task.Run(() =>
            {
                var startIndex = index * numberOfBeesPerTeam;
                var aliveBeeCount = Data.AliveBeeCountPerTeam[index];
                var bees = new Span<BeeData>(Data.AliveBees, startIndex, aliveBeeCount);
                for (var i = 0; i < aliveBeeCount; i++)
                {
                    ref var bee = ref bees[i];
                    bee.Direction = Vector3.Lerp(bee.Direction, Vector3.Normalize(bee.Velocity), dt * 4f);
                    bee.Position += bee.Velocity * dt;
                    //Logger.Trace(bee.Velocity);
                
                    if (MathF.Abs(bee.Position.X) > fieldHalfWidth)
                    {
                        bee.Position.X = fieldHalfWidth * MathF.Sign(bee.Position.X);
                        bee.Velocity.X *= -.5f;
                        bee.Velocity.Y *= .8f;
                        bee.Velocity.Z *= .8f;
                    }
                    if (MathF.Abs(bee.Position.Z) > fieldHalfDepth)
                    {
                        bee.Position.Z = fieldHalfDepth * MathF.Sign(bee.Position.Z);
                        bee.Velocity.Z *= -.5f;
                        bee.Velocity.X *= .8f;
                        bee.Velocity.Y *= .8f;
                    }
                    if (MathF.Abs(bee.Position.Y) > fieldHalfHeight)
                    {
                        bee.Position.Y = fieldHalfHeight * MathF.Sign(bee.Position.Y);
                        bee.Velocity.Y *= -.5f;
                        bee.Velocity.Z *= .8f;
                        bee.Velocity.X *= .8f;
                    }
                }
            });
        }

        Task.WaitAll(m_Tasks);
    }
}
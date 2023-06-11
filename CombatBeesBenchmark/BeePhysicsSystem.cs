using System.Numerics;

namespace CombatBeesBenchmark;


public sealed class BeePhysicsSystem
{
    public void Update(float dt)
    {
        var fieldHalfWidth = Data.FieldWidth * 0.5f;
        var fieldHalfHeight = Data.FieldHeight * 0.5f;
        var fieldHalfDepth = Data.FieldDepth * 0.5f;

        for (var teamIndex = 0; teamIndex < Data.NumberOfBeeTeams; teamIndex++)
        {
            var startIndex = teamIndex * Data.NumberOfBeesPerTeam;
            var aliveBeeCount = Data.AliveBeeCountPerTeam[teamIndex];
            var bees = new Span<BeeData>(Data.AliveBees, startIndex, aliveBeeCount);
            for (var i = 0; i < aliveBeeCount; i++)
            {
                ref var bee = ref bees[i];
                bee.Direction = Vector3.Lerp(bee.Direction, Vector3.Normalize(bee.Velocity), dt * 4f);
                bee.Position += bee.Velocity * dt;
            
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
        }
    }
}
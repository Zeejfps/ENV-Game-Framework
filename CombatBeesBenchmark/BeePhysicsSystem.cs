using System.Numerics;

namespace CombatBeesBenchmark;

public struct BeePhysicsSystemData
{
    public Memory<BeeData> Bees;
}

public sealed class BeePhysicsSystem
{
    private Field Field { get; }

    public BeePhysicsSystem(Field field)
    {
        Field = field;
    }

    public void Update(float dt, BeePhysicsSystemData data)
    {
        var field = Field;
        var fieldHalfX = field.Size.X * 0.5f;
        var fieldHalfY = field.Size.Y * 0.5f;
        var fieldHalfZ = field.Size.Z * 0.5f;
        
        var dataLength = data.Bees.Length;
        var transforms = data.Bees.Span;
        for (var i = 0; i < dataLength; i++)
        {
            ref var bee = ref transforms[i];
            bee.Direction = Vector3.Lerp(bee.Direction, Vector3.Normalize(bee.Velocity), dt * 4f);
            bee.Position += bee.Velocity * dt;
            
            if (MathF.Abs(bee.Position.X) > fieldHalfX)
            {
                bee.Position.X = fieldHalfX * MathF.Sign(bee.Position.X);
                bee.Velocity.X *= -.5f;
                bee.Velocity.Y *= .8f;
                bee.Velocity.Z *= .8f;
            }
            if (MathF.Abs(bee.Position.Z) > fieldHalfZ)
            {
                bee.Position.Z = fieldHalfZ * MathF.Sign(bee.Position.Z);
                bee.Velocity.Z *= -.5f;
                bee.Velocity.X *= .8f;
                bee.Velocity.Y *= .8f;
            }
            if (MathF.Abs(bee.Position.Y) > fieldHalfY)
            {
                bee.Position.Y = fieldHalfY * MathF.Sign(bee.Position.Y);
                bee.Velocity.Y *= -.5f;
                bee.Velocity.Z *= .8f;
                bee.Velocity.X *= .8f;
            }
        }
    }
}
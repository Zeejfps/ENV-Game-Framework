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
    public void Update(BeeTransformSystemData data)
    {
        var dataLength = data.Transforms.Length;
        var transforms = data.Transforms.Span;
        var modelMatrices = data.ModelMatrices.Span;
        for (var i = 0; i < dataLength; i++)
        {
            ref var transform = ref transforms[i];
            var size = transform.Size;
            var direction = transform.Direction;
            var position = transform.Position;
            modelMatrices[i] = Matrix4x4.CreateScale(size, size, size)
                             * Matrix4x4.CreateLookAt(Vector3.Zero, direction, Vector3.UnitY)
                             * Matrix4x4.CreateTranslation(position);
        }
    }
}
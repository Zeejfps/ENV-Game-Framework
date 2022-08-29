using System.Numerics;

namespace EasyGameFramework.API;

public interface ICamera
{
    Matrix4x4 ProjectionMatrix { get; }
    ITransform3D Transform { get; }
}
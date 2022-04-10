using System.Numerics;
using Framework;
using TicTacToePrototype;

namespace Framework;

public class OrthographicCamera : ICamera
{
    public Matrix4x4 ProjectionMatrix { get; }
    public ITransform Transform { get; }

    public OrthographicCamera()
    {
        ProjectionMatrix = Matrix4x4.CreateOrthographic(10, 10, 0.1f, 100f);
        Transform = new Transform3D();
    }

    public void Load(IScene scene)
    {
        
    }

    public void Update(IScene scene)
    {
    }

    public void Unload(IScene scene)
    {
        
    }
}
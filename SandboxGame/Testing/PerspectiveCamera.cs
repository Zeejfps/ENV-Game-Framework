using System.Numerics;
using ENV.Engine;
using TicTacToePrototype;

namespace ENV;

public class PerspectiveCamera : ICamera
{
    public Matrix4x4 ProjectionMatrix { get; private set; }
    public ITransform Transform { get; }

    public PerspectiveCamera()
    {
        Transform = new Transform3D();
    }

    public void Load(IScene scene)
    {
        UpdateMatrices(scene);
    }

    public void Update(IScene scene)
    {
        UpdateMatrices(scene);
    }

    public void Unload(IScene scene)
    {
        
    }

    private void UpdateMatrices(IScene scene)
    {
        var window = scene.Context.Window;
        var aspect = window.Framebuffer.Width / (float)window.Framebuffer.Height;
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(0.7f, aspect, 0.1f, 100f);
    }
}
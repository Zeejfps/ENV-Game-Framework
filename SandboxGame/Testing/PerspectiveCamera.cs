using System.Numerics;
using Framework;
using TicTacToePrototype;

namespace Framework;

public class PerspectiveCamera : ICamera
{
    const float DegToRad = 0.0174533f;

    public Matrix4x4 ProjectionMatrix { get; private set; }
    
    public float Fov { get; set; }
    
    public ITransform Transform { get; }

    public PerspectiveCamera()
    {
        Transform = new Transform3D();
        Fov = 75f;
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
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(Fov * DegToRad, aspect, 0.1f, 100f);
    }
}
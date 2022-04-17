using System.Numerics;

namespace Framework.Common.Cameras;

public class PerspectiveCamera : ICamera
{
    const float DegToRad = 0.0174533f;

    public Matrix4x4 ProjectionMatrix { get; private set; }
    public float Fov { get; set; }
    
    public ITransform Transform { get; }

    private IContext m_Context;
    
    public PerspectiveCamera(IContext context)
    {
        m_Context = context;
        Transform = new Transform3D();
        Fov = 75f;
        var window = m_Context.Window;
        var aspect = window.Framebuffer.Width / (float)window.Framebuffer.Height;
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(Fov * DegToRad, aspect, 0.1f, 500f);
    }
    
    public void Update()
    {
    }
}
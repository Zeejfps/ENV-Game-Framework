using ENV.Engine;
using ENV.Engine.InputDevices;

namespace ENV;

public class TestTriangle : ISceneObject
{
    private readonly IMesh m_Mesh;
    private readonly IContext m_Context;
    private readonly ICamera m_Camera;
    private readonly IMaterial m_Material;
    private readonly Random m_Random;
    
    public TestTriangle(IContext context, ICamera camera)
    {
        m_Context = context;
        m_Camera = camera;
        m_Mesh = new Mesh
        {
            Vertices = new[]
            {
                -0.5f, -0.5f, 0.0f,
                0.5f, -0.5f, 0.0f,
                0.0f, 0.5f, 0.0f,
            }
        };

        m_Random = new Random();
        m_Material = new Material("Resources/triangle");
    }

    public void Load(IScene scene)
    {
        
    }

    public void Update(IScene scene)
    {
        var keyboard = m_Context.Window.Input.Keyboard;
        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.R))
            SetRandomColor();
        
        m_Material.SetMatrix4x4("matrix_projection", m_Camera.ProjectionMatrix);
        m_Material.SetMatrix4x4("matrix_view", m_Camera.Transform.WorldMatrix);
        m_Context.Window.Framebuffer.RenderMesh(m_Mesh, m_Material);
    }

    public void Unload(IScene scene)
    {
        
    }

    private void SetRandomColor()
    {
        var r = (float) m_Random.NextDouble();
        var g = (float) m_Random.NextDouble();
        var b = (float) m_Random.NextDouble();

        m_Material.SetVector3("color", r, g, b);
    }
}
using Framework;
using Framework.InputDevices;

namespace Framework;

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


        m_Random = new Random();
    }

    public void Load(IScene scene)
    {
        
    }

    public void Update(IScene scene)
    {
        var keyboard = m_Context.Window.Input.Keyboard;
        
        using var material = m_Material.Use();

        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.R))
            SetRandomColor(material);
        
        material.SetMatrix4x4("matrix_projection", m_Camera.ProjectionMatrix);
        material.SetMatrix4x4("matrix_view", m_Camera.Transform.WorldMatrix);
    }

    public void Unload(IScene scene)
    {
        
    }

    private void SetRandomColor(IMaterialApi material)
    {
        var r = (float) m_Random.NextDouble();
        var g = (float) m_Random.NextDouble();
        var b = (float) m_Random.NextDouble();

        material.SetVector3("color", r, g, b);
    }
}
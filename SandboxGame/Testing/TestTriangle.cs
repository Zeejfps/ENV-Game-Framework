using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.API.InputDevices;

namespace Framework;

public class TestTriangle : ISceneObject
{
    private readonly IGpuMesh m_Mesh;
    private readonly IApplication m_Context;
    private readonly ICamera m_Camera;
    private readonly IHandle<IGpuShader> m_Shader;
    private readonly Random m_Random;
    
    public TestTriangle(IApplication context, ICamera camera)
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
        var gpu = scene.App.Gpu;
        var keyboard = m_Context.Input.Keyboard;

        var shaderManager = gpu.ShaderManager;
        shaderManager.Bind(m_Shader);

        if (keyboard.WasKeyPressedThisFrame(KeyboardKey.R))
            SetRandomColor(shaderManager);
        
        shaderManager.SetMatrix4x4("matrix_projection", m_Camera.ProjectionMatrix);
        shaderManager.SetMatrix4x4("matrix_view", m_Camera.Transform.WorldMatrix);
    }

    public void Unload(IScene scene)
    {
        
    }

    public void Render()
    {
        
    }

    private void SetRandomColor(IShaderManager shaderManager)
    {
        var r = (float) m_Random.NextDouble();
        var g = (float) m_Random.NextDouble();
        var b = (float) m_Random.NextDouble();

        shaderManager.SetVector3("color", r, g, b);
    }
}
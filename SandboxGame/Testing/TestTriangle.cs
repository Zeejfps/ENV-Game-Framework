using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;

namespace Framework;

public class TestTriangle : ISceneObject
{
    private readonly IGpuMesh m_Mesh;
    private readonly IGpu m_Gpu;
    private readonly ICamera m_Camera;
    private readonly IHandle<IGpuShader> m_Shader;
    private readonly Random m_Random;
    private readonly IInputSystem m_Input;
    
    public TestTriangle(IGpu gpu, IInputSystem input, ICamera camera)
    {
        m_Gpu = gpu;
        m_Input = input;
        m_Camera = camera;


        m_Random = new Random();
    }

    private IScene m_Scene;
    
    public void Load(IScene scene)
    {
        m_Scene = scene;
    }

    public void Update(float dt)
    {
        var gpu = m_Gpu;
        var keyboard = m_Input.Keyboard;

        var shaderManager = gpu.ShaderController;
        shaderManager.Bind(m_Shader);
        
        shaderManager.SetMatrix4x4("matrix_projection", m_Camera.ProjectionMatrix);
        shaderManager.SetMatrix4x4("matrix_view", m_Camera.Transform.WorldMatrix);
    }

    public void Unload(IScene scene)
    {
        
    }

    public void Render()
    {
        
    }

    private void SetRandomColor(IShaderController shaderManager)
    {
        var r = (float) m_Random.NextDouble();
        var g = (float) m_Random.NextDouble();
        var b = (float) m_Random.NextDouble();

        shaderManager.SetVector3("color", r, g, b);
    }
}
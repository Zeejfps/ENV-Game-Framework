using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace SampleGames;

public class SpriteRenderer
{
    private const int MAX_BATCH_SIZE = 128;
    
    private IGpu Gpu { get; }
    private IHandle<IGpuShader>? ShaderHandle { get; set; }
    private IHandle<IGpuMesh>? MeshHandle { get; set; }

    public SpriteRenderer(IGpu gpu)
    {
        Gpu = gpu;
    }

    public void LoadResources()
    {
        ShaderHandle = Gpu.Shader.Load("Assets/sprite");
        MeshHandle = Gpu.Mesh.Load("Assets/quad");
    }

    private int m_Size = 0;
    private readonly Vector3[] m_Colors = new Vector3[MAX_BATCH_SIZE];
    private readonly Matrix4x4[] m_ModelMatrices = new Matrix4x4[MAX_BATCH_SIZE];
    
    public void NewBatch()
    {
        m_Size = 0;
    }

    public void DrawSprite(Vector2 position, Vector3 color)
    {
        var modelMatrix = Matrix4x4.CreateScale(0.5f)
                          * Matrix4x4.CreateTranslation(position.X + 0.5f, position.Y + 0.5f, 0f);

        m_Colors[m_Size] = color;
        m_ModelMatrices[m_Size] = modelMatrix;
        
        m_Size++;
    }

    public void RenderBatch(ICamera camera)
    {
        Debug.Assert(ShaderHandle != null);
        Debug.Assert(MeshHandle != null);

        var shader = Gpu.Shader;
        var mesh = Gpu.Mesh;
        
        shader.Bind(ShaderHandle);
        mesh.Bind(MeshHandle);
        
        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
            
        shader.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shader.SetMatrix4x4("matrix_view", viewMatrix);
        shader.SetVector3Array("colors", m_Colors);
        shader.SetMatrix4x4Array("model_matrices", m_ModelMatrices);

        mesh.RenderInstanced(m_Size);
    }

}
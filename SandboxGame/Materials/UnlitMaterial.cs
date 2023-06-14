using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace Framework.Materials;

public class UnlitMaterial : IMaterial
{
    public Matrix4x4 ProjectionMatrix { get; set; }
    public Matrix4x4 ViewMatrix { get; set; }
    
    private readonly Dictionary<IHandle<IGpuMesh>, List<Properties>> m_Batch = new();
    private readonly IHandle<IGpuShader> m_ShaderHandle;
    
    private UnlitMaterial(IHandle<IGpuShader> shaderHandle)
    {
        m_ShaderHandle = shaderHandle;
    }
    
    public void Batch(IHandle<IGpuMesh> meshHandle, in Properties props)
    {
        if (!m_Batch.TryGetValue(meshHandle, out var propsList))
        {
            propsList = new List<Properties>();
            m_Batch[meshHandle] = propsList;
        }
        
        propsList.Add(props);
    }

    public void RenderBatches(IGpu gpu)
    {
        //var gpu = m_Gpu;
        var meshManager = gpu.MeshController;
        var shaderManager = gpu.ShaderController;

        gpu.SaveState();
        gpu.EnableDepthTest = true;
        gpu.EnableBackfaceCulling = false;
        
        shaderManager.Bind(m_ShaderHandle);
        shaderManager.SetMatrix4x4("matrix_projection", ProjectionMatrix);
        shaderManager.SetMatrix4x4("matrix_view", ViewMatrix);
        
        foreach (var kvp in m_Batch)
        {
            var mesh = kvp.Key;
            var propsList = kvp.Value;

            meshManager.Bind(mesh);
            foreach (var props in propsList)
            {
                shaderManager.SetMatrix4x4("matrix_model", props.ModelMatrix);
                shaderManager.SetVector3("color", props.Color);
                meshManager.Render();
            }
        }
        
        gpu.RestoreState();
        m_Batch.Clear();
    }
    
    public readonly struct Properties
    {
        public Matrix4x4 ModelMatrix { get; init; }
        public Vector3 Color { get; init; }
    }

    public static UnlitMaterial Load(IGpu gpu)
    {
        var shader = gpu.ShaderController.Load("Assets/Shaders/unlit.shader");
        return new UnlitMaterial(shader);
    }
}
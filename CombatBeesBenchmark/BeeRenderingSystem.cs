using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace CombatBeesBenchmark;

public struct BeeRenderingData
{
    public Memory<Matrix4x4> ModelMatrices;
    public Memory<Vector3> Colors;
}

public sealed class BeeRenderingSystem
{
    private const int MaxBatchSize = 512;
    private const int MaxBeeCount = 100000;

    private IGpu Gpu { get; }
    private ICamera Camera { get; }
    private IHandle<IGpuMesh>? QuadMeshHandle { get; set; }
    private IHandle<IGpuShader>? BeeShaderHandle { get; set; }

    public BeeRenderingSystem(IGpu gpu, ICamera camera)
    {
        Gpu = gpu;
        Camera = camera;
    }

    public void LoadResources()
    {
        var gpu = Gpu;
        QuadMeshHandle = gpu.Mesh.Load("Assets/quad");
        BeeShaderHandle = gpu.Shader.Load("Assets/bee");
    }
    
    public void Render(BeeRenderingData data)
    {
        var gpu = Gpu;
        var camera = Camera;
        
        gpu.SaveState();

        var activeShader = gpu.Shader;
        var activeMesh = gpu.Mesh;
        
        activeShader.Bind(BeeShaderHandle);
        activeMesh.Bind(QuadMeshHandle);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        activeShader.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        activeShader.SetMatrix4x4("matrix_view", viewMatrix);
        
        var dataLength = data.ModelMatrices.Length;
        var numberOfBatches = (int)MathF.Ceiling(dataLength / (float)MaxBatchSize);

        for (var batchIndex = 0; batchIndex < numberOfBatches; batchIndex++)
        {
            var dataStartIndex = batchIndex * MaxBatchSize;
            var dataEndIndex = dataStartIndex + MaxBatchSize;
            if (dataEndIndex > dataLength)
                dataEndIndex = dataLength;

            var batchSize = dataEndIndex - dataStartIndex;

            var colors = data.Colors.Slice(dataStartIndex, batchSize);
            activeShader.SetVector3Array("colors", colors.Span);

            var modelMatrices = data.ModelMatrices.Slice(dataStartIndex, batchSize);
            activeShader.SetMatrix4x4Array("model_matrices", modelMatrices.Span);
            
            activeMesh.RenderInstanced(batchSize);
        }    
        
        gpu.RestoreState();
    }
}
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace CombatBeesBenchmark;

public sealed class BeeRenderingSystem
{
    private const int MaxBatchSize = 250;

    private ILogger Logger { get; }
    private IGpu Gpu { get; }
    private ICamera Camera { get; }
    private IHandle<IGpuMesh>? QuadMeshHandle { get; set; }
    private IHandle<IGpuShader>? BeeShaderHandle { get; set; }

    public BeeRenderingSystem(ILogger logger, IGpu gpu, ICamera camera)
    {
        Logger = logger;
        Gpu = gpu;
        Camera = camera;
    }

    public void LoadResources()
    {
        var gpu = Gpu;
        QuadMeshHandle = gpu.Mesh.Load("Assets/quad");
        BeeShaderHandle = gpu.Shader.Load("Assets/bee");
    }
    
    public void Render()
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
        
        for (var teamIndex = 0; teamIndex < Data.NumberOfBeeTeams; teamIndex++)
        {
            var startIndex = teamIndex * Data.NumberOfBeesPerTeam;
            var aliveBeesCount = Data.AliveBeeCountPerTeam[teamIndex];

            var colors = new Span<Vector3>(Data.AliveBeeColors, startIndex, aliveBeesCount);
            var modelMatrices = new Span<Matrix4x4>(Data.AliveBeenModelMatrices, startIndex, aliveBeesCount);

            var numBatches = (int)MathF.Ceiling(aliveBeesCount / (float)MaxBatchSize);
            //Logger.Trace($"StartIndex: {startIndex}, Number of batches: {numBatches} for {aliveBeesCount} bees");
            for (var batchIndex = 0; batchIndex < numBatches; batchIndex++)
            {
                var s = batchIndex * MaxBatchSize;
                var batchSize = aliveBeesCount - s;
                if (batchSize > MaxBatchSize)
                    batchSize = MaxBatchSize;
                
                //Logger.Trace($"Batch: {batchIndex} S: {s} BatchSize: {batchSize}");
                activeShader.SetVector3Array("colors", colors.Slice(s, batchSize));
                activeShader.SetMatrix4x4Array("model_matrices", modelMatrices.Slice(s, batchSize));
                activeMesh.RenderInstanced(batchSize);
            } 
        }

        gpu.RestoreState();
    }
}
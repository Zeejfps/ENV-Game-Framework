using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace CombatBeesBenchmark;

public sealed class BeeRenderingSystem
{
    private const int MaxBatchSize = 512;

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
            for (var batchIndex = 0; batchIndex < numBatches; batchIndex++)
            {
                activeShader.SetVector3Array("colors", colors);
                activeShader.SetMatrix4x4Array("model_matrices", modelMatrices);
                activeMesh.RenderInstanced(aliveBeesCount);
            } 
        }

        gpu.RestoreState();
    }
}
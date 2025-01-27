using System.Numerics;
using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class NewBeeRenderingSystem : System<Bee, BeeRenderArchetype>
{
    private IGpu Gpu { get; }
    private ICamera Camera { get; }
    private IHandle<IGpuShader> BeeShaderHandle { get; }
    private IHandle<IGpuMesh> QuadMeshHandle { get; }
    private IShaderStorageBufferHandle BeeBuffer { get; }
    
    public NewBeeRenderingSystem(World<Bee> world, int size, IGpu gpu, ICamera camera) : base(world, size)
    {
        Gpu = gpu;
        Camera = camera;

        BeeShaderHandle = gpu.ShaderController.Load("Assets/bee");
        QuadMeshHandle = gpu.MeshController.Load("Assets/quad");
        BeeBuffer = gpu.BufferController.CreateAndBindShaderStorageBuffer(
            BufferUsage.DynamicDraw, 
            size * 16 * 4 * sizeof(float));
        gpu.ShaderController.AttachBuffer("beeDataBlock", 0, BeeBuffer);
    }

    protected override void OnUpdate(float dt, ref Memory<BeeRenderArchetype> memory)
    {
        var components = memory.Span;
        var gpu = Gpu;
        var camera = Camera;
        var shaderController = gpu.ShaderController;
        var meshController = gpu.MeshController;
        var bufferController = gpu.BufferController;

        gpu.EnableDepthTest = true;
        
        shaderController.Bind(BeeShaderHandle);
        meshController.Bind(QuadMeshHandle);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        shaderController.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shaderController.SetMatrix4x4("matrix_view", viewMatrix);
        
        bufferController.Bind(BeeBuffer);
        bufferController.Upload<BeeRenderArchetype>(components);
        
        meshController.RenderInstanced(components.Length);
    }
}
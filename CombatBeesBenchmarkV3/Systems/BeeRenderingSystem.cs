using System.Numerics;
using CombatBeesBenchmarkV3.EcsPrototype;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace CombatBeesBenchmarkV3.Systems;

public sealed class BeeRenderingSystem : System<Entity, RenderableBee>
{
    private readonly IGpu m_Gpu;
    private readonly ICamera m_Camera;
    private readonly IHandle<IGpuShader> m_BeeShaderHandle;
    private readonly IHandle<IGpuMesh> m_QuadMeshHandle;
    private readonly IShaderStorageBufferHandle m_BeeBuffer;
    
    public BeeRenderingSystem(World<Entity> world, int size, IGpu gpu, ICamera camera) : base(world, size)
    {
        m_Gpu = gpu;
        m_Camera = camera;

        m_BeeShaderHandle = gpu.ShaderController.Load("Assets/bee");
        m_QuadMeshHandle = gpu.MeshController.Load("Assets/quad");
        m_BeeBuffer = gpu.BufferController.CreateAndBindShaderStorageBuffer(
            BufferUsage.DynamicDraw, 
            size * 16 * 4 * sizeof(float));
        gpu.ShaderController.AttachBuffer("beeDataBlock", 0, m_BeeBuffer);
    }
    
    protected override void OnUpdate(float dt, ref Memory<RenderableBee> memory)
    {
        var gpu = m_Gpu;
        var camera = m_Camera;
        var shaderController = gpu.ShaderController;
        var meshController = gpu.MeshController;
        var bufferController = gpu.BufferController;

        gpu.EnableDepthTest = true;
        
        shaderController.Bind(m_BeeShaderHandle);
        meshController.Bind(m_QuadMeshHandle);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        shaderController.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shaderController.SetMatrix4x4("matrix_view", viewMatrix);
        
        bufferController.Bind(m_BeeBuffer);
        bufferController.Upload<RenderableBee>(memory.Span);
        
        //Logger.Trace($"Rendering: {stateCount}");
        meshController.RenderInstanced(memory.Length);
    }
}
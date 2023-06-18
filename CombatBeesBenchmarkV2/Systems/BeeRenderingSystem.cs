using System.Numerics;
using System.Runtime.InteropServices;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace CombatBeesBenchmark;

public interface IRenderableBee
{
    BeeRenderState SaveRenderState();
}

[StructLayout(LayoutKind.Sequential)]
public struct BeeRenderState
{
    public Vector4 Color;
    public Matrix4x4 ModelMatrix;
}

public sealed class BeeRenderingSystem
{
    private IGpu Gpu { get; }
    private ICamera Camera { get; }
    private ILogger Logger { get; }
    private List<IRenderableBee> Entities { get; } = new();
    private IHandle<IGpuShader> BeeShaderHandle { get; }
    private IHandle<IGpuMesh> QuadMeshHandle { get; }
    private IShaderStorageBufferHandle BeeBuffer { get; }

    private readonly BeeRenderState[] m_States;

    public BeeRenderingSystem(int maxBeeCount, IGpu gpu, ICamera camera, ILogger logger)
    {
        Gpu = gpu;
        Camera = camera;
        Logger = logger;
        m_States = new BeeRenderState[maxBeeCount];

        BeeShaderHandle = gpu.ShaderController.Load("Assets/bee");
        QuadMeshHandle = gpu.MeshController.Load("Assets/quad");
        BeeBuffer = gpu.BufferController.CreateAndBindShaderStorageBuffer(
            BufferUsage.DynamicDraw, 
            maxBeeCount * 16 * 4 * sizeof(float));
        gpu.ShaderController.AttachBuffer("beeDataBlock", 0, BeeBuffer);
    }

    public void Render()
    {
        var stateCount = Entities.Count;
        //Logger.Trace($"[RenderingSystem] Entity Count: {Entities.Count}");
        Parallel.For(0, stateCount, i =>
        {
            var entity = Entities[i];
            m_States[i] = entity.SaveRenderState();
        });
        
        var states = m_States.AsSpan();
        var gpu = Gpu;
        var camera = Camera;
        var shaderController = gpu.ShaderController;
        var meshController = gpu.MeshController;
        var bufferController = gpu.BufferController;
        
        shaderController.Bind(BeeShaderHandle);
        meshController.Bind(QuadMeshHandle);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        shaderController.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shaderController.SetMatrix4x4("matrix_view", viewMatrix);
        
        bufferController.Bind(BeeBuffer);
        bufferController.Upload<BeeRenderState>(states);
        
        //Logger.Trace($"Rendering: {stateCount}");
        meshController.RenderInstanced(stateCount);
    }

    public void Add(IRenderableBee bee)
    {
        Entities.Add(bee);
        //Logger.Trace($"Added: {bee.GetHashCode()}, Count: {Entities.Count}");
    }

    public void Remove(IRenderableBee bee)
    {
        var removed = Entities.Remove(bee);
        //Logger.Trace($"Removed: {bee.GetHashCode()} Removed? {removed}, Count: {Entities.Count}");
    }
}
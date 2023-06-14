using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;

namespace Pong;

public class SpriteRenderer : ISpriteRenderer
{
    private const int MAX_BATCH_SIZE = 512;
    
    private IGpu Gpu { get; }
    private IHandle<IGpuShader>? ShaderHandle { get; set; }
    private IHandle<IGpuMesh>? MeshHandle { get; set; }
    private Dictionary<IGpuTextureHandle, SpriteBatch> Batches { get; } = new();
    
    private int m_Size = 0;
    private readonly Vector3[] m_Colors = new Vector3[MAX_BATCH_SIZE];
    private readonly Matrix4x4[] m_ModelMatrices = new Matrix4x4[MAX_BATCH_SIZE];
    
    private IHandle<IBuffer> ColorsBuffer { get; set; }
    private IHandle<IBuffer> ModelMatricesBuffer { get; set; }
    // private IHandle<IPipeline> Pipeline { get; set; }

    public SpriteRenderer(IWindow window)
    {
        Gpu = window.Gpu;
    }

    public void LoadResources()
    {
        var gpu = Gpu;
        var shaderController = gpu.ShaderController;
        var bufferController = gpu.BufferController;
        
        ShaderHandle = shaderController.Load("Assets/sprite");

        ModelMatricesBuffer = bufferController.CreateAndBind(
            BufferKind.UniformBuffer, 
            BufferUsage.DynamicDraw, 
            16 * sizeof(float) * MAX_BATCH_SIZE);

        shaderController.AttachBuffer("modelMatricesBlock", 0, ModelMatricesBuffer);
        
        ColorsBuffer = bufferController.CreateAndBind(
            BufferKind.UniformBuffer,
            BufferUsage.DynamicDraw,
            4 * sizeof(float) * MAX_BATCH_SIZE);
        
        shaderController.AttachBuffer("colorsBlock", 1, ColorsBuffer);
        
        MeshHandle = gpu.MeshController.Load("Assets/quad");
    }
    
    public void NewBatch()
    {
        m_Size = 0;
        foreach (var batch in Batches.Values)
            batch.Clear();
    }

    public void DrawSprite(Vector2 position, Vector2 scale, Sprite sprite, Vector3 tint)
    {
        var spriteSheet = sprite.Texture;

        if (spriteSheet == null)
        {
            var color = tint;
            var pivot = sprite.Pivot;
            var modelMatrix = Matrix4x4.CreateScale(scale.X, 0f, scale.Y)
                              * Matrix4x4.CreateTranslation(position.X + pivot.X, position.Y + pivot.Y, 0f);

            m_Colors[m_Size] = color;
            m_ModelMatrices[m_Size] = modelMatrix;
            m_Size++;
        }
        else
        {
            if (!Batches.TryGetValue(spriteSheet, out var batch))
            {
                batch = new SpriteBatch();
                Batches[spriteSheet] = batch;
            }
            batch.Add(position, scale, sprite, tint);
        }
    }

    public void RenderBatch(ICamera camera)
    {
        Debug.Assert(ShaderHandle != null);
        Debug.Assert(MeshHandle != null);

        var shaderController = Gpu.ShaderController;
        var meshController = Gpu.MeshController;
        var bufferController = Gpu.BufferController;
        
        shaderController.Bind(ShaderHandle);
        meshController.Bind(MeshHandle);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        shaderController.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shaderController.SetMatrix4x4("matrix_view", viewMatrix);
        
        foreach (var (spriteSheetHandle, batch) in Batches)
        {
            bufferController.Bind(ColorsBuffer);
            bufferController.Upload(batch.Colors);
            
            bufferController.Bind(ModelMatricesBuffer);
            bufferController.Upload(batch.ModelMatrices);
            
            meshController.RenderInstanced(batch.Size);
        }

    }

}
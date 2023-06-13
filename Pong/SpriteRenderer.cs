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
    
    private IBuffer ColorsBuffer { get; set; }
    private IHandle<IBuffer> ModelMatricesBuffer { get; set; }
    // private IHandle<IPipeline> Pipeline { get; set; }

    public SpriteRenderer(IWindow window)
    {
        Gpu = window.Gpu;
    }

    public void LoadResources()
    {
        var gpu = Gpu;
        var bufferController = gpu.BufferController;
        
        ModelMatricesBuffer = bufferController.CreateAndBind(
            BufferKind.UniformBuffer, 
            BufferUsage.DynamicDraw, 
            16 * sizeof(float) * MAX_BATCH_SIZE);
        
        // var bufferController = gpu.BufferController;
        // var pipelineController = gpu.PipelineController;
        //
        // ICpuMesh mesh = null;
        // var vertices = mesh.Vertices.AsSpan();
        
        // Pipeline = gpu.CreatePipeline();
        //
        // var vertexBuffer = gpu.CreateBuffer(
        //     BufferKind.ArrayBuffer, 
        //     BufferUsage.DynamicDraw,
        //     vertices.Length * sizeof(float)
        // );
        // bufferController.Bind(vertexBuffer);
        // bufferController.Put<float>(vertices);
        // bufferController.Write();
        //
        // pipelineController.Bind(Pipeline);
        // pipelineController.AttachBuffer(0, vertexBuffer);
        
        //gpu.Shader.Bind();
        //gpu.Shader.AttachBuffer(0, vertexBuffer);

        ShaderHandle = gpu.Shader.Load("Assets/sprite");
        MeshHandle = gpu.Mesh.Load("Assets/quad");
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

        var shaderController = Gpu.Shader;
        var meshController = Gpu.Mesh;
        var bufferController = Gpu.BufferController;
        
        shaderController.Bind(ShaderHandle);
        shaderController.AttachBuffer("test", 0, ModelMatricesBuffer);
        
        meshController.Bind(MeshHandle);

        Matrix4x4.Invert(camera.Transform.WorldMatrix, out var viewMatrix);
        shaderController.SetMatrix4x4("matrix_projection", camera.ProjectionMatrix);
        shaderController.SetMatrix4x4("matrix_view", viewMatrix);
        
        foreach (var (spriteSheetHandle, batch) in Batches)
        {
            shaderController.SetTexture2d("sprite_sheet", spriteSheetHandle);
            shaderController.SetVector2("texture_size", new Vector2(spriteSheetHandle.Width, spriteSheetHandle.Height));
            shaderController.SetVector2Array("offsets", batch.Offsets);
            shaderController.SetVector2Array("sizes", batch.Sizes);
            shaderController.SetVector3Array("colors", batch.Colors);
            shaderController.SetMatrix4x4Array("model_matrices", batch.ModelMatrices);
            
            bufferController.Bind(ModelMatricesBuffer);
            bufferController.Put(batch.ModelMatrices);
            bufferController.Upload();
            
            meshController.RenderInstanced(batch.Size);
        }

    }

}
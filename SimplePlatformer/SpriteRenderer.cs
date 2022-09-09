using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using SimplePlatformer;

namespace SampleGames;

public class SpriteBatch
{
    private const int MAX_BATCH_SIZE = 512;

    private int m_Size = 0;
    private readonly Vector2[] m_Offsets = new Vector2[MAX_BATCH_SIZE];
    private readonly Vector2[] m_Sizes = new Vector2[MAX_BATCH_SIZE];
    private readonly Vector3[] m_Colors = new Vector3[MAX_BATCH_SIZE];
    private readonly Matrix4x4[] m_ModelMatrices = new Matrix4x4[MAX_BATCH_SIZE];

    public ReadOnlySpan<Vector3> Colors => m_Colors;
    public ReadOnlySpan<Matrix4x4> ModelMatrices => m_ModelMatrices;
    public int Size => m_Size;
    public ReadOnlySpan<Vector2> Sizes => m_Sizes;
    public ReadOnlySpan<Vector2> Offsets => m_Offsets;

    public void Add(Vector2 position, Sprite sprite)
    {
        var offset = sprite.Offset;
        var size = sprite.Size;
        var color = sprite.Color;
        var pivot = sprite.Pivot;
        var modelMatrix = Matrix4x4.CreateScale(0.5f)
                          * Matrix4x4.CreateTranslation(position.X + pivot.X, position.Y + pivot.Y, 0f);

        m_Offsets[m_Size] = offset;
        m_Sizes[m_Size] = size;
        m_Colors[m_Size] = color;
        m_ModelMatrices[m_Size] = modelMatrix;
        
        m_Size++;
    }
    
    public void Clear()
    {
        m_Size = 0;
    }
}

public class SpriteRenderer
{
    private const int MAX_BATCH_SIZE = 512;
    
    private IGpu Gpu { get; }
    private IHandle<IGpuShader>? ShaderHandle { get; set; }
    private IHandle<IGpuMesh>? MeshHandle { get; set; }
    private Dictionary<IGpuTextureHandle, SpriteBatch> Batches { get; } = new();
    
    private int m_Size = 0;
    private readonly Vector3[] m_Colors = new Vector3[MAX_BATCH_SIZE];
    private readonly Matrix4x4[] m_ModelMatrices = new Matrix4x4[MAX_BATCH_SIZE];
    
    public SpriteRenderer(IGpu gpu)
    {
        Gpu = gpu;
    }

    public void LoadResources()
    {
        ShaderHandle = Gpu.Shader.Load("Assets/sprite");
        MeshHandle = Gpu.Mesh.Load("Assets/quad");
    }
    
    public void NewBatch()
    {
        m_Size = 0;
        foreach (var batch in Batches.Values)
            batch.Clear();
    }

    public void DrawSprite(Vector2 position, Sprite sprite)
    {
        var spriteSheet = sprite.SpriteSheet;

        if (spriteSheet == null)
        {
            var color = sprite.Color;
            var pivot = sprite.Pivot;
            var modelMatrix = Matrix4x4.CreateScale(0.5f)
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

            batch.Add(position, sprite);
        }
    
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
        
        foreach (var (spriteSheetHandle, batch) in Batches)
        {
            shader.SetTexture2d("sprite_sheet", spriteSheetHandle);
            shader.SetVector2("texture_size", new Vector2(spriteSheetHandle.Width, spriteSheetHandle.Height));
            shader.SetVector2Array("offsets", batch.Offsets);
            shader.SetVector2Array("sizes", batch.Sizes);
            shader.SetVector3Array("colors", batch.Colors);
            shader.SetMatrix4x4Array("model_matrices", batch.ModelMatrices);
            mesh.RenderInstanced(batch.Size);
        }

    }

}
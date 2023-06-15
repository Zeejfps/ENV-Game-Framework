using System.Numerics;

namespace Pong;

public class SpriteBatch
{
    private const int MAX_BATCH_SIZE = 2000;

    private int m_Size = 0;
    private readonly Vector2[] m_Offsets = new Vector2[MAX_BATCH_SIZE];
    private readonly Vector2[] m_Sizes = new Vector2[MAX_BATCH_SIZE];
    private readonly Vector4[] m_Colors = new Vector4[MAX_BATCH_SIZE];
    private readonly Matrix4x4[] m_ModelMatrices = new Matrix4x4[MAX_BATCH_SIZE];

    public ReadOnlySpan<Vector4> Colors => m_Colors;
    public ReadOnlySpan<Matrix4x4> ModelMatrices => m_ModelMatrices;
    public int Size => m_Size;
    public ReadOnlySpan<Vector2> Sizes => m_Sizes;
    public ReadOnlySpan<Vector2> Offsets => m_Offsets;

    public void Add(Vector2 position, Vector2 scale, Sprite sprite, Vector3 tint)
    {
        var offset = sprite.Offset;
        var size = sprite.Size;
        var color = tint;
        var pivot = sprite.Pivot;

        var scaleX = sprite.FlipX ? -scale.X : scale.X;
        var scaleY = scale.Y;
        var modelMatrix = Matrix4x4.CreateScale(scaleX, scaleY, 0f)
                          * Matrix4x4.CreateTranslation(position.X + pivot.X, position.Y + pivot.Y, 0f);

        m_Offsets[m_Size] = offset;
        m_Sizes[m_Size] = size;
        m_Colors[m_Size] = new Vector4(tint, 1f);
        m_ModelMatrices[m_Size] = modelMatrix;
        
        m_Size++;
    }
    
    public void Clear()
    {
        m_Size = 0;
    }
}
using System.Numerics;

public sealed class RendererTest
{
    public void Execute()
    {
        var renderer = new MeshRenderer();
        
        var meshHandle = renderer.Upload(new Triangle());
        
        renderer.Render(meshHandle);
    }
}

public class Triangle : IMesh<Vertex>
{
    public ReadOnlySpan<Vertex> Vertices => m_Vertices;
    
    private readonly Vertex[] m_Vertices;

    public Triangle()
    {
        m_Vertices =
        [
            new Vertex
            {
                Position = new Vector2(-1f, 1f),
                UVs = new Vector2(0, 1f)
            },
            new Vertex
            {
                Position = new Vector2(-1f, -1f),
                UVs = new Vector2(0, 0)
            },
            new Vertex
            {
                Position = new Vector2(1f, -1f),
                UVs = new Vector2(1f, 0f)
            }
        ];
    }
}
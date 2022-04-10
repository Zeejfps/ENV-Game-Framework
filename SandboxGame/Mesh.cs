using ENV.Engine;

namespace ENV;

public interface IMesh : IAsset
{
    float[] Vertices { get; }
    float[] Uvs { get; set; }
    float[] Normals { get; }
    int[] Triangles { get; }
}

public class Mesh : IMesh
{
    public float[] Vertices { get; set; }
    public float[] Uvs { get; set; }
    public float[] Normals { get; set; }
    public int[] Triangles { get; set; }

    public bool IsLoaded { get; private set; }

    public Mesh()
    {
        IsLoaded = true;
    }
    
    public void Unload()
    {
        IsLoaded = false;
    }
}
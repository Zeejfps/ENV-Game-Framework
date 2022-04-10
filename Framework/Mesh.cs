namespace ENV;

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
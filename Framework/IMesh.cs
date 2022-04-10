using Framework;

namespace Framework;

public interface IMesh : IAsset
{
    float[] Vertices { get; }
    float[] Uvs { get; set; }
    float[] Normals { get; }
    int[] Triangles { get; }
}
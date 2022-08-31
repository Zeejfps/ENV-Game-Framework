namespace EasyGameFramework.Api.AssetTypes;

public interface ICpuMesh : IAsset
{
    public float[] Vertices { get; set; }
    public float[] Uvs { get; set; }
    public float[] Normals { get; set; }
    public float[] Tangents { get; set; }
    public int[] Triangles { get; set; }
}
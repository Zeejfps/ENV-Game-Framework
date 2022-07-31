using Framework.GLFW.NET;

namespace Framework;

public class GpuMeshAssetLoader_GL : IAssetLoader<IGpuMesh>
{
    private readonly CpuMeshAssetLoader m_CpuMeshAssetLoader = new();

    public IGpuMesh Load(string assetPath)
    {
        using var asset = m_CpuMeshAssetLoader.Load(assetPath);
        var mesh = new Mesh_GL(asset.Vertices, asset.Normals, asset.Uvs, asset.Tangents, asset.Triangles);
        return mesh;
    }
}
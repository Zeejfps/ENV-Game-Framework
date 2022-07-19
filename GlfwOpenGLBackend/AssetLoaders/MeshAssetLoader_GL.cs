using Framework.Assets;
using Framework.GLFW.NET;

namespace Framework;

public class MeshAssetLoader_GL : IAssetLoader<IMesh>
{
    public IAsset LoadAsset(string assetPath)
    {
        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);

        using var meshAsset = MeshAsset.Deserialize(reader);
        var mesh = new Mesh_GL(meshAsset.Vertices, meshAsset.Normals, meshAsset.Uvs, meshAsset.Tangents, meshAsset.Triangles);
        return mesh;
    }
}
using Framework.Assets;

namespace Framework;

public class Mesh_GL
{
    
}

public class MeshAssetLoader : IAssetLoader<IMesh>
{
    public IAsset LoadAsset(string assetPath)
    {
        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);

        var meshAsset = MeshAsset_GL.Deserialize(reader);

        return new Mesh
        {
            Vertices = meshAsset.Vertices,
            Normals = meshAsset.Normals,
            Triangles = meshAsset.Triangles,
            Uvs = meshAsset.Uvs,
            Tangents = meshAsset.Tangents,
        };
    }
}
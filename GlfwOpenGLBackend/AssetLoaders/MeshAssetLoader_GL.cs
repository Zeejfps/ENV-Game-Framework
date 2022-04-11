using Framework.Assets;
using Framework.GLFW.NET;

namespace Framework;

public class MeshAssetLoader_GL : IAssetLoader<IMesh>
{
    private readonly Dictionary<string, IMesh> m_PathToAssetMap = new Dictionary<string, IMesh>();

    public IAsset LoadAsset(string assetPath)
    {
        if (m_PathToAssetMap.TryGetValue(assetPath, out var mesh))
            return mesh;
        
        using var stream = File.Open(assetPath, FileMode.Open);
        using var reader = new BinaryReader(stream);

        using var meshAsset = MeshAsset_GL.Deserialize(reader);
        mesh = new Mesh_GL(meshAsset.Vertices, meshAsset.Normals, meshAsset.Uvs, meshAsset.Tangents, meshAsset.Triangles);
        m_PathToAssetMap[assetPath] = mesh;
        return mesh;
    }
}
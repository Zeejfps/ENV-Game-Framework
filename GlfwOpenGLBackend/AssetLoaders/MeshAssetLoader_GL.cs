using Framework.Assets;
using Framework.GLFW.NET;

namespace Framework;

public class MeshAssetLoader_GL : MeshAssetLoader
{
    protected override IMesh LoadAsset(MeshAsset asset)
    {
        var mesh = new Mesh_GL(asset.Vertices, asset.Normals, asset.Uvs, asset.Tangents, asset.Triangles);
        return mesh;
    }
}
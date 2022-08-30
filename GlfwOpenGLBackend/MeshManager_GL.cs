using System.Diagnostics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.AssetManagement;
using Framework.GLFW.NET;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class MeshManager_GL : GpuResourceManager<IHandle<IGpuMesh>, Mesh_GL>, IMeshManager
{
    private readonly CpuMeshAssetLoader m_CpuMeshLoader = new();

    protected override void OnBound(Mesh_GL resource)
    {
        glBindVertexArray(resource.VaoId);
    }

    protected override void OnUnbound()
    {
        glBindVertexArray(0);
    }

    protected override IHandle<IGpuMesh> CreateHandle(Mesh_GL resource)
    {
        return new GpuMeshHandle(resource);
    }

    protected override Mesh_GL LoadResource(string assetPath)
    {
        var cpuMesh = m_CpuMeshLoader.Load(assetPath);
        return new Mesh_GL(cpuMesh.Vertices, cpuMesh.Normals, cpuMesh.Uvs, cpuMesh.Tangents,
            cpuMesh.Triangles);
    }

    public void Render()
    {
        Debug.Assert(BoundResource != null);
        BoundResource.Render();
    }

    public void RenderInstanced(int count)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.RenderInstanced(count);
    }
}
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

    public IHandle<IGpuMesh> Load(string assetPath)
    {
        var cpuMesh = m_CpuMeshLoader.Load(assetPath);
        var gpuMesh = new Mesh_GL(cpuMesh.Vertices, cpuMesh.Normals, cpuMesh.Uvs, cpuMesh.Tangents,
            cpuMesh.Triangles);
        var handle = new GpuMeshHandle(gpuMesh);
        Add(handle, gpuMesh);
        BoundResource = gpuMesh;
        return handle;
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
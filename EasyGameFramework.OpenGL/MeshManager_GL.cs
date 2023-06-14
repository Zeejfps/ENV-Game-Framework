using System.Diagnostics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using EasyGameFramework.Core;
using EasyGameFramework.Core.AssetLoaders;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal class MeshManager_GL : GpuResourceManager<IHandle<IGpuMesh>, Mesh_GL>, IMeshController
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

    protected override Mesh_GL LoadAndBindResource(string assetPath)
    {
        var cpuMesh = m_CpuMeshLoader.Load(assetPath);
        return new Mesh_GL(cpuMesh.Vertices, cpuMesh.Normals, cpuMesh.Uvs, cpuMesh.Tangents,
            cpuMesh.Triangles);
    }

    public IHandle<IGpuMesh> CreateAndBind(CpuMesh mesh)
    {
        var resource = new Mesh_GL(mesh.Vertices, mesh.Normals, mesh.Uvs, mesh.Tangents,
            mesh.Triangles);
        var handle = new GpuMeshHandle(resource);
        Add(handle, resource);
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
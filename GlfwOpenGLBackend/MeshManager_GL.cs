using System.Diagnostics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using Framework.GLFW.NET;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class MeshManager_GL : GpuResourceManager<IHandle<IGpuMesh>, Mesh_GL>, IMeshManager
{
    protected override void OnBound(Mesh_GL resource)
    {
        glBindVertexArray(resource.VaoId);
    }

    protected override void OnUnbound()
    {
        glBindVertexArray(0);
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
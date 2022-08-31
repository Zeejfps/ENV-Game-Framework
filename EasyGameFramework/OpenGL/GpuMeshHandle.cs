using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using Framework.GLFW.NET;
using OpenGL;

namespace GlfwOpenGLBackend;

public class GpuMeshHandle : IHandle<IGpuMesh>
{
    private readonly Mesh_GL m_MeshGl;

    public GpuMeshHandle(Mesh_GL meshGl)
    {
        m_MeshGl = meshGl;
    }

    public IGpuMesh Use()
    {
        Gl.glBindVertexArray(m_MeshGl.VaoId);
        return m_MeshGl;
    }
}
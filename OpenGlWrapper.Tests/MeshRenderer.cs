
using OpenGlWrapper;
using OpenGlWrapper.Buffers;

public interface IMesh<TVertex> where TVertex : unmanaged
{
    public ReadOnlySpan<TVertex> Vertices { get; }
}

public interface IMeshHandle
{
    
}

public interface IMaterialHandle
{
    
}

public sealed class MeshRenderer
{
    private readonly OpenGlContext m_OpenGlContext;
    
    public IMeshHandle Upload<TVertex>(IMesh<TVertex> mesh) where TVertex : unmanaged
    {
        var context = m_OpenGlContext;
        var vaoManager = context.VertexArrayObjectManager;
        var vboManager = context.ArrayBufferManager;

        var vbo = vboManager.CreateAndBind();
        vboManager.AllocFixedSizedAndUploadData(mesh.Vertices, FixedSizedBufferAccessFlag.Read);

        var vertexTemplate = vaoManager.CreateTemplate<TVertex>();

        var vao = vaoManager.CreateAndBind();
        vaoManager.EnableAndBindAttribsFromTemplate(vertexTemplate, vbo);

        return new OpenGlMeshHandle(vao);
    }

    public void Render(IMeshHandle meshHandle)
    {
        if (meshHandle is not OpenGlMeshHandle openGlMeshHandle)
            throw new ArgumentException("mesh handle was not created by this mesh renderer");
        
        var context = m_OpenGlContext;
        var vaoManager = context.VertexArrayObjectManager;
        
        vaoManager.Bind(openGlMeshHandle.Vao);
    }
    
}

sealed class OpenGlMeshHandle : IMeshHandle
{
    public VertexArrayObjectId Vao { get; }

    public OpenGlMeshHandle(VertexArrayObjectId vao)
    {
        Vao = vao;
    }
}
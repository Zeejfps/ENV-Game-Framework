using ENV.Engine;
using static OpenGL.Gl;

namespace ENV.GLFW.NET;

class CommandBuffer_GL : ICommandBuffer
{
    private readonly Dictionary<string, ShaderProgram_GL> m_ShaderToProgramMap = new();
    private readonly Dictionary<IMesh, VaoRenderMesh_GL> m_MeshToRenderMeshMap = new();

    public void SubmitClearCommand()
    { 
        glClearColor(1f, 0f, 1f, 1f);
        glClear(GL_COLOR_BUFFER_BIT);
    }

    public void SubmitDrawCommand(IMesh mesh, IMaterial material)
    {
        var renderMesh = LoadMesh(mesh);
        var shaderProgram = LoadShaderProgram(material);
        
        shaderProgram.Use();
        renderMesh.Render();
    }

    private VaoRenderMesh_GL LoadMesh(IMesh mesh)
    {
        if (!m_MeshToRenderMeshMap.TryGetValue(mesh, out var renderMesh))
        {
            renderMesh = new VaoRenderMesh_GL(mesh);
            m_MeshToRenderMeshMap[mesh] = renderMesh;
        }

        return renderMesh;
    }

    private ShaderProgram_GL LoadShaderProgram(IMaterial material)
    {
        var shader = material.Shader;
        
        if (!m_ShaderToProgramMap.TryGetValue(shader, out var shaderProgram))
        {
            shaderProgram = new ShaderProgram_GL(shader);
            m_ShaderToProgramMap[shader] = shaderProgram;
        }

        material.Apply(shaderProgram);
        return shaderProgram;
    }
}
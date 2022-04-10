using Framework;
using OpenGL;
using static OpenGL.Gl;

namespace Framework.GLFW.NET;

public class MainFramebuffer_GL : IFramebuffer
{
    private readonly Dictionary<string, ShaderProgram_GL> m_ShaderToProgramMap = new();
    private readonly Dictionary<IMesh, IRenderMesh> m_MeshToRenderMeshMap = new();

    public int Width { get; private set; }
    public int Height { get; private set; }
    
    public void Init(int width, int height, GetProcAddressHandler getProcAddress)
    {
        Width = width;
        Height = height;
        Import(getProcAddress);
        glEnable(GL_CULL_FACE);
        glEnable(GL_DEPTH_TEST);
    }

    public void Clear()
    {
        glClearColor(.1f, .1f, .2f, 1f);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    }

    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;
        glViewport(0, 0, width, height);
    }

    public void RenderMesh(IMesh mesh, IMaterial material)
    {
        var renderMesh = LoadMesh(mesh);
        var shaderProgram = LoadShaderProgram(material);
        
        shaderProgram.Use();
        material.Apply(shaderProgram);
        renderMesh.Render();
    }
    
    private IRenderMesh LoadMesh(IMesh mesh)
    {
        if (!m_MeshToRenderMeshMap.TryGetValue(mesh, out var renderMesh))
        {
            if (mesh.Triangles != null && mesh.Triangles.Length > 0)
            {
                renderMesh = new IndexedRenderMesh_GL(mesh);
            }
            else
            {
                renderMesh = new VaoRenderMesh_GL(mesh);
            }
            
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

        return shaderProgram;
    }
}
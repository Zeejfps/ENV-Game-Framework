using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class GpuShaderHandle : IHandle<IGpuShader>
{
    private readonly Shader_GL m_Shader;
    
    public GpuShaderHandle(Shader_GL shader)
    {
        m_Shader = shader;
    }
    
    public IGpuShader Use()
    {
        glUseProgram(m_Shader.ProgramId);
        return m_Shader;
    }
}
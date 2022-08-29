using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using OpenGL;

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
        Gl.glUseProgram(m_Shader.ProgramId);
        return m_Shader;
    }
}
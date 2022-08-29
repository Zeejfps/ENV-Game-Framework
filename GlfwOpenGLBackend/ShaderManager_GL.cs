using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class ShaderManager_GL : IShaderManager
{
    private IGpuShader? m_ActiveShader;
    private readonly Dictionary<IHandle<IGpuShader>, Shader_GL> m_HandleToShaderMap = new();
    
    public void UseShader(IHandle<IGpuShader>? handle)
    {
        if (handle == null)
        {
            glUseProgram(0);
            return;
        }
        
        var shader = m_HandleToShaderMap[handle];
        m_ActiveShader = shader;
        glUseProgram(shader.Id);
    }

    public void SetFloat(string propertyName, float value)
    {
        Debug.Assert(m_ActiveShader != null);
        m_ActiveShader.SetFloat(propertyName, value);
    }

    public void SetVector3(string propertyName, float x, float y, float z)
    {
        Debug.Assert(m_ActiveShader != null);
        m_ActiveShader.SetVector3(propertyName, x, y, z);
    }

    public void SetVector3(string propertyName, Vector3 value)
    {
        Debug.Assert(m_ActiveShader != null);
        m_ActiveShader.SetVector3(propertyName, value);
    }

    public void SetTexture2d(string propertyName, IHandle<IGpuTexture> value)
    {
        Debug.Assert(m_ActiveShader != null);
        m_ActiveShader.SetTexture2d(propertyName, value);
    }

    public void SetMatrix4x4(string propertyName, Matrix4x4 value)
    {
        Debug.Assert(m_ActiveShader != null);
        m_ActiveShader.SetMatrix4x4(propertyName, value);
    }

    public IBuffer GetBuffer(string name)
    {
        Debug.Assert(m_ActiveShader != null);
        return m_ActiveShader.GetBuffer(name);
    }

    public void Add(IHandle<IGpuShader> handle, Shader_GL gpuShader)
    {
        m_HandleToShaderMap[handle] = gpuShader;
    }
}
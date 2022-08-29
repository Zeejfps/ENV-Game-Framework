using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class ShaderManager_GL : GpuResourceManager<IHandle<IGpuShader>, Shader_GL>, IShaderManager
{
    public void SetFloat(string propertyName, float value)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetFloat(propertyName, value);
    }

    public void SetVector3(string propertyName, float x, float y, float z)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetVector3(propertyName, x, y, z);
    }

    public void SetVector3(string propertyName, Vector3 value)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetVector3(propertyName, value);
    }

    public void SetTexture2d(string propertyName, IHandle<IGpuTexture> value)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetTexture2d(propertyName, value);
    }

    public void SetMatrix4x4(string propertyName, Matrix4x4 value)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetMatrix4x4(propertyName, value);
    }

    public IBuffer GetBuffer(string name)
    {
        Debug.Assert(BoundResource != null);
        return BoundResource.GetBuffer(name);
    }

    protected override void OnBound(Shader_GL resource)
    {
        glUseProgram(resource.Id);
    }

    protected override void OnUnbound()
    {
        glUseProgram(0);
    }
}
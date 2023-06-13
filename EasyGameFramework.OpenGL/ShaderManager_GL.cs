using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using EasyGameFramework.Core.AssetLoaders;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal class ShaderManager_GL : GpuResourceManager<IHandle<IGpuShader>, Shader_GL>, IShaderManager
{
    private readonly CpuShaderAssetLoader m_CpuShaderLoader = new();

    private readonly ITextureManager m_Texture;

    public ShaderManager_GL(ITextureManager texture)
    {
        m_Texture = texture;
    }

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

    public void SetVector2(string propertyName, Vector2 value)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetVector2(propertyName, value);
    }

    public void SetVector2Array(string uniformName, ReadOnlySpan<Vector2> array)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetVector2Array(uniformName, array);
    }

    public void SetVector3(string propertyName, Vector3 value)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetVector3(propertyName, value);
    }

    public void SetVector3Array(string uniformName, ReadOnlySpan<Vector3> array)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetVector3Array(uniformName, array);
    }

    public void SetTexture2d(string propertyName, IGpuTextureHandle value)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetTexture2d(propertyName, value);
    }

    public void SetMatrix4x4(string propertyName, Matrix4x4 value)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetMatrix4x4(propertyName, value);
    }

    public void SetMatrix4x4Array(string uniformName, ReadOnlySpan<Matrix4x4> array)
    {
        Debug.Assert(BoundResource != null);
        BoundResource.SetMatrix4x4Array(uniformName, array);
    }

    public IBufferHandle GetBuffer(string name)
    {
        Debug.Assert(BoundResource != null);
        return BoundResource.GetBuffer(name);
    }

    public void AttachBuffer(string name, uint bindingPoint, IHandle<IBuffer> handle)
    {
        Debug.Assert(BoundResource != null);
        var buffer = (BufferHandle)handle;
        BoundResource.AttachBuffer(name, bindingPoint, buffer.Id);
    }

    protected override void OnBound(Shader_GL resource)
    {
        glUseProgram(resource.Id);
    }

    protected override void OnUnbound()
    {
        glUseProgram(0);
    }

    protected override Shader_GL LoadAndBindResource(string assetPath)
    {
        var cpuShader = m_CpuShaderLoader.Load(assetPath);
        var gpuShader = Shader_GL.LoadFromSource(cpuShader.VertexShader, cpuShader.FragmentShader, m_Texture);

        glUseProgram(gpuShader.Id);
        glAssertNoError();
        
        return gpuShader;
    }

    protected override IHandle<IGpuShader> CreateHandle(Shader_GL resource)
    {
        return new GpuShaderHandle(resource);
    }
}
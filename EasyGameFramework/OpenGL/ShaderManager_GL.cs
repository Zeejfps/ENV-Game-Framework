﻿using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

public class ShaderManager_GL : GpuResourceManager<IHandle<IGpuShader>, Shader_GL>, IShaderManager
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

    protected override Shader_GL LoadResource(string assetPath)
    {
        var cpuShader = m_CpuShaderLoader.Load(assetPath);
        return Shader_GL.LoadFromSource(cpuShader.VertexShader, cpuShader.FragmentShader, m_Texture);
    }

    protected override IHandle<IGpuShader> CreateHandle(Shader_GL resource)
    {
        return new GpuShaderHandle(resource);
    }
}
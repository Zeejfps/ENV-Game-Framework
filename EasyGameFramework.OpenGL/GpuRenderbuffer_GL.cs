﻿using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal class GpuRenderbuffer_GL : IGpuRenderbuffer
{
    private readonly TextureManager_GL m_TextureManager;
    private readonly int[] m_drawBufferIds;

    public GpuRenderbuffer_GL(TextureManager_GL textureManager, int width, int height, int colorBufferCount,
        bool createDepthBuffer)
    {
        m_TextureManager = textureManager;

        Width = width;
        Height = height;

        Id = glGenFramebuffer();
        glBindFramebuffer(Id);

        ColorBuffers = new IGpuTextureHandle[colorBufferCount];
        m_drawBufferIds = new int[colorBufferCount];
        
        // TODO: These can potentially be created in one go?
        for (var i = 0; i < colorBufferCount; i++)
        {
            var colorTextureId = glGenTexture();
            glBindTexture(GL_TEXTURE_2D, colorTextureId);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA16F, width, height, 0, GL_RGBA, GL_FLOAT, IntPtr.Zero);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0 + i, GL_TEXTURE_2D, colorTextureId, 0);

            var texture = new Texture2D_GL(colorTextureId, width, height);
            var handle = new GpuTextureHandle(texture);
            ColorBuffers[i] = handle;
            m_drawBufferIds[i] = GL_COLOR_ATTACHMENT0 + i;
            m_TextureManager.Add(handle, texture);
        }

        if (m_drawBufferIds.Length > 0)
            glDrawBuffers(m_drawBufferIds);

        if (createDepthBuffer)
        {
            var depthTextureId = glGenTexture();
            glBindTexture(GL_TEXTURE_2D, depthTextureId);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH24_STENCIL8, width, height, 0, GL_DEPTH_STENCIL,
                GL_UNSIGNED_INT_24_8, IntPtr.Zero);
            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_TEXTURE_2D, depthTextureId, 0);
            var texture = new Texture2D_GL(depthTextureId, width, height);
            var handle = new GpuTextureHandle(texture);
            m_TextureManager.Add(handle, texture);
            DepthBuffer = handle;
        }

        if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
            throw new Exception("Failed to create framebuffer");

        glBindFramebuffer(0);
    }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public IGpuTextureHandle[] ColorBuffers { get; }

    public IGpuTextureHandle? DepthBuffer { get; }

    public uint Id { get; }

    public void Clear(float r, float g, float b, float a)
    {
        glClearColor(r, g, b, a);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    }

    public void SetSize(int width, int height)
    {
        if (Width == width && Height == height)
            return;

        Width = width;
        Height = height;

        foreach (var colorBuffer in ColorBuffers)
        {
            m_TextureManager.Bind(colorBuffer);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA16F, width, height, 0, GL_RGBA, GL_FLOAT, IntPtr.Zero);
        }

        m_TextureManager.Bind(DepthBuffer);
        glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, IntPtr.Zero);
    }

    public void Dispose()
    {
    }
}
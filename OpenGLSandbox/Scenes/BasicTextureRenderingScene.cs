using System.Numerics;
using System.Runtime.InteropServices;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

[StructLayout(LayoutKind.Sequential)]
struct Vertex
{
    public Vector2 Position;
    public Vector2 TexCoords;
}

[StructLayout(LayoutKind.Sequential)]
struct Triangle
{
    public Vertex V1;
    public Vertex V2;
    public Vertex V3;
}

[StructLayout(LayoutKind.Sequential)]
struct Quad
{
    public readonly Triangle T1;
    public readonly Triangle T2;

    public Quad()
    {
        T1 = new Triangle
        {
            V1 =
            {
                Position = new Vector2(-1f, -1f),
                TexCoords = new Vector2(0f, 0f)
            },
            V2 =
            {
                Position = new Vector2(1f, -1f),
                TexCoords = new Vector2(1f, 0f)
            },
            V3 =
            {
                Position = new Vector2(-1f, 1f),
                TexCoords = new Vector2(0f, 1f)
            }
        };

        T2 = new Triangle
        {
            V1 =
            {
                Position = new Vector2(1f, -1f),
                TexCoords = new Vector2(1f, 0f)
            },
            V2 =
            {
                Position = new Vector2(1f, 1f),
                TexCoords = new Vector2(1f, 1f)
            },
            V3 =
            {
                Position = new Vector2(-1f, 1f),
                TexCoords = new Vector2(0f, 1f)
            }
        };
    }
}

public unsafe class BasicTextureRenderingScene : IScene
{
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_TextureId;
    private uint m_ShaderProgramId;
    private readonly IAssetLoader<ICpuTexture> m_ImageLoader;

    public BasicTextureRenderingScene(IAssetLoader<ICpuTexture> loader)
    {
        m_ImageLoader = loader;
    }

    private uint getGenBuffer()
    {
        uint bufferId;
        glGenBuffers(1, &bufferId);
        AssertNoGlError();
        return bufferId;
    }

    private uint glGenVertexArray()
    {
        uint vaoId;
        glGenVertexArrays(1, &vaoId);
        return vaoId;
    }
    
    public void Load()
    {
        var image = m_ImageLoader.Load("Assets/lol");

        m_Vao = glGenVertexArray();
        glBindVertexArray(m_Vao);
        
        m_Vbo = getGenBuffer();
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);

        var quad = new Quad();
        glBufferData(GL_ARRAY_BUFFER, new IntPtr(sizeof(Quad)), &quad, GL_STATIC_DRAW);
        
        glVertexAttribPointer(0, 2, GL_FLOAT, false, sizeof(Vertex), Offset(0));
        glEnableVertexAttribArray(0);
        
        glVertexAttribPointer(1, 2, GL_FLOAT, false, sizeof(Vertex), Offset<Vertex>(nameof(Vertex.TexCoords)));
        glEnableVertexAttribArray(1);
        
        uint textureId;
        glGenTextures(1, &textureId);
        AssertNoGlError();

        m_TextureId = textureId;
        
        m_ShaderProgramId = new ShaderProgramBuilder()
            .WithVertexShader("Assets/tex.vert.glsl")
            .WithFragmentShader("Assets/tex.frag.glsl")
            .Build();
        
        glUseProgram(m_ShaderProgramId);
        
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_2D, textureId);
        AssertNoGlError();

        var pixels = image.Pixels;
        fixed (byte* ptr = &pixels[0])
        {
            var width = image.Width;
            var height = image.Height;
            glCompressedTexImage2D(GL_TEXTURE_2D, 0, GL_COMPRESSED_RGBA_BPTC_UNORM, width, height, 0,
                pixels.Length, ptr);
            //glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, 100, 100, 0, GL_COMPRESSED_RGB8_ETC2, GL_UNSIGNED_BYTE, ptr);
            AssertNoGlError();
        }
        
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        glDrawArrays(GL_TRIANGLES, 0, 6);
        glFlush();
    }

    public void Unload()
    {
        fixed (uint* ptr = &m_Vbo)
            glDeleteBuffers(1, ptr);
        
        fixed (uint* ptr = &m_TextureId)
            glDeleteTextures(1, ptr);
    }
}
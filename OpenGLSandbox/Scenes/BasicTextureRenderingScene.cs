using System.Diagnostics;
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
readonly struct Quad
{
    public readonly Triangle T1 = new()
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
    
    public readonly Triangle T2 = new()
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

    public Quad(){}
}

public unsafe class BasicTextureRenderingScene : IScene
{
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_TextureId;
    private uint m_PixelUnpackBufferId;
    private uint m_ShaderProgramId;
    private readonly IAssetLoader<ICpuTexture> m_ImageLoader;

    public BasicTextureRenderingScene(IAssetLoader<ICpuTexture> loader)
    {
        m_ImageLoader = loader;
    }

    private uint glGenBuffer()
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
        m_Vao = glGenVertexArray();
        glBindVertexArray(m_Vao);
        
        m_Vbo = glGenBuffer();
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
        AssertNoGlError();
        glBindTexture(GL_TEXTURE_2D, textureId);
        AssertNoGlError();

        var stopwatch = Stopwatch.StartNew();

        // m_PixelUnpackBufferId = glGenBuffer();
        // glBindBuffer(GL_PIXEL_UNPACK_BUFFER, m_PixelUnpackBufferId);
        // var imageHeader = LoadImageIntoPixelUnpackBuffer();
        //
        // var width = imageHeader.ImageWidth;
        // var height = imageHeader.ImageHeight;
        // var contentSize = imageHeader.ContentSizeInBytes;
        // glCompressedTexImage2D(GL_TEXTURE_2D, 0, GL_COMPRESSED_RGBA_BPTC_UNORM, width, height, 0, contentSize, Offset(0));
        // AssertNoGlError();
        
        // NOTE(Zee): there isn't really a need to load the pixel data into the CPU
        // We can probably just stream this in directly
        // Somehow based on my testing this is still faster.
        // My assumption is that the glBufferData call is the cause, since we have to allocate that data anyway the first time
        // Its the same as if I uploaded the data directly.
        // I speculate all subsequent calls would be faster to use the direct reading into the image.
        // Also we aren't really measuring the memory savings on the CPU since the data never has to be upload into the RAM at all
        
        var image = m_ImageLoader.Load("Assets/lol");
        
        var pixels = image.Pixels;
        fixed (byte* ptr = &pixels[0])
        {
            var width = image.Width;
            var height = image.Height;
            glCompressedTexImage2D(GL_TEXTURE_2D, 0, GL_COMPRESSED_RGBA_BPTC_UNORM, width, height, 0,
                pixels.Length, ptr);
            AssertNoGlError();
        }
        
        stopwatch.Stop();
        Console.WriteLine($"Loading took: {stopwatch.ElapsedTicks}");
        
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        
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

    private ImageHeader LoadImageIntoPixelUnpackBuffer()
    {
        using var fileStream = File.OpenRead("Assets/lol.texture");
        
        var header = new ImageHeader();
        header.Read(fileStream);

        var test = Stopwatch.StartNew();
        glBufferData(GL_PIXEL_UNPACK_BUFFER, new IntPtr(header.ContentSizeInBytes), (void*)0, GL_STATIC_DRAW);
        var bufferPtr = glMapBuffer(GL_PIXEL_UNPACK_BUFFER, GL_WRITE_ONLY);
        var span = new Span<byte>(bufferPtr, header.ContentSizeInBytes);
        fileStream.Read(span);
        glUnmapBuffer(GL_PIXEL_UNPACK_BUFFER);
        test.Stop();
        Console.WriteLine(test.ElapsedMilliseconds);
        
        //Console.WriteLine("Image Width: " + header.ImageWidth);
        //Console.WriteLine("Image Height: " + header.ImageHeight);
        //Console.WriteLine("Image Content Size: " + header.ContentSize + " bytes");
        // Span<byte> buffer = stackalloc byte[4096];
        // using (var bufferWriter = BufferWriter<byte>.AllocateAndMap(GL_PIXEL_UNPACK_BUFFER, header.ContentSize, GL_STATIC_DRAW))
        // {
        //     int bytesRead;
        //     do
        //     {
        //         bytesRead = fileStream.Read(buffer);
        //         if (bytesRead == 0)
        //             break;
        //             
        //         bufferWriter.Write(buffer[..bytesRead]);
        //     } while (bytesRead == buffer.Length);
        // }

        return header;
    }
}

struct ImageHeader
{
    public int ImageWidth;
    public int ImageHeight;
    public int ContentSizeInBytes;

    public void Read(FileStream reader)
    {
        Span<byte> buffer = stackalloc byte[4];
        reader.Read(buffer);

        ImageWidth = BitConverter.ToInt32(buffer);
        
        reader.Read(buffer);
        ImageHeight = BitConverter.ToInt32(buffer);
        
        reader.Read(buffer);
        ContentSizeInBytes = BitConverter.ToInt32(buffer);
    }
}
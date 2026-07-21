namespace OpenGL.NET.Abstractions;

public interface ITexture
{
    uint Id { get; }
    uint Target { get; }
}

public interface ITexture2D : ITexture;
public interface ITexture1DArray : ITexture;
public interface ITexSubImage2DTarget : ITexture2D, ITexture1DArray;

public readonly struct Texture2D(uint id) : ITexSubImage2DTarget
{
    public uint Id { get; } = id;
    public uint Target { get; } = GL46.GL_TEXTURE_2D;
}

/// <summary>
/// All texture targets that can be bound with glBindTexture.
/// Each value equals the corresponding OpenGL enum constant.
/// </summary>
public enum TextureTarget : uint
{
    Texture1D                 = GL46.GL_TEXTURE_1D,                    // 0x0DE0
    Texture2D                 = GL46.GL_TEXTURE_2D,                    // 0x0DE1
    Texture3D                 = GL46.GL_TEXTURE_3D,                    // 0x806F
    Texture1DArray            = GL46.GL_TEXTURE_1D_ARRAY,             // 0x8C18
    Texture2DArray            = GL46.GL_TEXTURE_2D_ARRAY,             // 0x8C1A
    TextureRectangle          = GL46.GL_TEXTURE_RECTANGLE,            // 0x84F5
    TextureCubeMap            = GL46.GL_TEXTURE_CUBE_MAP,             // 0x8513
    TextureCubeMapArray       = GL46.GL_TEXTURE_CUBE_MAP_ARRAY,      // 0x9009
    TextureBuffer             = GL46.GL_TEXTURE_BUFFER,              // 0x8C2A
    Texture2DMultisample      = GL46.GL_TEXTURE_2D_MULTISAMPLE,      // 0x9100
    Texture2DMultisampleArray = GL46.GL_TEXTURE_2D_MULTISAMPLE_ARRAY,// 0x9102
}

public static class Textures
{
    public static void glBindTexture<T>(T texture) where T : ITexture
    {
        GL46.glBindTexture(texture.Target, texture.Id);
    }
    
    public static void glDeleteTexture<T>(T texture) where T : ITexture
    {
        unsafe
        {
            var id = texture.Id;
            GL46.glDeleteTextures(1, &id);
        }
    }

    public static void glTexSubImage2D<T>(
        T texture, int level, 
        int xoffset, int yoffset, 
        int width, int height,
        uint format, uint type, 
        ReadOnlySpan<uint> pixels) where T : ITexSubImage2DTarget
    {
        unsafe
        {
            fixed (void* pixelDataPtr = &pixels[0])
            {
                GL46.glTexSubImage2D(texture.Target, level, xoffset, yoffset, width, height, format, type, pixelDataPtr);
            }
        }
    }
    
    public static unsafe void glTexImage2D<TTexture, TData>(
        TTexture texture, 
        int level,
        uint internalFormat,
        int width,
        int height,
        uint format,
        uint channelType,
        ReadOnlySpan<TData> data)
        where TTexture : ITexture
        where TData : unmanaged
    {
        fixed (void* ptr = &data[0])
        {
            GL46.glTexImage2D(texture.Target, level, (int)internalFormat, 
                width, height, 0, format, channelType, ptr);
        }
    }

    public static unsafe void glTexImage2D<TTexture>(
        TTexture texture,
        int level,
        uint internalFormat,
        int width,
        int height,
        uint format,
        uint channelType) 
        where TTexture : ITexture2D
    {
        GL46.glTexImage2D(texture.Target, level, (int)internalFormat,
            width, height, 0, format, channelType, (void*)0);
    }
}
namespace OpenGL.NET.Abstractions;

public readonly struct Texture
{
    public uint Id { get; }
    public uint Target { get; }

    public Texture(uint id, uint target)
    {
        Id = id;
        Target = target;
    }
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
    public static void glBindTexture(Texture texture)
    {
        GL46.glBindTexture(texture.Target, texture.Id);
    }
    
    public static void glDeleteTexture(Texture texture)
    {
        unsafe
        {
            var id = texture.Id;
            GL46.glDeleteTextures(1, &id);
        }
    }
    
    public static Texture glBindTexture(uint target, uint textureId)
    {
        GL46.glBindTexture(target, textureId);
        return new Texture(textureId, target);
    }
    
    public static unsafe void glTexImage2D<T>(
        Texture texture, 
        int level,
        uint internalFormat,
        int width,
        int height,
        uint format,
        uint channelType,
        ReadOnlySpan<T> data) where T : unmanaged
    {
        fixed (void* ptr = &data[0])
        {
            GL46.glTexImage2D(texture.Target, level, (int)internalFormat, 
                width, height, 0, format, channelType, ptr);
        }
    }

    public static unsafe void glTexImage2D(
        Texture texture,
        int level,
        uint internalFormat,
        int width,
        int height,
        uint format,
        uint channelType)
    {
        GL46.glTexImage2D(texture.Target, level, (int)internalFormat,
            width, height, 0, format, channelType, (void*)0);
    }
}
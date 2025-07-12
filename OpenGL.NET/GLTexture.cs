namespace OpenGL.NET;

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

public static class GLTexture
{
    public static Texture glBindTexture(uint target, uint textureId)
    {
        GL46.glBindTexture(target,  textureId);
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
}
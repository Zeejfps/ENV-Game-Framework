namespace OpenGLSandbox;

public sealed class ShaderStorageBuffer<T> : IImmutableBuffer<T>
{
    public uint BindTarget => GL46.GL_SHADER_STORAGE_BUFFER;
    public uint Id { get; set; }
    public int Size { get; set; }
    public bool IsAllocated { get; set; }

    public static IImmutableBuffer<T> CreateImmutable()
    {
        return new ShaderStorageBuffer<T>();
    }
}
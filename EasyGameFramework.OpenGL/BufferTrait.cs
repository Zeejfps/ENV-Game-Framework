namespace OpenGLSandbox;

public interface IBuffer
{
    int BindTarget { get; }
    uint Id { get; set; }
}

public static class BufferMethods
{
    public static void Bind(this IBuffer buffer)
    {
        GL46.glBindBuffer(buffer.BindTarget, buffer.Id);
    }

    public static void Free(this IBuffer buffer)
    {
        var id = buffer.Id;
        unsafe
        {
            GL46.glDeleteBuffers(1, &id);
        }
    }
}
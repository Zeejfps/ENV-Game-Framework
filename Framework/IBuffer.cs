namespace Framework;

public interface IBuffer
{
    IBufferApi Use();
}

public interface IBufferApi : IDisposable
{
    void Clear();
    void Put<T>(T data) where T : unmanaged;
    void Put<T>(Span<T> data) where T : unmanaged;
    void Apply();
}
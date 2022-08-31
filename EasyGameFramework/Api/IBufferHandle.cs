namespace EasyGameFramework.Api;

public interface IBufferHandle : IDisposable
{
    void Clear();
    void Put<T>(T data) where T : unmanaged;
    void Put<T>(Span<T> data) where T : unmanaged;
    void Apply();
}
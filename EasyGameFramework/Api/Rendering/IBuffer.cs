namespace EasyGameFramework.Api.Rendering;

public interface IBuffer : IDisposable
{
    void Put<T>(T data) where T : unmanaged;
    void Put<T>(Span<T> data) where T : unmanaged;
    void Write();
}
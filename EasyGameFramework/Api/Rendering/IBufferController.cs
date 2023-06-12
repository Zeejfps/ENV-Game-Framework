namespace EasyGameFramework.Api.Rendering;

public interface IBufferController
{
    void Put<T>(T data) where T : unmanaged;
    void Put<T>(ReadOnlySpan<T> data) where T : unmanaged;
    void Write();
}
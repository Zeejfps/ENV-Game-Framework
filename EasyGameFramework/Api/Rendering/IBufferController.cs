namespace EasyGameFramework.Api.Rendering;

public interface IBufferController
{
    void Bind(IHandle<IBuffer> bufferHandle);
    void Put<T>(ReadOnlySpan<T> data) where T : unmanaged;
    void Write();
}

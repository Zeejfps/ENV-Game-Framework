namespace EasyGameFramework.Api.Rendering;

public interface IBufferController
{
    void Bind(IHandle<IBuffer> bufferHandle);
    void Upload<T>(ReadOnlySpan<T> data) where T : unmanaged;

    IHandle<IBuffer> CreateAndBind(BufferKind kind, BufferUsage usage, int sizeInBytes);
    IShaderStorageBufferHandle CreateAndBindShaderStorageBuffer(BufferUsage usage, int sizeInBytes);
}

using static GL46;
using static OpenGlWrapper.OpenGlUtilsTwo;

namespace OpenGlWrapper.Buffers;

internal class ArrayBuffer : Buffer
{
    public override uint Kind => GL_ARRAY_BUFFER;
    protected override uint Id => Handle.Id;

    public ArrayBufferHandle Handle { get; init; }

    
    public unsafe void AllocFixedSizedAndUploadData<T>(ReadOnlySpan<T> data, FixedSizedBufferAccessFlag accessFlags) where T : unmanaged
    {
        fixed (void* dataPtr = &data[0])
            AllocFixeSizeInternal<T>(data.Length, dataPtr, accessFlags);
    }
    
    public unsafe void AllocFixedSize<T>(int length, FixedSizedBufferAccessFlag accessFlags) where T : unmanaged
    {
        AllocFixeSizeInternal<T>(length, null, accessFlags);
    }
    
    private unsafe void AllocFixeSizeInternal<T>(int length, void* dataPtr, FixedSizedBufferAccessFlag accessFlags) where T : unmanaged
    {
        if (IsAllocated && IsFixedSize)
            throw new InvalidOperationException($"Can't re-allocate an already allocated FIXED-SIZED buffer, Id: {Handle.Id}");

        var sizeInBytes = SizeOf<T>(length);
        glBufferStorage(Kind, sizeInBytes, dataPtr, (uint)accessFlags);
        AssertNoGlError();
        IsFixedSize = true;
        IsAllocated = true;
        AccessFlags = accessFlags;
        SizeInBytes = sizeInBytes.ToInt32();
    }
}
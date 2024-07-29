namespace OpenGlWrapper.Buffers;

internal class ArrayBuffer
{
    public uint BufferKind => GL46.GL_ARRAY_BUFFER;
    
    public ArrayBufferHandle Handle { get; init; }
    public bool IsAllocated { get; set; }
    public bool IsFixedSize { get; set; }
    public FixedSizedBufferAccessFlag AccessFlags { get; set; } = FixedSizedBufferAccessFlag.None;
    public int SizeInBytes { get; set; }

    public void Bind()
    {
        GL46.glBindBuffer(BufferKind, Handle);
        OpenGlUtils.AssertNoGlError();
    }
    
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

        var sizeInBytes = OpenGlUtils.SizeOf<T>(length);
        GL46.glBufferStorage(BufferKind, sizeInBytes, dataPtr, (uint)accessFlags);
        OpenGlUtils.AssertNoGlError();
        IsFixedSize = true;
        IsAllocated = true;
        AccessFlags = accessFlags;
        SizeInBytes = sizeInBytes.ToInt32();
    }
}
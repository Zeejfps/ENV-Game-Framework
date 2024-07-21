namespace OpenGLSandbox;

using static GL46;
using static OpenGlUtils;

public interface IMutableBuffer<T> : IBuffer
{
    MutableBufferUsageHints UsageHint { get; set; }
    int Size { get; set; }
    bool IsAllocated { get; set; }
}

public static class MutableBufferMethods
{
    public static void Alloc<T>(this IMutableBuffer<T> buffer, int size, MutableBufferUsageHints usageHint) where T : unmanaged
    {
        unsafe
        {
            buffer.AllocUnsafe(size, (void*)0, usageHint);
        }
    }
    
    public static void ReAlloc<T>(this IMutableBuffer<T> buffer, int size, MutableBufferUsageHints usageHint) where T : unmanaged
    {
        unsafe
        {
            buffer.AllocUnsafe(size, (void*)0, usageHint);
        }
    }
    
    private static unsafe void AllocUnsafe<T>(this IMutableBuffer<T> buffer, int size, void* data, MutableBufferUsageHints usageHint) where T : unmanaged
    {
        var sizePtr = SizeOf<T>(size);
        buffer.Id = glGenBuffer();
        
        glBindBuffer(buffer.BindTarget, buffer.Id);
        AssertNoGlError();
        glBufferData(buffer.BindTarget, sizePtr, data, (int)usageHint);
        AssertNoGlError();
        
        buffer.Size = size;
        buffer.UsageHint = usageHint;
        buffer.IsAllocated = true;
    }
}
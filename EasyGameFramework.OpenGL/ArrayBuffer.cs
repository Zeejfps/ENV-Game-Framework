using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public enum MutableBufferUsageHints : int
{
    DynamicDraw = GL_DYNAMIC_DRAW,
    StaticDraw = GL_STATIC_DRAW,
}

public sealed class NotAllocatedException : Exception
{
    public NotAllocatedException() : base("Buffer is not allocated")
    {
        
    }
}

public sealed class ArrayBuffer<T> : IMutableBuffer<T>, IImmutableBuffer<T> 
    where T : unmanaged
{
    public int BindTarget => GL_ARRAY_BUFFER;
    public uint Id { get; set; }
    public MutableBufferUsageHints UsageHint { get; set; }
    public int Size { get; set; }
    public bool IsAllocated { get; set; }
    
    public static IMutableBuffer<T> CreateMutable()
    {
        return new ArrayBuffer<T>();
    }

    public static IImmutableBuffer<T> CreateImmutable()
    {
        return new ArrayBuffer<T>();
    }
    
    public void Alloc(int size, MutableBufferUsageHints usageHint)
    {
        if (IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            AllocUnsafe(size, (void*)0, usageHint);
        }
    }

    public void AllocAndWrite(T data, MutableBufferUsageHints usageHint)
    {
        if (IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            AllocUnsafe(1, &data, usageHint);
        }
    }

    public void AllocAndWrite(ReadOnlySpan<T> data, MutableBufferUsageHints usageHint)
    {
        if (IsAllocated)
            throw new NotAllocatedException();

        unsafe
        {
            fixed (void* dataPtr = &data[0])
            {
                AllocUnsafe(data.Length, dataPtr, usageHint);
            }
        }
    }

    private unsafe void AllocUnsafe(int size, void* data, MutableBufferUsageHints usageHint)
    {
        var sizePtr = SizeOf<T>(size);
        Id = glGenBuffer();
        glBindBuffer(BindTarget, Id);
        AssertNoGlError();
        glBufferData(BindTarget, sizePtr, data, (int)usageHint);
        AssertNoGlError();
        Size = size;
        UsageHint = usageHint;
        IsAllocated = true;
    }

    public void Write(int offset, ReadOnlySpan<T> data)
    {
        if (!IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            glBindBuffer(BindTarget, Id);
            AssertNoGlError();
            fixed (void* ptr = &data[0])
            {
                glBufferSubData(BindTarget, new IntPtr(offset), SizeOf<T>(data.Length), ptr);
                AssertNoGlError();
            }
        }
    }
    
    // NOTE(Zee): This API may not be the best, but it works for now
    public void WriteMapped(int offset, int length, Action<GpuMemory<T>> writeFunc)
    {
        if (!IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            glBindBuffer(GL_ARRAY_BUFFER, Id);
            AssertNoGlError();
            var bufferPtr = glMapBufferRange(BindTarget, SizeOf<T>(offset), SizeOf<T>(length), GL_MAP_WRITE_BIT);
            AssertNoGlError();
            writeFunc.Invoke(new GpuMemory<T>(bufferPtr, length));
            glUnmapBuffer(BindTarget);
            AssertNoGlError();
        }
    }
    
    public void WriteMapped(Action<GpuMemory<T>> writeFunc)
    {
        if (!IsAllocated)
            throw new NotAllocatedException();
        
        unsafe
        {
            glBindBuffer(GL_ARRAY_BUFFER, Id);
            AssertNoGlError();
            var bufferPtr = glMapBuffer(BindTarget, GL_WRITE_ONLY);
            AssertNoGlError();
            writeFunc.Invoke(new GpuMemory<T>(bufferPtr, Size));
            glUnmapBuffer(BindTarget);
            AssertNoGlError();
        }
    }
    
    public void Free()
    {
        if (!IsAllocated)
            throw new NotAllocatedException();
        
        IsAllocated = false;
        unsafe
        {
            var id = Id;
            glDeleteBuffers(1, &id);
            AssertNoGlError();
        }
        Id = 0;
    }
}
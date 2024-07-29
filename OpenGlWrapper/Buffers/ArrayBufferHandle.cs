namespace OpenGlWrapper.Buffers;

public readonly struct ArrayBufferHandle : IEquatable<ArrayBufferHandle>
{
    public static ArrayBufferHandle Null => new(0);
    
    internal uint Id { get; }

    public ArrayBufferHandle(uint id)
    {
        Id = id;
    }

    public bool Equals(ArrayBufferHandle other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArrayBufferHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(ArrayBufferHandle left, ArrayBufferHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ArrayBufferHandle left, ArrayBufferHandle right)
    {
        return !left.Equals(right);
    }

    public static implicit operator uint(ArrayBufferHandle handle)
    {
        return handle.Id;
    }
}
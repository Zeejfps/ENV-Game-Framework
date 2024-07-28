using static GL46;

namespace OpenGlWrapper;

public sealed class Texture2dManager
{
    private Texture2dHandle m_BoundHandle;
    
    public void Bind(Texture2dHandle textureHandle)
    {
        glBindTexture(GL_TEXTURE_2D, textureHandle.Id);
    }
    
    public Texture2dHandle CreateAndBind()
    {
        unsafe
        {
            uint id;
            glGenTextures(1, &id);
            var textureId = new Texture2dHandle(id);
            Bind(textureId);
            return textureId;
        }
    }

    public void Destroy(Texture2dHandle handle)
    {
        unsafe
        {
            Unbind(handle);
            var id = handle.Id;
            glDeleteTextures(1, &id);
        }
    }

    private void Unbind(Texture2dHandle handle)
    {
        if (handle != m_BoundHandle)
            return;
        
        glBindTexture(GL_TEXTURE_2D, Texture2dHandle.Null.Id);
        m_BoundHandle = Texture2dHandle.Null;
    }
}

public readonly struct Texture2dHandle : IEquatable<Texture2dHandle>
{
    public static Texture2dHandle Null => new(0);
    internal uint Id { get; }

    public Texture2dHandle(uint id)
    {
        Id = id;
    }

    public bool Equals(Texture2dHandle other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Texture2dHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(Texture2dHandle left, Texture2dHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Texture2dHandle left, Texture2dHandle right)
    {
        return !left.Equals(right);
    }
}
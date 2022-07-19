using Framework;
using static OpenGL.Gl;

namespace TicTacToePrototype.OpenGL.AssetLoaders;

public class ReadonlyTexture2D_GL : ITexture, IEquatable<ReadonlyTexture2D_GL>
{
    public bool IsLoaded => m_Id != GL_NONE;
    
    private uint m_Id;

    public unsafe ReadonlyTexture2D_GL(int width, int height, byte[]? pixels = null)
    {
        //Console.WriteLine($"Texture: {width}x{height}, pixels: {pixels.Length}");
        m_Id = glGenTexture();
        glBindTexture(GL_TEXTURE_2D, m_Id);

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);	
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

        if (pixels != null)
        {
            fixed (byte* p = &pixels[0])
                glCompressedTexImage2D(GL_TEXTURE_2D, 0, GL_COMPRESSED_RGBA_BPTC_UNORM_ARB, width, height, 0, width*height, p);
        }
       
        // fixed (byte* p = &pixels[0])
        //     glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, p);
        
        glAssertNoError();
    }
    
    public void Unload()
    {
        glDeleteTexture(m_Id);
        m_Id = GL_NONE;
    }

    public ITextureHandle Use()
    {
        glBindTexture(GL_TEXTURE_2D, m_Id);
        return new Handle();
    }

    public bool Equals(ReadonlyTexture2D_GL? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return m_Id == other.m_Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ReadonlyTexture2D_GL)obj);
    }

    public override int GetHashCode()
    {
        return (int)m_Id;
    }

    public static bool operator ==(ReadonlyTexture2D_GL? left, ReadonlyTexture2D_GL? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ReadonlyTexture2D_GL? left, ReadonlyTexture2D_GL? right)
    {
        return !Equals(left, right);
    }
    
    class Handle : ITextureHandle {}
}
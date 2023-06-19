using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

public class ReadonlyCubeMap_GL : CubeMapTexture_GL, IEquatable<ReadonlyCubeMap_GL>
{
    public ReadonlyCubeMap_GL(uint id, int width, int height) : base(id, width, height)
    {
    }


    public bool Equals(ReadonlyCubeMap_GL? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((ReadonlyCubeMap_GL)obj);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(ReadonlyCubeMap_GL? left, ReadonlyCubeMap_GL? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ReadonlyCubeMap_GL? left, ReadonlyCubeMap_GL? right)
    {
        return !Equals(left, right);
    }

    public static ReadonlyCubeMap_GL Create(int width, int height, byte[][] facesData, TextureFilterKind textureFilterKind)
    {
        var id = glGenTexture();
        glBindTexture(GL_TEXTURE_CUBE_MAP, id);

        var filterParam = textureFilterKind == TextureFilterKind.Linear ? GL_LINEAR : GL_NEAREST;

        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, filterParam);
        glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, filterParam);

        for (uint i = 0; i < 6; i++)
        {
            byte[] facePixels = facesData[i];
            unsafe
            {
                fixed (byte* p = &facePixels[0])
                {
                    glCompressedTexImage2D((int)(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i), 0, GL_COMPRESSED_RGBA_BPTC_UNORM_ARB, width, height, 0, width * height, new IntPtr(p));
                }
            }
        }

        // If you need non-compressed textures, uncomment and replace glCompressedTexImage2D with the line below:
        // glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, p);

        glAssertNoError();

        return new ReadonlyCubeMap_GL(id, width, height);
    }

}
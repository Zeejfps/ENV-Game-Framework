using OpenGLSandbox;

namespace OpenGL.NET;

using static GL46;
using static OpenGlUtils;

public enum TextureMinFilter : uint
{
    Nearest = GL_NEAREST,
    Linear = GL_LINEAR,
    NearestMipmapNearest = GL_NEAREST_MIPMAP_NEAREST,
    LinearMipmapNearest = GL_LINEAR_MIPMAP_NEAREST,
    NearestMipmapLinear = GL_NEAREST_MIPMAP_LINEAR,
    LinearMipmapLinear = GL_LINEAR_MIPMAP_LINEAR,
}


public enum TextureMagFilter : uint
{
    Nearest = GL_NEAREST,
    Linear = GL_LINEAR,
}

public sealed class Texture2DBuilder
{
    private uint _minFilter = GL_LINEAR;
    private uint _magFilter = GL_LINEAR;
    
    public Texture2DBuilder WithMinFilter(TextureMinFilter filter)
    {
        _minFilter = (uint)filter;
        return this;
    }
    
    public Texture2DBuilder WithMagFilter(TextureMagFilter filter)
    {
        _magFilter = (uint)filter;
        return this;
    }
    
    public unsafe Texture BindAndBuild()
    {
        uint textureId;
        
        glGenTextures(1, &textureId);
        AssertNoGlError();
    
        glBindTexture(GL_TEXTURE_2D, textureId);
        AssertNoGlError();
    
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)_minFilter);
        AssertNoGlError();

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)_magFilter);
        AssertNoGlError();
        
        return new Texture(textureId, GL_TEXTURE_2D);
    }
}
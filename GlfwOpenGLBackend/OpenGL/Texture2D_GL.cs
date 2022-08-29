using EasyGameFramework.API.AssetTypes;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend.OpenGL;

public class Texture2D_GL : IGpuTexture
{
    public bool IsLoaded { get; private set; }
    public uint Id => m_Id;

    private uint m_Id;

    public Texture2D_GL(uint id)
    {
        m_Id = id;
    }

    public void Dispose()
    {
        
    }
}
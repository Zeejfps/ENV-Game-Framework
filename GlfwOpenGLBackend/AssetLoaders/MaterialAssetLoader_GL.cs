using Framework;
using Framework.Assets;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend.AssetLoaders;

public class MaterialAssetLoader_GL : MaterialAssetLoader
{
    protected override IMaterial LoadAsset(MaterialAsset asset)
    {
        var vertexShader = glCreateShader(GL_VERTEX_SHADER);
        
        return null;
    }
}
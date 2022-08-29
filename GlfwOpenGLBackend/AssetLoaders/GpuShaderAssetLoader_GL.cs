using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.AssetManagement;
using Framework;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend.AssetLoaders;

// public class DebugMaterialAssetLoader_GL : IAssetLoader<IGpuShader>
// {
//     public IAsset LoadAsset(string assetPath)
//     {
//         var fileName = Path.GetFileNameWithoutExtension(assetPath);
//
//         var vertexShaderName = $"{fileName}.vert";
//         var fragmentShaderName = $"{fileName}.frag";
//         var pathToVertShader = Directory.GetFiles("Assets/", vertexShaderName, SearchOption.AllDirectories).FirstOrDefault();
//         var pathToFragShader = Directory.GetFiles("Assets/", fragmentShaderName, SearchOption.AllDirectories).FirstOrDefault();
//
//         if (string.IsNullOrEmpty(pathToVertShader))
//             throw new Exception($"Failed to find a vertex shader {vertexShaderName}");
//         
//         if (string.IsNullOrEmpty(pathToFragShader))
//             throw new Exception($"Failed to find a fragment shader {fragmentShaderName}");
//
//         var vertShaderSource = File.ReadAllText(pathToVertShader);
//         var fragShaderSource = File.ReadAllText(pathToFragShader);
//         
//         return Material_GL.LoadFromSource(vertShaderSource, fragShaderSource);
//     }
// }

public class GpuShaderAssetLoader_GL : IAssetLoader<IGpuShader>
{
    private readonly CpuShaderAssetLoader m_CpuShaderAssetLoader = new();

    public IGpuShader Load(string assetPath)
    {
        var asset = m_CpuShaderAssetLoader.Load(assetPath);
        return Shader_GL.LoadFromSource(asset.VertexShader, asset.FragmentShader);
    }

    // private unsafe void LoadShaderFromBinary(uint shader, byte[] shaderData)
    // {
    //     fixed (void* p = &shaderData[0])
    //         glShaderBinary(shader, GL_SHADER_BINARY_FORMAT_SPIR_V_ARB, p, shaderData.Length);
    //     var err = glGetError();
    //     if (err != GL_NO_ERROR)
    //         throw new Exception($"Error loading shader: {err:X}");
    //
    //     glSpecializeShader(shader, "main", 0, null, null);
    //     err = glGetError();
    //     if (err != GL_NO_ERROR)
    //         throw new Exception($"Error loading shader: {err:X}");
    // }
}
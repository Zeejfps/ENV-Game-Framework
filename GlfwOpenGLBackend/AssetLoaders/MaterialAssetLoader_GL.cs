﻿using Framework;
using Framework.Assets;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend.AssetLoaders;

public class MaterialAssetLoader_GL : MaterialAssetLoader
{
    protected override IMaterial LoadAsset(MaterialAsset asset)
    {
        var vertexShader = glCreateShader(GL_VERTEX_SHADER);
        LoadShaderFromBinary(vertexShader, asset.VertexShader);

        var fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
        LoadShaderFromBinary(fragmentShader, asset.FragmentShader);
        
        var program = glCreateProgram();
        glAttachShader(program, vertexShader);
        glAttachShader(program, fragmentShader);

        glLinkProgram(program);

        var error = glGetProgramInfoLog(program);
        if (!string.IsNullOrEmpty(error))
            throw new Exception($"Error compiling program:\n{error}");
        
        glDeleteShader(vertexShader);
        glDeleteShader(fragmentShader);
        
        return null;
    }

    private unsafe void LoadShaderFromBinary(uint shader, byte[] shaderData)
    {
        fixed (void* p = &shaderData[0])
            glShaderBinary(shader, GL_SHADER_BINARY_FORMAT_SPIR_V_ARB, p, shaderData.Length);
        var err = glGetError();
        if (err != GL_NO_ERROR)
            throw new Exception($"Error loading shader: {err:X}");

        glSpecializeShader(shader, "main", 0, null, null);
        err = glGetError();
        if (err != GL_NO_ERROR)
            throw new Exception($"Error loading shader: {err:X}");
    }
}
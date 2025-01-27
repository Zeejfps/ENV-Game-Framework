﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static OpenGL.Gl;

namespace OpenGLSandbox;

public sealed class OpenGlException : Exception
{
    private readonly StackTrace m_StackTrace;

    public OpenGlException(string error, StackTrace stackTrace) : base(error)
    {
        m_StackTrace = stackTrace;
    }

    public override string? StackTrace => m_StackTrace.ToString();
}

public static class OpenGlUtils
{
    [Conditional("DEBUG")]
    public static void AssertNoGlError()
    {
        var hasError = glTryGetError(out var error);
        if (hasError)
        {
            var stackTrace = new StackTrace(1, true);
            throw new OpenGlException(error, stackTrace);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe void* Offset(int offset)
    {
        return (void*)offset;
    } 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Offset<T>(string field)
    {
        return (void*)Marshal.OffsetOf<T>(field);
    } 

    public static unsafe uint CreateAndCompileShaderFromSourceFile(uint type, string filePath)
    {
        var source = File.ReadAllText(filePath);
        return CreateAndCompileShaderFromSource(type, source);
    }

    public static unsafe uint glGenBuffer()
    {
        uint b;
        glGenBuffers(1, &b);
        return b;
    }

    public static unsafe uint CreateAndCompileShaderFromSource(uint type, string source)
    {
        var shader = glCreateShader((int)type);
        AssertNoGlError();
        
        glShaderSource(shader, source);
        AssertNoGlError();
        
        glCompileShader(shader);
        AssertNoGlError();

        int status;
        glGetShaderiv(shader, GL_COMPILE_STATUS, &status);
        AssertNoGlError();
        
        if (status == GL_FALSE)
        {
            var infoLog = glGetShaderInfoLog(shader);
            AssertNoGlError();

            var shaderTypeAsString = type == GL_VERTEX_SHADER ? "Vertex Shader" : "Fragment Shader";
            Console.WriteLine($"Failed to compile shader: {shaderTypeAsString}");
            Console.WriteLine(infoLog);
            glDeleteShader(shader);
            AssertNoGlError();
            
            return 0;
        }

        return shader;
    }

    public static unsafe uint glGenTexture()
    {
        uint id;
        glGenTextures(1, &id);
        AssertNoGlError();
        return id;
    }

    public static unsafe int GetUniformLocation(uint shaderProgram, string uniformName)
    {
        var uniformNameAsAsciiBytes = Encoding.ASCII.GetBytes(uniformName);
        int uniformLocation;
        fixed(byte* ptr = &uniformNameAsAsciiBytes[0])
            uniformLocation = glGetUniformLocation(shaderProgram, ptr);
        AssertNoGlError();
        return uniformLocation;
    }

    public static unsafe IntPtr SizeOf<T>() where T : unmanaged
    {
        return new IntPtr(sizeof(T));
    }
    
    public static unsafe IntPtr SizeOf<T>(uint count) where T : unmanaged
    {
        return new IntPtr(sizeof(T) * count);
    }
    
    public static unsafe IntPtr SizeOf<T>(int count) where T : unmanaged
    {
        return new IntPtr(sizeof(T) * count);
    }
}
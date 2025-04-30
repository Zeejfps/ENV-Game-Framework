using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using OpenGL.NET;
using OpenGlWrapper;
using static GL46;

namespace OpenGLSandbox;

public static class OpenGlUtils
{
    [Conditional("DEBUG")]
    public static void AssertNoGlError()
    {
        var hasError = TryGetGlError(out var error);
        if (hasError)
        {
            var stackTrace = new StackTrace(1, true);
            throw new OpenGlException(error, stackTrace);
        }
    }

    public static ShaderProgramCompiler NewShader()
    {
        return new ShaderProgramCompiler();
    }

    public static bool TryGetGlError(out string errorStr)
    {
        var error = glGetError();
        if (error != GL_NO_ERROR)
        {
            errorStr = $"Unknown Error 0x{error:X}";
            switch (error)
            {
                case 0x0500:
                    errorStr = "GL_INVALID_ENUM";
                    break;
                case 0x0501:
                    errorStr = "GL_INVALID_VALUE";
                    break;
                case 0x0502:
                    errorStr = "GL_INVALID_OPERATION";
                    break;
                case 0x0503:
                    errorStr = "GL_STACK_OVERFLOW";
                    break;
                case 0x0504:
                    errorStr = "GL_STACK_UNDERFLOW";
                    break;
                case 0x0505:
                    errorStr = "GL_OUT_OF_MEMORY";
                    break;
                case 0x0506:
                    errorStr = "GL_INVALID_FRAMEBUFFER_OPERATION - Given when doing anything that would attempt to read from or write/render to a framebuffer that is not complete.";
                    break;
            }

            return true;
        }

        errorStr = string.Empty;
        return false;
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

    public static uint CreateAndCompileShaderFromSourceFile(uint type, string pathToFile)
    {
        var source = File.ReadAllText(pathToFile);
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
        var shader = glCreateShader(type);
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

            var shaderTypeAsString = "Unknown Shader";
            if (type == GL_VERTEX_SHADER)
                shaderTypeAsString = "Vertex Shader";
            else if (type == GL_FRAGMENT_SHADER)
                shaderTypeAsString = "Fragment Shader";
            else if (type == GL_GEOMETRY_SHADER)
                shaderTypeAsString = "Geometry Shader";
            
            Console.WriteLine($"Failed to compile shader: {shaderTypeAsString}");
            Console.WriteLine(infoLog);
            glDeleteShader(shader);
            AssertNoGlError();
            
            return 0;
        }

        return shader;
    }

    public unsafe static void glShaderSource(uint shader,  string source)
    {
        var buffer = Encoding.UTF8.GetBytes(source);
        fixed (byte* p = &buffer[0])
        {
            var sources = new[] { p };
            fixed (byte** s = &sources[0])
            {
                var length = buffer.Length;
                GL46.glShaderSource(shader, 1, s, &length);
            }
        }
    }

    public static unsafe uint glGenTexture()
    {
        uint id;
        glGenTextures(1, &id);
        AssertNoGlError();
        return id;
    }

    public unsafe static string glGetShaderInfoLog(uint shader, int bufSize = 1024)
    {
        var buffer = Marshal.AllocHGlobal(bufSize);
        try
        {
            int length;
            var source = (byte*) buffer.ToPointer();
            GL46.glGetShaderInfoLog(shader, bufSize, &length, source);
            return PtrToStringUtf8(buffer, length);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string PtrToStringUtf8(IntPtr ptr, int length)
    {
        var buffer = new byte[length];
        Marshal.Copy(ptr, buffer, 0, length);
        return Encoding.UTF8.GetString(buffer);
    }


    public static unsafe int GetUniformLocation(uint shaderProgram, string uniformName)
    {
        var uniformNameAsAsciiBytes = Encoding.ASCII.GetBytes(uniformName);
        int uniformLocation;
        fixed(byte* ptr = &uniformNameAsAsciiBytes[0])
            uniformLocation = glGetUniformLocation(shaderProgram, ptr); AssertNoGlError();
        return uniformLocation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe IntPtr SizeOf<T>() where T : unmanaged
    {
        return new IntPtr(sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe IntPtr SizeOf<T>(uint count) where T : unmanaged
    {
        return new IntPtr(sizeof(T) * count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe IntPtr SizeOf<T>(int count) where T : unmanaged
    {
        return new IntPtr(sizeof(T) * count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AttribOffset<T>(string field)
    {
        return Marshal.OffsetOf<T>(field).ToInt32();
    }
}
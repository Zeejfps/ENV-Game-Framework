using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static OpenGL.Gl;

namespace OpenGLSandbox;

public static class Utils_GL
{
    [Conditional("DEBUG")]
    public static void AssertNoGlError()
    {
        Debug.Assert(!glTryGetError(out var error), error);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Offset(int offset)
    {
        return (void*)offset;
    } 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Offset<T>(string field)
    {
        return (void*)Marshal.OffsetOf<T>(field);
    } 

    public static unsafe uint CreateAndCompileShaderFromSourceFile(int type, string filePath)
    {
        var source = File.ReadAllText(filePath);
        return CreateAndCompileShaderFromSource(type, source);
    }

    public static unsafe uint CreateAndCompileShaderFromSource(int type, string source)
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
            
            Console.WriteLine(infoLog);
            glDeleteShader(shader);
            AssertNoGlError();
            
            return 0;
        }

        return shader;
    }
}
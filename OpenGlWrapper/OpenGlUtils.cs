using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static GL46;

namespace OpenGlWrapper;

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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AttribOffset<T>(string field)
    {
        return Marshal.OffsetOf<T>(field).ToInt32();
    } 
}
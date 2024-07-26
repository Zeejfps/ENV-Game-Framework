using System.Text;
using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class ShaderProgramManager
{
    public void Bind(ShaderProgramId shaderProgram)
    {
        
    }

    public ShaderProgramId CompileFromSourceFiles(string vertexShaderFilePath, string fragmentShaderFilePath)
    {
        var vertexShaderSource = ReadShaderSourceFromFile(vertexShaderFilePath);
        var fragmentShaderSource = ReadShaderSourceFromFile(fragmentShaderFilePath);
        return CreateAndCompileFromSource(vertexShaderSource, fragmentShaderSource);
    }

    public ShaderProgramId CreateAndCompileFromSource(string vertexShaderSource, string fragmentShaderSource)
    {
        unsafe
        {
            var shaderProgram = glCreateProgram();
            AssertNoGlError();

            uint vertexShader = 0;
            vertexShader = CreateAndCompileShaderFromSource(GL_VERTEX_SHADER, vertexShaderSource);
            glAttachShader(shaderProgram, vertexShader);
            AssertNoGlError();
            
            uint fragmentShader = 0;
            fragmentShader = CreateAndCompileShaderFromSource(GL_FRAGMENT_SHADER, fragmentShaderSource);
            glAttachShader(shaderProgram, fragmentShader);
            AssertNoGlError();
        
            glLinkProgram(shaderProgram);
            AssertNoGlError();
        
            int status;
            glGetProgramiv(shaderProgram, GL_LINK_STATUS, &status);
            AssertNoGlError();
        
            if (status == GL_FALSE)
            {
                Span<byte> buffer = stackalloc byte[256];
                int length;
                fixed (byte* ptr = &buffer[0])
                    glGetProgramInfoLog(shaderProgram, 256, &length, ptr);
                AssertNoGlError();
                var log = Encoding.ASCII.GetString(buffer);
                Console.WriteLine($"Linking Failed: {log}");
            }
        
            glDeleteShader(vertexShader);
            AssertNoGlError();
        
            glDeleteShader(fragmentShader);
            AssertNoGlError();
        
            return new ShaderProgramId(shaderProgram);
        }
    }
    
    private string ReadShaderSourceFromFile(string sourceFilePath)
    {
        var source = File.ReadAllText(sourceFilePath);
        return source;
    }
    
    private uint CreateAndCompileShaderFromSource(uint type, string source)
    {
        unsafe
        {
            var shader = glCreateShader(type);
            AssertNoGlError();

            var line = Encoding.Default.GetBytes(source);
            fixed (byte* ptr = &line[0])
            {
                var lines = stackalloc[]
                {
                    ptr
                };
                var length = line.Length;
                glShaderSource(shader, 1, &lines[0], &length);
            }
            AssertNoGlError();
        
            glCompileShader(shader);
            AssertNoGlError();

            int status;
            glGetShaderiv(shader, GL_COMPILE_STATUS, &status);
            AssertNoGlError();
        
            if (status == GL_FALSE)
            {
                int length;
                Span<byte> infoLogBuffer = stackalloc byte[1024];
                fixed (byte* ptr = &infoLogBuffer[0])
                    glGetShaderInfoLog(shader, infoLogBuffer.Length, &length, ptr);
                AssertNoGlError();

                var infoLog = Encoding.Default.GetString(infoLogBuffer);
                var shaderTypeAsString = type == GL_VERTEX_SHADER ? "Vertex Shader" : "Fragment Shader";
                Console.WriteLine($"Failed to compile shader: {shaderTypeAsString}");
                Console.WriteLine(infoLog);
                glDeleteShader(shader);
                AssertNoGlError();
            
                return 0;
            }

            return shader;
        }
    }
}

public readonly struct ShaderProgramId : IEquatable<ShaderProgramId>
{
    public static ShaderProgramId Null => new(0);
    
    internal uint Id { get; }

    public ShaderProgramId(uint id)
    {
        Id = id;
    }

    public bool Equals(ShaderProgramId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ShaderProgramId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(ShaderProgramId left, ShaderProgramId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ShaderProgramId left, ShaderProgramId right)
    {
        return !left.Equals(right);
    }
}
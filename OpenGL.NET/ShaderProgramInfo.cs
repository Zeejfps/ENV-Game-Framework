namespace OpenGL.NET;

public readonly struct ShaderProgramInfo
{
    public uint Id { get; init; }

    public IDictionary<string, int> Uniforms { get; init; }
}
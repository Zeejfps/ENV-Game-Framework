namespace OpenGL.NET.Abstractions;

public readonly struct ShaderProgramId
{
    public uint Id { get; init; }
    
    public static implicit operator uint(ShaderProgramId info) => info.Id;
}
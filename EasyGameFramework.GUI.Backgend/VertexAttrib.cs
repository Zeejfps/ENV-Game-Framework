namespace OpenGLSandbox;

[AttributeUsage(AttributeTargets.Field)]
public sealed class VertexAttrib : Attribute
{
    public VertexAttrib(uint componentCount, uint componentType)
    {
        ComponentCount = (int)componentCount;
        ComponentType = componentType;
    }

    public int ComponentCount { get; }
    public uint ComponentType { get; }
}
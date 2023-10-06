namespace OpenGLSandbox;

[AttributeUsage(AttributeTargets.Field)]
public sealed class InstancedAttrib : Attribute
{
    public InstancedAttrib(uint componentCount, int componentType)
    {
        ComponentCount = (int)componentCount;
        ComponentType = componentType;
    }

    public int ComponentCount { get; }
    public int ComponentType { get; }
}
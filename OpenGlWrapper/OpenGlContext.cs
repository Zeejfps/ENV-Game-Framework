using static GL46;

namespace OpenGlWrapper;

public sealed class OpenGlContext
{
    public ArrayBufferManager ArrayBufferManager { get; }
    public VertexArrayObjectManager VertexArrayObjectManager { get; }

    private OpenGlContext()
    {
        ArrayBufferManager = new ArrayBufferManager();
        VertexArrayObjectManager = new VertexArrayObjectManager(ArrayBufferManager);
    }

    public static OpenGlContext Init(GetProcAddressDelegate getProcAddressDelegate)
    {
        Import(getProcAddressDelegate);
        return new OpenGlContext();
    }
}
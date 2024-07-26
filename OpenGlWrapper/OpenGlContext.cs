using static GL46;

namespace OpenGlWrapper;

public sealed class OpenGlContext
{
    public ArrayBufferManager ArrayBufferManager { get; }
    public VertexArrayObjectManager VertexArrayObjectManager { get; }
    public FramebufferManager FramebufferManager { get; }
    public ShaderProgramManager ShaderProgramManager { get; }

    private OpenGlContext()
    {
        ArrayBufferManager = new ArrayBufferManager();
        VertexArrayObjectManager = new VertexArrayObjectManager(ArrayBufferManager);
        FramebufferManager = new FramebufferManager(VertexArrayObjectManager);
        ShaderProgramManager = new ShaderProgramManager();
    }

    public static OpenGlContext Init(GetProcAddressDelegate getProcAddressDelegate)
    {
        Import(getProcAddressDelegate);
        return new OpenGlContext();
    }
}
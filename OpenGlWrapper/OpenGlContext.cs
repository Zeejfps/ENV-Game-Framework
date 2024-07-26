using OpenGlWrapper.Buffers;
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
        ShaderProgramManager = new ShaderProgramManager();
        FramebufferManager = new FramebufferManager(ShaderProgramManager, VertexArrayObjectManager);
    }

    public static OpenGlContext Init(GetProcAddressDelegate getProcAddressDelegate)
    {
        Import(getProcAddressDelegate);
        return new OpenGlContext();
    }
}
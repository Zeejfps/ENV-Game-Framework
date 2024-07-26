using static GL46;

namespace OpenGlWrapper;

public sealed class OpenGlContext
{
    public ArrayBufferManager ArrayBufferManager { get; }

    private OpenGlContext()
    {
        ArrayBufferManager = new ArrayBufferManager();
    }

    public static OpenGlContext Init(GetProcAddressDelegate getProcAddressDelegate)
    {
        Import(getProcAddressDelegate);
        return new OpenGlContext();
    }
}
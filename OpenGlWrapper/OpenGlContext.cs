using static GL46;

namespace OpenGlWrapper;

public sealed class OpenGlContext
{
    public ArrayBufferManager ArrayBufferManager { get; }

    public OpenGlContext(GetProcAddressDelegate getProcAddressDelegate)
    {
        Import(getProcAddressDelegate);
        ArrayBufferManager = new ArrayBufferManager();
    }
}
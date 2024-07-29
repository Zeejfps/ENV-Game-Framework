using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper.Buffers;

internal abstract class Buffer
{
    public abstract uint Kind { get; }
    
    protected abstract uint Id { get; }
    
    public void Bind()
    {
        glBindBuffer(Kind, Id);
        AssertNoGlError();
    }
}
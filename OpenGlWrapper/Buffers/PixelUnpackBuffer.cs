using static GL46;

namespace OpenGlWrapper.Buffers;

internal class PixelUnpackBuffer : Buffer
{
    public override uint Kind => GL_PIXEL_UNPACK_BUFFER;
    protected override uint Id { get; }
}

public readonly struct PixelUnpackBufferHandle
{
    
}
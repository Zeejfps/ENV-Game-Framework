using WebPSharp.Api.Exceptions;
using WebPSharp.Vp8L;
using WebPSharp.Vp8L.Transforms;

namespace WebPSharp.Tests;

public class Vp8LTransformRejectTests
{
    private const uint Signature = 0x2F;

    /// <summary>
    /// Builds a minimal VP8L payload whose transform list signals two transforms with identical
    /// type codes back to back. The decoder must reject this before it reaches any image data.
    /// </summary>
    private static byte[] BuildDuplicateTransformStream(Vp8LTransformType type)
    {
        var w = new Vp8LBitWriter();
        w.PutBits(Signature, 8);
        w.PutBits(0, 14); // width - 1  (1x1 image)
        w.PutBits(0, 14); // height - 1
        w.PutBit(0);      // alpha_is_used hint
        w.PutBits(0, 3);  // version

        // First transform: present bit + 2-bit type.
        w.PutBit(1);
        w.PutBits((uint)type, 2);
        // SubtractGreen carries no further transform data, so the reader immediately loops.

        // Second transform: same type -> duplicate, must throw.
        w.PutBit(1);
        w.PutBits((uint)type, 2);

        return w.ToArray();
    }

    [Fact]
    public void Decode_DuplicateSubtractGreenTransform_Throws()
    {
        var payload = BuildDuplicateTransformStream(Vp8LTransformType.SubtractGreen);
        var ex = Assert.Throws<WebPCorruptException>(() => Vp8LDecoder.Decode(payload));
        Assert.Contains("more than once", ex.Message);
    }
}

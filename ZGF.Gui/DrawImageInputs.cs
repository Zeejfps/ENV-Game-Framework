using System.Numerics;
using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawImageInputs
{
    public required RectF Position { get; init; }
    public required int ZIndex { get; init; }
    public required string ImageId { get; init; }
    public required uint TintColor { get; init; }
    public required float Rotation { get; init; }

    /// <summary>How round each corner of the drawn image is, in canvas units, as top-left, top-right,
    /// bottom-right and bottom-left; zero for square. The image is cut to the rounded shape, its edge
    /// smoothed over a device pixel.</summary>
    public Vector4 CornerRadius { get; init; }
}

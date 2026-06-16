using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

public sealed record Image : Widget
{
    public required string ImageId { get; init; }
    public uint Tint { get; init; } = 0xFFFFFFFF;
    public float Rotation { get; init; }

    protected override View CreateView(Context ctx) => new ImageView(ctx.Canvas)
    {
        ImageId = ImageId,
        TintColor = Tint,
        Rotation = Rotation,
    };
}

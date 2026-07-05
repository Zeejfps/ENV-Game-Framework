using ZGF.AppUtils;
using ZGF.Gui.Views;
using ZGF.Svg;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Displays an SVG, rasterized at its laid-out size (and DPI) so it stays crisp at any
/// scale. Unlike <see cref="Image"/>, which draws a texture loaded up front by id, the
/// document loads lazily through the app's <see cref="SvgImageCache"/> because the raster
/// size is only known after layout. Exactly one of <see cref="Source"/> or
/// <see cref="Data"/> must be set.
/// </summary>
public sealed record SvgImage : Widget
{
    /// <summary>Path to an .svg file, absolute or relative to the app base directory.</summary>
    public string? Source { get; init; }

    /// <summary>Raw SVG bytes — for embedded resources or downloaded content.</summary>
    public byte[]? Data { get; init; }

    /// <summary>The SVG currentColor, baked at raster time — recolors monochrome icons per theme.</summary>
    public Prop<uint> Color { get; init; }

    /// <summary>Multiplied at draw time on the GPU, like <see cref="Image.Tint"/> — no re-raster.</summary>
    public uint Tint { get; init; } = 0xFFFFFFFF;

    public float Rotation { get; init; }

    protected override View CreateView(Context ctx)
    {
        if (Source is null == Data is null)
            throw new InvalidOperationException("SvgImage requires exactly one of Source or Data.");

        var cache = ctx.Require<SvgImageCache>();
        var document = Source != null
            ? cache.GetOrParseFile(Path.IsPathRooted(Source) ? Source : PathUtils.ResolveLocalPath(Source))
            : cache.GetOrParse(Data!);

        var v = new SvgView(cache, ctx.Require<IFrameTicker>(), document);
        Color.Apply(ctx, v, static (x, c) => x.CurrentColor = c);
        v.TintColor = Tint;
        v.Rotation = Rotation;
        return v;
    }
}

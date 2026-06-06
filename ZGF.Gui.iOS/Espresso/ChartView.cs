using ZGF.Geometry;
using ZGF.Gui;

namespace ZGF.Gui.iOS.Espresso;

/// <summary>
/// The espresso brewing-control chart, VST-style: Extraction Yield % on X, Strength (TDS) % on Y,
/// a central "ideal" zone, and a live point for the current shot. Drawn entirely from rectangles
/// — gridlines and the zone outline are thin rects, the point is a rounded rect — since the canvas
/// has no line/arc primitives. App-specific (coffee semantics live here); the reusable slider it
/// pairs with lives in ZGF.Gui.Mobile.
/// </summary>
public sealed class ChartView : MultiChildView
{
    // Axis ranges, espresso-tuned.
    public const float EyMin = 14f, EyMax = 26f;
    public const float TdsMin = 4f, TdsMax = 14f;

    // The central "balanced" zone.
    public const float IdealEyMin = 19f, IdealEyMax = 22f;
    public const float IdealTdsMin = 8f, IdealTdsMax = 12f;

    private const uint PanelColor = 0xFF161B26;
    private const uint GridColor = 0xFF252C3C;
    private const uint ZoneFill = 0x2255E08A;
    private const uint ZoneBorder = 0xFF55E08A;
    private const uint LabelColor = 0xFF7C879E;
    private const uint CrosshairColor = 0x33FFFFFF;
    private const uint PointRing = 0xFFFFFFFF;

    private const float MarginLeft = 32f;
    private const float MarginBottom = 22f;
    private const float MarginTop = 12f;
    private const float MarginRight = 14f;

    private float _ey = 20f;
    private float _tds = 9f;
    private uint _pointColor = 0xFF55E08A;

    public void SetShot(float extractionYield, float tds, uint pointColor)
    {
        SetField(ref _ey, extractionYield);
        SetField(ref _tds, tds);
        SetField(ref _pointColor, pointColor);
    }

    private static float MapX(in RectF plot, float ey) => plot.Left + (ey - EyMin) / (EyMax - EyMin) * plot.Width;
    private static float MapY(in RectF plot, float tds) => plot.Bottom + (tds - TdsMin) / (TdsMax - TdsMin) * plot.Height;

    protected override void OnDrawSelf(ICanvas c)
    {
        var z = GetDrawZIndex();
        var outer = Position;

        c.DrawRect(new DrawRectInputs
        {
            Position = outer,
            Style = new RectStyle { BackgroundColor = PanelColor, BorderRadius = BorderRadiusStyle.All(16f) },
            ZIndex = z,
        });

        var plot = new RectF(
            outer.Left + MarginLeft,
            outer.Bottom + MarginBottom,
            outer.Width - MarginLeft - MarginRight,
            outer.Height - MarginBottom - MarginTop);

        if (plot.Width <= 1f || plot.Height <= 1f)
            return;

        var labelStyle = new TextStyle
        {
            FontSize = 10f,
            TextColor = LabelColor,
            HorizontalAlignment = TextAlignment.Center,
            VerticalAlignment = TextAlignment.Center,
        };

        // Vertical gridlines + EY axis labels.
        for (var ey = 14f; ey <= 26f; ey += 2f)
        {
            var x = MapX(plot, ey);
            FillRect(c, new RectF(x - 0.5f, plot.Bottom, 1f, plot.Height), GridColor, z + 1);
            c.DrawText(new DrawTextInputs
            {
                Position = new RectF(x - 14f, outer.Bottom + 3f, 28f, 14f),
                Text = ((int)ey).ToString(),
                Style = labelStyle,
                ZIndex = z + 6,
            });
        }

        // Horizontal gridlines + TDS axis labels.
        for (var tds = 4f; tds <= 14f; tds += 2f)
        {
            var y = MapY(plot, tds);
            FillRect(c, new RectF(plot.Left, y - 0.5f, plot.Width, 1f), GridColor, z + 1);
            c.DrawText(new DrawTextInputs
            {
                Position = new RectF(outer.Left + 2f, y - 7f, MarginLeft - 6f, 14f),
                Text = ((int)tds).ToString(),
                Style = labelStyle,
                ZIndex = z + 6,
            });
        }

        // Ideal zone: translucent fill + 1px stroke (four edge rects).
        var zoneLeft = MapX(plot, IdealEyMin);
        var zoneRight = MapX(plot, IdealEyMax);
        var zoneBottom = MapY(plot, IdealTdsMin);
        var zoneTop = MapY(plot, IdealTdsMax);
        var zone = new RectF(zoneLeft, zoneBottom, zoneRight - zoneLeft, zoneTop - zoneBottom);
        c.DrawRect(new DrawRectInputs
        {
            Position = zone,
            Style = new RectStyle { BackgroundColor = ZoneFill, BorderRadius = BorderRadiusStyle.All(6f) },
            ZIndex = z + 2,
        });
        StrokeRect(c, zone, ZoneBorder, 1f, z + 3);

        // The live point, clamped onto the plot so it never leaves the panel.
        var px = MapX(plot, Math.Clamp(_ey, EyMin, EyMax));
        var py = MapY(plot, Math.Clamp(_tds, TdsMin, TdsMax));

        FillRect(c, new RectF(plot.Left, py - 0.5f, plot.Width, 1f), CrosshairColor, z + 4);
        FillRect(c, new RectF(px - 0.5f, plot.Bottom, 1f, plot.Height), CrosshairColor, z + 4);

        const float ring = 9f;
        const float core = 6f;
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(px - ring, py - ring, ring * 2f, ring * 2f),
            Style = new RectStyle { BackgroundColor = PointRing, BorderRadius = BorderRadiusStyle.All(ring) },
            ZIndex = z + 7,
        });
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(px - core, py - core, core * 2f, core * 2f),
            Style = new RectStyle { BackgroundColor = _pointColor, BorderRadius = BorderRadiusStyle.All(core) },
            ZIndex = z + 8,
        });
    }

    private static void FillRect(ICanvas c, in RectF rect, uint color, int z)
    {
        c.DrawRect(new DrawRectInputs
        {
            Position = rect,
            Style = new RectStyle { BackgroundColor = color },
            ZIndex = z,
        });
    }

    private static void StrokeRect(ICanvas c, in RectF rect, uint color, float thickness, int z)
    {
        FillRect(c, new RectF(rect.Left, rect.Top - thickness, rect.Width, thickness), color, z);     // top
        FillRect(c, new RectF(rect.Left, rect.Bottom, rect.Width, thickness), color, z);              // bottom
        FillRect(c, new RectF(rect.Left, rect.Bottom, thickness, rect.Height), color, z);             // left
        FillRect(c, new RectF(rect.Right - thickness, rect.Bottom, thickness, rect.Height), color, z); // right
    }
}

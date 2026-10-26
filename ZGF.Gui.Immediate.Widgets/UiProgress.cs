using ZGF.Geometry;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private const double AnimationStep = 1.0 / 30.0;
    private const double SpinnerPeriod = 0.9;
    private const int SpinnerDots = 8;
    private const double SlidePeriod = 1.4;
    private const float SlideShare = 0.3f;

    /// <summary>Dots in a ring <paramref name="size"/> across, <see cref="UiTheme.IconSize"/> by
    /// default, the brightest going round once every <see cref="SpinnerPeriod"/> and the others fading
    /// toward <paramref name="background"/>, the color behind them. Asks for the frames that turn it.</summary>
    public static void Spinner(this Ui ui, uint background, float? size = null, uint? color = null)
    {
        var theme = ui.Theme;
        var side = size ?? theme.IconSize;
        var phase = (float)(ui.RealTime % SpinnerPeriod / SpinnerPeriod);
        var ink = color ?? theme.AccentText;
        using (ui.Box(new Box { Width = Sizing.Fixed(side), Height = Sizing.Fixed(side) }))
            ui.Paint((phase, ink, background), static (p, s) => PaintSpinner(p, s.phase, s.ink, s.background));
        ui.RequestUpdateIn(AnimationStep);
    }

    private static void PaintSpinner(UiPainter p, float phase, uint color, uint background)
    {
        var r = p.Rect;
        var center = new PointF(r.X + r.Width / 2, r.Y + r.Height / 2);
        var ring = r.Width * 0.38f;
        var dot = r.Width * 0.09f;
        for (var i = 0; i < SpinnerDots; i++)
        {
            var share = (float)i / SpinnerDots;
            var angle = share * MathF.Tau;
            var at = new PointF(center.X + MathF.Cos(angle) * ring, center.Y + MathF.Sin(angle) * ring);
            var behind = (phase - share + 1f) % 1f;
            var shade = UiColor.Mix(color, background, behind * 0.85f);
            p.Circle(at, dot, shade);
        }
    }

    /// <summary>A <see cref="ProgressBar"/> for work of unknown size, <paramref name="height"/> tall,
    /// <see cref="UiTheme.ProgressHeight"/> by default: a segment sliding along it once every
    /// <see cref="SlidePeriod"/>. Asks for the frames that move it.</summary>
    public static void SlidingBar(this Ui ui, Sizing? width = null, float? height = null)
    {
        var theme = ui.Theme;
        var tall = height ?? theme.ProgressHeight;
        var phase = (float)(ui.RealTime % SlidePeriod / SlidePeriod);
        using (ui.Box(new Box { Width = width ?? Sizing.Grow(), Height = tall, Background = theme.Control, Radius = tall / 2 }))
            ui.Paint((phase, theme.Accent), static (p, s) => PaintSlide(p, s.phase, s.Accent));
        ui.RequestUpdateIn(AnimationStep);
    }

    private static void PaintSlide(UiPainter p, float phase, uint color)
    {
        var r = p.Rect;
        var width = r.Width * SlideShare;
        var travel = r.Width + width;
        var left = r.X - width + travel * phase;
        var start = MathF.Max(r.X, left);
        var end = MathF.Min(r.X + r.Width, left + width);
        if (end <= start)
            return;
        p.FillRect(new UiRect(start, r.Y, end - start, r.Height), color, r.Height / 2);
    }
}

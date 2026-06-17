using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

public sealed record Text : Widget
{
    /// <summary>The text: a constant, an observable, a projection, or a compute (see <see cref="Prop{T}"/>).</summary>
    public Prop<string?> Value { get; init; }
    public Prop<float> FontSize { get; init; }
    public Prop<string> FontFamily { get; init; }
    public Prop<FontWeight> Weight { get; init; }
    public Prop<TextWrap> Wrap { get; init; }
    public Prop<uint> Color { get; init; }
    public Prop<TextAlignment> HAlign { get; init; }
    public Prop<TextAlignment> VAlign { get; init; }

    protected override View CreateView(Context ctx)
    {
        var v = new TextView(ctx.Canvas);
        Value.Apply(v, static (x, t) => x.Text = t);
        FontSize.Apply(v, static (x, s) => x.FontSize = s);
        FontFamily.Apply(v, static (x, f) => x.FontFamily = f);
        Weight.Apply(v, static (x, w) => x.FontWeight = w);
        Wrap.Apply(v, static (x, w) => x.TextWrap = w);
        Color.Apply(v, static (x, c) => x.TextColor = c);
        HAlign.Apply(v, static (x, a) => x.HorizontalTextAlignment = a);
        VAlign.Apply(v, static (x, a) => x.VerticalTextAlignment = a);
        return v;
    }
}
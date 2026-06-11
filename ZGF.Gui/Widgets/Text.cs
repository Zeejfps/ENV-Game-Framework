using ZGF.Gui.Bindings;
using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

public sealed record Text : Widget
{
    public string? Value { get; init; }
    public StyleValue<float> FontSize { get; init; }
    public StyleValue<uint> Color { get; init; }
    public StyleValue<TextAlignment> HAlign { get; init; }
    public StyleValue<TextAlignment> VAlign { get; init; }

    /// <summary>Auto-tracked text binding; overrides <see cref="Value"/> once attached.</summary>
    public Func<string?>? Bind { get; init; }

    /// <summary>Auto-tracked color binding.</summary>
    public Func<uint>? BindColor { get; init; }

    protected override View CreateView(Context ctx)
    {
        var v = new TextView(ctx.Canvas) { Text = Value };
        if (FontSize.IsSet) v.FontSize = FontSize;
        if (Color.IsSet) v.TextColor = Color;
        if (HAlign.IsSet) v.HorizontalTextAlignment = HAlign;
        if (VAlign.IsSet) v.VerticalTextAlignment = VAlign;
        if (Bind != null) v.BindText(Bind);
        if (BindColor != null) v.BindTextColor(BindColor);
        return v;
    }
}
namespace ZGF.Gui;

/// <summary>
/// Output of the cascade — plain typed values (no <see cref="StyleValue{T}"/> Set flag),
/// defaults baked in. A view holds one of these and the renderer / property getters
/// read from it. The cascade rebuilds it in place each time classes / modifiers /
/// sheet / local style change.
/// </summary>
public sealed class ResolvedStyle
{
    // --- Visual ---
    public uint TintColor { get; set; } = 0xFFFFFFFF;
    public float Rotation { get; set; } = 0f;
    public uint BackgroundColor { get; set; } = 0x00000000;
    public PaddingStyle Padding { get; set; }
    public BorderSizeStyle BorderSize { get; set; }
    public BorderColorStyle BorderColor { get; set; }
    public BorderRadiusStyle BorderRadius { get; set; }
    public BoxShadowStyle BoxShadow { get; set; }

    // --- Text ---
    public uint TextColor { get; set; } = 0xFF000000;
    public string? FontFamily { get; set; }
    public float FontSize { get; set; } = 14f;
    public FontWeight FontWeight { get; set; } = ZGF.Gui.FontWeight.Normal;
    public TextAlignment HorizontalAlignment { get; set; } = TextAlignment.Start;
    public TextAlignment VerticalAlignment { get; set; } = TextAlignment.Start;
    public TextWrap TextWrap { get; set; } = ZGF.Gui.TextWrap.NoWrap;

    // --- Layout ---
    public StyleValue<float> PreferredWidth { get; set; }
    public StyleValue<float> PreferredHeight { get; set; }

    /// <summary>
    /// Reset every field to its default. Called at the start of each cascade run before
    /// re-layering defaults → sheet rules → local style.
    /// </summary>
    public void ResetToDefaults()
    {
        TintColor = 0xFFFFFFFF;
        Rotation = 0f;
        BackgroundColor = 0x00000000;
        Padding = default;
        BorderSize = default;
        BorderColor = default;
        BorderRadius = default;
        BoxShadow = default;

        TextColor = 0xFF000000;
        FontFamily = null;
        FontSize = 14f;
        FontWeight = ZGF.Gui.FontWeight.Normal;
        HorizontalAlignment = TextAlignment.Start;
        VerticalAlignment = TextAlignment.Start;
        TextWrap = ZGF.Gui.TextWrap.NoWrap;

        PreferredWidth = default;
        PreferredHeight = default;
    }

    /// <summary>
    /// Overlay <paramref name="style"/>'s set fields onto this resolved style. Fields whose
    /// <see cref="StyleValue{T}.IsSet"/> is false are skipped. The flat (StyleValue) sub-style
    /// values (padding, border, etc.) handle their own per-edge "only set sides override" via
    /// their <c>ApplyTo</c> methods.
    /// </summary>
    public void Apply(Style style)
    {
        // Visual
        if (style.TintColor.IsSet) TintColor = style.TintColor.Value;
        if (style.Rotation.IsSet) Rotation = style.Rotation.Value;
        if (style.BackgroundColor.IsSet) BackgroundColor = style.BackgroundColor.Value;
        var padding = Padding; style.Padding.ApplyTo(ref padding); Padding = padding;
        var borderSize = BorderSize; style.BorderSize.ApplyTo(ref borderSize); BorderSize = borderSize;
        var borderColor = BorderColor; style.BorderColor.ApplyTo(ref borderColor); BorderColor = borderColor;
        var borderRadius = BorderRadius; style.BorderRadius.ApplyTo(ref borderRadius); BorderRadius = borderRadius;
        var boxShadow = BoxShadow; style.BoxShadow.ApplyTo(ref boxShadow); BoxShadow = boxShadow;

        // Text
        if (style.TextColor.IsSet) TextColor = style.TextColor.Value;
        if (style.FontFamily.IsSet) FontFamily = style.FontFamily.Value;
        if (style.FontSize.IsSet) FontSize = style.FontSize.Value;
        if (style.FontWeight.IsSet) FontWeight = style.FontWeight.Value;
        if (style.HorizontalAlignment.IsSet) HorizontalAlignment = style.HorizontalAlignment.Value;
        if (style.VerticalAlignment.IsSet) VerticalAlignment = style.VerticalAlignment.Value;
        if (style.TextWrap.IsSet) TextWrap = style.TextWrap.Value;

        // Layout
        if (style.PreferredWidth.IsSet) PreferredWidth = style.PreferredWidth;
        if (style.PreferredHeight.IsSet) PreferredHeight = style.PreferredHeight;
    }
}

using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Round-trip dark/light toggle for the top toolbar. The icon flips to the theme the user
/// would land on if they clicked: shows a Sun while Dark is active (click → Light), shows
/// a Moon while Light is active (click → Dark). Hooks <see cref="IThemeService.SetTheme"/>
/// directly; no view-model needed.
/// </summary>
internal sealed class ThemeToggleButton : HoverableButton
{
    private const float ButtonSize = 28f;
    private const float IconSize = 16f;

    private IThemeService? _service;

    public ThemeToggleButton() : base(null, tooltip: "Toggle light / dark theme")
    {
        PreferredWidth = ButtonSize;
        PreferredHeight = ButtonSize;

        var icon = new TextView
        {
            FontFamily = LucideIcons.FontFamily,
            FontSize = IconSize,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        icon.BindTextColorFromTheme(t => t.Text.Row);
        icon.BindToTheme(t => icon.Text = IsCurrentlyDark(t) ? LucideIcons.Sun : LucideIcons.Moon);

        var hoverBg = new State<uint>(ThemePresets.Dark.Dialog.ButtonHover);
        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Children = { icon },
        };
        background.BindToTheme(t => hoverBg.Value = t.Dialog.ButtonHover);
        background.BindBackgroundColor(() => IsHovered.Value ? hoverBg.Value : 0x00000000u);
        SetBackground(background);
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        _service = context.Get<IThemeService>();
    }

    protected override void OnClicked()
    {
        if (_service == null) return;
        var next = IsCurrentlyDark(_service.Tokens.Value) ? ThemePresets.Light : ThemePresets.Dark;
        _service.SetTheme(next);
    }

    // Identify the active theme by reference rather than scanning fields — ThemePresets
    // hands out the same singletons for Dark / Light, so reference equality is reliable.
    private static bool IsCurrentlyDark(ThemeTokens tokens) =>
        ReferenceEquals(tokens, ThemePresets.Dark);
}

namespace ZGF.Gui.Immediate;

/// <summary>
/// The palette and metrics a UI is drawn with. Colors are named by role, not by widget. State
/// roles (hover, press, selection, scrollbars) can be set like any other; a role left unset is
/// derived from the base roles, so a theme can be as little as its base roles.
/// Everything recorded inside <c>ui.Provide(theme)</c> uses that theme, and
/// <see cref="Dark"/> outside any. A provided theme replaces the whole theme; to change only some
/// tokens, provide <c>ui.Theme with { ... }</c>, and <see cref="WithDensity"/> for a whole set of
/// metrics.
/// </summary>
public sealed record UiTheme
{
    private readonly uint _surfaceRaised;
    private readonly uint _editorBackground;
    private readonly uint _borderStrong;
    private readonly uint _separator;
    private readonly uint _textDisabled;
    private readonly uint _accentText;
    private readonly uint _accentHovered;
    private readonly uint _accentPressed;
    private readonly uint _focusRing;
    private readonly uint _controlHovered;
    private readonly uint _controlPressed;
    private readonly uint _rowHover;
    private readonly uint _selection;
    private readonly uint _selectionInactive;
    private readonly uint _selectionText;
    private readonly uint _danger;
    private readonly uint _dangerSurface;
    private readonly uint _onDanger;
    private readonly uint _warning;
    private readonly uint _warningSurface;
    private readonly uint _success;
    private readonly uint _scrim;
    private readonly uint _scrollThumb;
    private readonly uint _scrollThumbHovered;
    private readonly uint _scrollThumbHeld;

    /// <summary>The workspace: the window, trees and lists.</summary>
    public uint Background { get; init; }

    /// <summary>Docked chrome: menu bar, toolbar, tab strip, status bar, panels.</summary>
    public uint Surface { get; init; }

    /// <summary>Floating surfaces: menus, tooltips, dialogs.</summary>
    public uint SurfaceRaised { get => Or(_surfaceRaised, Surface); init => _surfaceRaised = value; }

    /// <summary>The base of neutral buttons and segments.</summary>
    public uint Control { get; init; }

    public uint ControlHovered { get => Or(_controlHovered, UiColor.Mix(Control, Text, 0.06f)); init => _controlHovered = value; }
    public uint ControlPressed { get => Or(_controlPressed, UiColor.Mix(Control, Text, 0.12f)); init => _controlPressed = value; }

    /// <summary>Editable fields.</summary>
    public uint Field { get; init; }

    /// <summary>A document or code view's background.</summary>
    public uint EditorBackground { get => Or(_editorBackground, Field); init => _editorBackground = value; }

    /// <summary>Component outlines.</summary>
    public uint Border { get; init; }

    /// <summary>An outline that answers the pointer: a hovered field, a hovered splitter.</summary>
    public uint BorderStrong { get => Or(_borderStrong, UiColor.Mix(Border, Text, 0.4f)); init => _borderStrong = value; }

    /// <summary>Structural divisions between panes and groups.</summary>
    public uint Separator { get => Or(_separator, Border); init => _separator = value; }

    public uint Text { get; init; }

    /// <summary>Secondary text: metadata, placeholders, shortcuts.</summary>
    public uint TextMuted { get; init; }

    /// <summary>Text of a control that cannot be used.</summary>
    public uint TextDisabled { get => Or(_textDisabled, UiColor.Mix(TextMuted, Background, 0.35f)); init => _textDisabled = value; }

    /// <summary>Primary filled actions and checked state.</summary>
    public uint Accent { get; init; }

    public uint AccentHovered { get => Or(_accentHovered, UiColor.Mix(Accent, OnAccent, 0.1f)); init => _accentHovered = value; }
    public uint AccentPressed { get => Or(_accentPressed, UiColor.Mix(Accent, Background, 0.15f)); init => _accentPressed = value; }

    /// <summary>Text drawn on <see cref="Accent"/>.</summary>
    public uint OnAccent { get; init; }

    /// <summary>Links and activity text, readable on <see cref="Background"/>.</summary>
    public uint AccentText { get => Or(_accentText, Accent); init => _accentText = value; }

    /// <summary>The ring a widget holding keyboard focus draws.</summary>
    public uint FocusRing { get => Or(_focusRing, Accent); init => _focusRing = value; }

    /// <summary>An unselected row under the pointer.</summary>
    public uint RowHover { get => Or(_rowHover, UiColor.Mix(Background, Text, 0.06f)); init => _rowHover = value; }

    /// <summary>Selected rows, segments and highlighted menu items in the focused pane.</summary>
    public uint Selection { get => Or(_selection, UiColor.Mix(Field, Accent, 0.35f)); init => _selection = value; }

    /// <summary>A selection whose pane does not hold focus.</summary>
    public uint SelectionInactive { get => Or(_selectionInactive, UiColor.Mix(Background, Text, 0.12f)); init => _selectionInactive = value; }

    /// <summary>Text on <see cref="Selection"/> and <see cref="SelectionInactive"/>.</summary>
    public uint SelectionText { get => Or(_selectionText, Text); init => _selectionText = value; }

    public uint Danger { get => Or(_danger, 0xFFE5484D); init => _danger = value; }
    public uint DangerSurface { get => Or(_dangerSurface, UiColor.Mix(Background, Danger, 0.2f)); init => _dangerSurface = value; }

    /// <summary>Text on a solid <see cref="Danger"/> badge.</summary>
    public uint OnDanger { get => Or(_onDanger, Background); init => _onDanger = value; }

    public uint Warning { get => Or(_warning, 0xFFD4A03C); init => _warning = value; }
    public uint WarningSurface { get => Or(_warningSurface, UiColor.Mix(Background, Warning, 0.2f)); init => _warningSurface = value; }
    public uint Success { get => Or(_success, 0xFF3FA35B); init => _success = value; }

    /// <summary>The backdrop dimming what is behind a modal dialog.</summary>
    public uint Scrim { get => Or(_scrim, 0x80000000); init => _scrim = value; }

    public uint ScrollThumb { get => Or(_scrollThumb, UiColor.Mix(Background, Text, 0.15f)); init => _scrollThumb = value; }
    public uint ScrollThumbHovered { get => Or(_scrollThumbHovered, UiColor.Mix(Background, Text, 0.25f)); init => _scrollThumbHovered = value; }
    public uint ScrollThumbHeld { get => Or(_scrollThumbHeld, UiColor.Mix(Background, Text, 0.5f)); init => _scrollThumbHeld = value; }

    /// <summary>The shadow under menus, context menus and tooltips.</summary>
    public UiShadow PopupShadow { get; init; } = new(0f, 4f, 12f, 0f, 0x66000000);

    /// <summary>The shadow under a modal dialog's card.</summary>
    public UiShadow DialogShadow { get; init; } = new(0f, 8f, 24f, 0f, 0x80000000);

    public float FontSize { get; init; } = 14f;
    public float FontSizeSmall { get; init; } = 12f;
    public float FontSizeTitle { get; init; } = 16f;
    public float MonoFontSize { get; init; } = 15f;
    public float CodeLineHeight { get; init; } = 22f;

    /// <summary>The family text is drawn in unless a call names one; null is the default font.
    /// Families are registered on <see cref="Gui{T}"/> before it runs.</summary>
    public string? FontFamily { get; init; }

    /// <summary>General gap between laid-out siblings.</summary>
    public float Gap { get; init; } = 8f;

    /// <summary>Between related controls, and between an icon and its label.</summary>
    public float GapTight { get; init; } = 4f;

    /// <summary>The total space between groups of a toolbar.</summary>
    public float GroupGap { get; init; } = 12f;

    public float ScreenPadding { get; init; } = 16f;
    public float PanelPadding { get; init; } = 12f;

    /// <summary>The horizontal inset of shell chrome: menu bar, toolbar, pane headers, status bar.</summary>
    public float ShellPaddingX { get; init; } = 8f;

    /// <summary>The horizontal inset of a text button's or segment's label.</summary>
    public float ControlPaddingX { get; init; } = 12f;

    /// <summary>The horizontal inset of a text field's text, caret and selection.</summary>
    public float FieldPaddingX { get; init; } = 8f;

    /// <summary>The vertical inset of controls on a toolbar.</summary>
    public float ToolbarPaddingY { get; init; } = 4f;

    public float ControlHeight { get; init; } = 32f;
    public float IconButtonSize { get; init; } = 32f;
    public float RowHeight { get; init; } = 28f;
    public float MenuItemHeight { get; init; } = 32f;
    public float MenuBarHeight { get; init; } = 32f;
    public float ToolbarHeight { get; init; } = 40f;
    public float TabHeight { get; init; } = 36f;
    public float PaneHeaderHeight { get; init; } = 36f;
    public float StatusBarHeight { get; init; } = 28f;

    public float BorderWidth { get; init; } = 1f;
    public float SeparatorWidth { get; init; } = 1f;
    public float FocusRingWidth { get; init; } = 2f;

    /// <summary>How far inside a widget's edge its focus ring is drawn; zero draws it over the
    /// widget's border.</summary>
    public float FocusRingInset { get; init; }

    /// <summary>The thickness of a selected tab's marker.</summary>
    public float ActiveIndicatorWidth { get; init; } = 2f;

    /// <summary>The stroke of line-drawn glyphs: arrows, crosses, check marks.</summary>
    public float IconStrokeWidth { get; init; } = 1.5f;

    /// <summary>Badges and checkbox corners.</summary>
    public float RadiusSmall { get; init; } = 2f;

    /// <summary>Buttons, fields, segmented controls, menu-item highlights.</summary>
    public float CornerRadius { get; init; } = 4f;

    /// <summary>Bordered inset panels.</summary>
    public float PanelRadius { get; init; } = 4f;

    /// <summary>Menus, context menus, tooltips.</summary>
    public float PopupRadius { get; init; } = 8f;

    /// <summary>Modal dialog cards.</summary>
    public float DialogRadius { get; init; } = 12f;

    public float IconSize { get; init; } = 16f;

    /// <summary>A glyph laid over an icon's corner: a tree row's lock or status mark.</summary>
    public float IconOverlaySize { get; init; } = 10f;
    public float CheckboxSize { get; init; } = 16f;
    public float SliderThumbSize { get; init; } = 16f;
    public float SliderTrackHeight { get; init; } = 4f;
    public float ProgressHeight { get; init; } = 8f;
    public float CaretWidth { get; init; } = 1.5f;

    public float TreeIndent { get; init; } = 16f;

    /// <summary>The inset of a tree row before its disclosure column.</summary>
    public float TreePaddingX { get; init; } = 4f;
    public float DisclosureWidth { get; init; } = 16f;

    /// <summary>The inset of a menu's rows from its edge.</summary>
    public float MenuPadding { get; init; } = 4f;
    public float MenuItemPaddingX { get; init; } = 8f;

    /// <summary>The leading column of a menu item, where its check mark sits.</summary>
    public float MenuIconColumnWidth { get; init; } = 24f;

    /// <summary>The least space between a menu item's label and its shortcut.</summary>
    public float MenuShortcutGap { get; init; } = 24f;
    public float MenuMinWidth { get; init; } = 192f;

    public float TabPaddingX { get; init; } = 12f;
    public float TabCloseSize { get; init; } = 24f;
    public float TabMinWidth { get; init; } = 80f;
    public float TabMaxWidth { get; init; } = 240f;

    public float DialogWidth { get; init; } = 480f;
    public float DialogPadding { get; init; } = 24f;
    public float DialogGap { get; init; } = 16f;

    /// <summary>The least space kept between a dialog's card and the window's edge.</summary>
    public float DialogMargin { get; init; } = 24f;

    public float TooltipPaddingX { get; init; } = 8f;
    public float TooltipPaddingY { get; init; } = 4f;
    public float TooltipMaxWidth { get; init; } = 360f;

    /// <summary>The space between a tooltip and what it describes.</summary>
    public float TooltipGap { get; init; } = 4f;
    public double TooltipDelaySeconds { get; init; } = 0.5;

    /// <summary>The draggable thickness of a split pane's divider; its painted line is
    /// <see cref="SeparatorWidth"/>.</summary>
    public float SplitterSize { get; init; } = 6f;

    /// <summary>The painted thickness of a scroll thumb.</summary>
    public float ScrollBarWidth { get; init; } = 8f;

    /// <summary>The thickness of a scroll thumb the pointer can grab.</summary>
    public float ScrollBarHitWidth { get; init; } = 12f;

    /// <summary>The space between a scroll thumb and the edges of its gutter.</summary>
    public float ScrollBarInset { get; init; } = 2f;
    public float ScrollThumbMinLength { get; init; } = 24f;
    public float ScrollWheelStep { get; init; } = 40f;

    /// <summary>This theme's palette with <paramref name="density"/>'s control geometry and type
    /// sizes; everything else is kept.</summary>
    public UiTheme WithDensity(UiDensity density) => density == UiDensity.Compact
        ? this with
        {
            ControlHeight = 28f,
            IconButtonSize = 28f,
            RowHeight = 24f,
            MenuItemHeight = 28f,
            MenuBarHeight = 28f,
            ToolbarHeight = 36f,
            TabHeight = 32f,
            PaneHeaderHeight = 32f,
            StatusBarHeight = 24f,
            FontSize = 13f,
            FontSizeSmall = 12f,
            FontSizeTitle = 16f,
            MonoFontSize = 14f,
            CodeLineHeight = 20f,
        }
        : this with
        {
            ControlHeight = 32f,
            IconButtonSize = 32f,
            RowHeight = 28f,
            MenuItemHeight = 32f,
            MenuBarHeight = 32f,
            ToolbarHeight = 40f,
            TabHeight = 36f,
            PaneHeaderHeight = 36f,
            StatusBarHeight = 28f,
            FontSize = 14f,
            FontSizeSmall = 12f,
            FontSizeTitle = 16f,
            MonoFontSize = 15f,
            CodeLineHeight = 22f,
        };

    /// <summary><paramref name="color"/> under the pointer: <see cref="ControlHovered"/> for
    /// <see cref="Control"/>, <see cref="AccentHovered"/> for <see cref="Accent"/>, else a step toward
    /// <see cref="Text"/>.</summary>
    public uint Hovered(uint color) =>
        color == Control ? ControlHovered : color == Accent ? AccentHovered : UiColor.Mix(color, Text, 0.06f);

    /// <summary><paramref name="color"/> while pressed, as <see cref="Hovered"/> picks.</summary>
    public uint Pressed(uint color) =>
        color == Control ? ControlPressed : color == Accent ? AccentPressed : UiColor.Mix(color, Text, 0.12f);

    /// <summary>True when <see cref="Background"/> is dark, for platform chrome that follows the theme.</summary>
    public bool IsDark => UiColor.IsDark(Background);

    private static uint Or(uint value, uint fallback) => value != 0 ? value : fallback;

    public static UiTheme Dark { get; } = new()
    {
        Background = 0xFF1B1E24,
        Surface = 0xFF22252C,
        SurfaceRaised = 0xFF2A2F38,
        Control = 0xFF303641,
        ControlHovered = 0xFF3A414D,
        ControlPressed = 0xFF424B59,
        Field = 0xFF191C22,
        EditorBackground = 0xFF191C22,
        Border = 0xFF4B5362,
        BorderStrong = 0xFF8B95A5,
        Separator = 0xFF343A45,
        Text = 0xFFE4E7ED,
        TextMuted = 0xFFA6ACB8,
        TextDisabled = 0xFF747D8C,
        Accent = 0xFF356FE0,
        AccentHovered = 0xFF4079E8,
        AccentPressed = 0xFF2D62C9,
        OnAccent = 0xFFFFFFFF,
        AccentText = 0xFF8AB4FF,
        FocusRing = 0xFF7AA7FF,
        RowHover = 0xFF2B303A,
        Selection = 0xFF293D60,
        SelectionInactive = 0xFF303641,
        SelectionText = 0xFFE4E7ED,
        Danger = 0xFFFF9A9A,
        DangerSurface = 0xFF472A32,
        OnDanger = 0xFF29171B,
        Warning = 0xFFE6BE73,
        WarningSurface = 0xFF443821,
        Success = 0xFF8BC99C,
        Scrim = 0x99000000,
        ScrollThumb = 0xFF626D80,
        ScrollThumbHovered = 0xFF7B879B,
        ScrollThumbHeld = 0xFF95A4BD,
        PopupShadow = new UiShadow(0f, 4f, 12f, 0f, 0x66000000),
        DialogShadow = new UiShadow(0f, 8f, 24f, 0f, 0x80000000),
    };

    public static UiTheme Light { get; } = new()
    {
        Background = 0xFFF7F8FA,
        Surface = 0xFFF0F2F5,
        SurfaceRaised = 0xFFFFFFFF,
        Control = 0xFFE7EBF1,
        ControlHovered = 0xFFDDE3EB,
        ControlPressed = 0xFFCFD7E3,
        Field = 0xFFFFFFFF,
        EditorBackground = 0xFFFFFFFF,
        Border = 0xFFBBC3CF,
        BorderStrong = 0xFF7D8796,
        Separator = 0xFFD9DFE7,
        Text = 0xFF242A35,
        TextMuted = 0xFF5D6675,
        TextDisabled = 0xFF9098A4,
        Accent = 0xFF245FCC,
        AccentHovered = 0xFF2B6BE0,
        AccentPressed = 0xFF1E51B0,
        OnAccent = 0xFFFFFFFF,
        AccentText = 0xFF245FCC,
        FocusRing = 0xFF245FCC,
        RowHover = 0xFFE8ECF2,
        Selection = 0xFFDCE8FF,
        SelectionInactive = 0xFFE5E8ED,
        SelectionText = 0xFF242A35,
        Danger = 0xFFB42332,
        DangerSurface = 0xFFFDE7EB,
        OnDanger = 0xFFFFFFFF,
        Warning = 0xFF805500,
        WarningSurface = 0xFFFFF0CC,
        Success = 0xFF286B3D,
        Scrim = 0x59000000,
        ScrollThumb = 0xFF8793A6,
        ScrollThumbHovered = 0xFF69768B,
        ScrollThumbHeld = 0xFF506078,
        PopupShadow = new UiShadow(0f, 4f, 12f, 0f, 0x24000000),
        DialogShadow = new UiShadow(0f, 8f, 24f, 0f, 0x33000000),
    };
}

/// <summary>A set of control geometry and type sizes for <see cref="UiTheme.WithDensity"/>.</summary>
public enum UiDensity
{
    Compact,
    Comfortable,
}

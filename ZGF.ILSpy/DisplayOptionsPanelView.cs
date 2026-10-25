using System.Globalization;
using System.Runtime.CompilerServices;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.CodeDisplay;
using static ZGF.ILSpy.IconTheming;
using static ZGF.ILSpy.Preferences;
using static ZGF.ILSpy.Theming;

namespace ZGF.ILSpy;

/// <summary>How the app is drawn, as settings cards: the interface's theme, icon theme, scale and language, then the
/// code's font size and switches for line numbers, the current line's highlight and matching brackets,
/// then how the tree writes metadata tokens. Each option is a row of its card, named on the left, with its control at the right. Every change
/// shows at once; an option changed from its default has a bold name.</summary>
internal static class DisplayOptionsPanelView
{
    private const string PaneId = "display-options";
    private const string FontSizeFormat = "0.#";
    private const float FormMaxWidth = 560f;
    private const string RowSuffix = "-row";
    private const float FontPickerMaxWidth = 220f;
    private const string PreviewText = "The quick brown fox jumps over the lazy dog";

    private const int NothingPicked = -1;

    private static readonly ConditionalWeakTable<Strings, string[]> UiScaleLabels = new();
    private static readonly ConditionalWeakTable<Strings, string[]> TokenFormatLabels = new();
    private static readonly TokenFormat[] TokenFormats = Enum.GetValues<TokenFormat>();
    private static readonly string[] LocaleLabels = ListLocaleLabels();

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var theme = ui.Theme;
        using var pane = ui.Scroll(PaneId, new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f });
        using var form = ui.Column(new Box
        {
            Width = Sizing.Grow(max: FormMaxWidth),
            Gap = theme.GroupGap,
            Padding = theme.PanelPadding,
        });
        ListInstalledFontsOnce(ui);
        InterfaceSection(ui, appState);
        CodeSection(ui);
        TreeSection(ui);
        ResetRow(ui);
    }

    private static void ListInstalledFontsOnce(Ui<UiState> ui)
    {
        var screen = ui.State;
        if (!screen.CodeFonts.TakeListingRequest())
            return;
        ui.ListSystemFonts(families => KeepInstalledFonts(screen, families));
    }

    /// <summary>The theme, the icon theme, the scale and the language. Only the scale is one of the
    /// options a reset puts back, so only its name turns bold.</summary>
    private static void InterfaceSection(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var section = Section(ui, ui.Memo("interface", t, static s => Heading(s.DisplayOptionsInterface)));
        using var card = Card(ui);
        using (OptionRow(ui, t.DisplayOptionsTheme, null, false))
            ThemePicker(ui, appState);
        ui.Separator();

        using (OptionRow(ui, t.DisplayOptionsIconTheme, null, false))
            IconThemePicker(ui, appState);
        ui.Separator();

        var changed = ui.State.Display.UiScale != DisplayOptions.Default.UiScale;
        using (OptionRow(ui, t.DisplayOptionsUiScale, t.DisplayOptionsUiScaleDescription, changed))
            UiScalePicker(ui);
        ui.Separator();

        using (OptionRow(ui, t.DisplayOptionsLanguage, null, false))
            LanguagePicker(ui, appState);
    }

    /// <summary>The themes of the catalog, built-in ones first, then the buttons that start a custom theme
    /// from the one shown, open the themes folder and read the user's themes again, while the settings
    /// have a folder to keep themes in.</summary>
    private static void ThemePicker(Ui<UiState> ui, AppState appState)
    {
        using var controls = ui.Row(new Box { Width = Sizing.Fit(), Gap = ui.Theme.GapTight, AlignY = Align.Center });
        PickTheme(ui, appState);
        if (appState.Settings.ThemesFolder == null)
            return;
        CustomizeThemeButton(ui, appState);
        OpenThemesFolderButton(ui, appState);
        ReloadThemesButton(ui, appState);
    }

    private static void PickTheme(Ui<UiState> ui, AppState appState)
    {
        var themes = appState.Themes;
        var labels = themes.ListNames(ui.State.Strings.Culture);
        var current = themes.IndexOf(appState.Theme);
        var picked = PickFrom(ui, "theme", labels, current, themes.Ids, themes.Revision);
        if (picked != NothingPicked)
            SetTheme(appState, themes.Ids[picked]);
    }

    /// <summary>Starts a theme of the user's own from the one shown, switches to it and opens its file.</summary>
    private static void CustomizeThemeButton(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        if (!ui.IconButton("customize-theme", ui.State.Icons.OptionsCustomizeTheme, t.DisplayOptionsCustomizeTheme, variant: ButtonVariant.Neutral))
            return;
        var file = CreateCustomTheme(appState);
        if (file != null)
            ui.OpenFile(file);
    }

    private static void OpenThemesFolderButton(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        if (!ui.IconButton("themes-folder", ui.State.Icons.OptionsOpenThemesFolder, t.DisplayOptionsOpenThemesFolder, variant: ButtonVariant.Neutral))
            return;
        var folder = PrepareThemesFolder(appState);
        if (folder != null)
            ui.OpenFolder(folder);
    }

    private static void ReloadThemesButton(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        if (ui.IconButton("reload-themes", ui.State.Icons.OptionsReloadThemes, t.DisplayOptionsReloadThemes, variant: ButtonVariant.Neutral))
            ReloadUserThemes(appState);
    }

    /// <summary>The icon themes of the catalog, built-in ones first, then the buttons that start a custom
    /// icon theme from the one shown, open the icon themes folder and read the user's icon themes again,
    /// while the settings have a folder to keep icon themes in.</summary>
    private static void IconThemePicker(Ui<UiState> ui, AppState appState)
    {
        using var controls = ui.Row(new Box { Width = Sizing.Fit(), Gap = ui.Theme.GapTight, AlignY = Align.Center });
        PickIconTheme(ui, appState);
        if (appState.Settings.IconThemesFolder == null)
            return;
        CustomizeIconThemeButton(ui, appState);
        OpenIconThemesFolderButton(ui, appState);
        ReloadIconThemesButton(ui, appState);
    }

    private static void PickIconTheme(Ui<UiState> ui, AppState appState)
    {
        var themes = appState.IconThemes;
        var labels = themes.ListNames(ui.State.Strings.Culture);
        var current = themes.IndexOf(appState.IconTheme);
        var picked = PickFrom(ui, "icon-theme", labels, current, themes.Ids, themes.Revision);
        if (picked != NothingPicked)
            SetIconTheme(appState, themes.Ids[picked]);
    }

    /// <summary>Starts an icon theme of the user's own from the one shown, switches to it and opens its file.</summary>
    private static void CustomizeIconThemeButton(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        if (!ui.IconButton("customize-icon-theme", ui.State.Icons.OptionsCustomizeIconTheme, t.DisplayOptionsCustomizeIconTheme, variant: ButtonVariant.Neutral))
            return;
        var file = CreateCustomIconTheme(appState);
        if (file != null)
            ui.OpenFile(file);
    }

    private static void OpenIconThemesFolderButton(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        if (!ui.IconButton("icon-themes-folder", ui.State.Icons.OptionsOpenIconThemesFolder, t.DisplayOptionsOpenIconThemesFolder, variant: ButtonVariant.Neutral))
            return;
        var folder = PrepareIconThemesFolder(appState);
        if (folder != null)
            ui.OpenFolder(folder);
    }

    private static void ReloadIconThemesButton(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        if (ui.IconButton("reload-icon-themes", ui.State.Icons.OptionsReloadIconThemes, t.DisplayOptionsReloadIconThemes, variant: ButtonVariant.Neutral))
            ReloadUserIconThemes(appState);
    }

    private static void UiScalePicker(Ui<UiState> ui)
    {
        var labels = UiScaleLabels.GetValue(ui.State.Strings, ListUiScaleLabels);
        var scales = DisplayOptions.UiScales;
        var current = IndexOfScale(scales, ui.State.Display.UiScale);
        var picked = PickFrom(ui, "ui-scale", labels, current);
        if (picked != NothingPicked)
            SetUiScale(ui.State, scales[picked]);
    }

    private static int IndexOfScale(IReadOnlyList<float> scales, float scale)
    {
        for (var i = 0; i < scales.Count; i++)
        {
            if (scales[i] == scale)
                return i;
        }
        return NothingPicked;
    }

    private static string[] ListUiScaleLabels(Strings t)
    {
        return DisplayOptions.UiScales
            .Select(s => UiScaleLabel(t, s))
            .ToArray();
    }

    private static string UiScaleLabel(Strings t, float scale)
    {
        var percent = (int)MathF.Round(scale * 100f);
        return t.DisplayOptionsUiScalePercent(percent);
    }

    private static void LanguagePicker(Ui<UiState> ui, AppState appState)
    {
        var locales = Locales.All;
        var current = NothingPicked;
        for (var i = 0; i < locales.Count; i++)
        {
            if (locales[i].Locale == appState.Locale)
                current = i;
        }
        var picked = PickFrom(ui, "language", LocaleLabels, current);
        if (picked != NothingPicked)
            SetLocale(appState, locales[picked].Locale);
    }

    private static string[] ListLocaleLabels()
    {
        return Locales.All
            .Select(l => l.Endonym)
            .ToArray();
    }

    /// <summary>A dropdown as wide as the widest of <paramref name="labels"/>, showing the one at
    /// <paramref name="current"/>, or the first when it is none of them; returns the index picked on
    /// this frame, or <see cref="NothingPicked"/>. Its items are told apart by <paramref name="itemIds"/>
    /// when labels can repeat, else by their labels; the open menu closes when <paramref name="revision"/>
    /// changes, as when the items change under it.</summary>
    private static int PickFrom(Ui<UiState> ui, string id, string[] labels, int current, IReadOnlyList<string>? itemIds = null,
        int revision = 0)
    {
        var width = ui.SelectButtonWidth(labels);
        var shown = Math.Max(current, 0);
        using var menu = ui.SelectButton(id, labels[shown], width, revision: revision);
        if (!menu.IsOpen)
            return NothingPicked;
        var picked = NothingPicked;
        for (var i = 0; i < labels.Length; i++)
        {
            var itemId = ItemIdAt(itemIds, i);
            if (ui.MenuRadioItem(labels[i], i == current, id: itemId))
                picked = i;
        }
        return picked;
    }

    private static string? ItemIdAt(IReadOnlyList<string>? itemIds, int index)
    {
        if (itemIds == null)
            return null;
        return itemIds[index];
    }

    private static void CodeSection(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var display = ui.State.Display;
        var defaults = DisplayOptions.Default;
        using var section = Section(ui, ui.Memo("code", t, static s => Heading(s.DisplayOptionsCode)));
        using var card = Card(ui);
        using (OptionRow(ui, t.DisplayOptionsFont, null, display.Font != null))
            FontPicker(ui);
        ui.Separator();

        var hint = ui.Memo("zoom-hint", t, static s => s.DisplayOptionsZoomHint(Shortcuts.ZoomReset));
        using (OptionRow(ui, t.DisplayOptionsFontSize, hint, display.FontSize != null))
            FontSizeStepper(ui);
        FontPreview(ui);
        ui.Separator();

        var lineNumbers = display.ShowLineNumbers;
        if (SwitchRow(ui, t.DisplayOptionsLineNumbers, ref lineNumbers, "line-numbers", lineNumbers != defaults.ShowLineNumbers))
            SetDisplayOptions(ui.State, display with { ShowLineNumbers = lineNumbers });
        ui.Separator();

        var currentLine = display.HighlightCurrentLine;
        if (SwitchRow(ui, t.DisplayOptionsCurrentLine, ref currentLine, "current-line", currentLine != defaults.HighlightCurrentLine))
            SetDisplayOptions(ui.State, display with { HighlightCurrentLine = currentLine });
        ui.Separator();

        var brackets = display.HighlightMatchingBrackets;
        if (SwitchRow(ui, t.DisplayOptionsMatchingBrackets, ref brackets, "matching-brackets", brackets != defaults.HighlightMatchingBrackets))
            SetDisplayOptions(ui.State, display with { HighlightMatchingBrackets = brackets });
    }

    /// <summary>The bundled font and the installed monospaced ones, the chosen one shown by name even
    /// before the installed ones are listed.</summary>
    private static void FontPicker(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var choices = ui.State.CodeFonts;
        var chosen = ui.State.Display.Font;
        var shown = CodeFontChoices.NameOf(chosen);
        using var menu = ui.SelectButton("code-font", shown, MeasureFontPicker(ui, shown));
        if (!menu.IsOpen)
            return;
        var fonts = choices.Fonts;
        for (var i = 0; i < fonts.Count; i++)
        {
            if (ui.MenuRadioItem(choices.Labels[i], choices.Labels[i] == shown))
                SetCodeFont(ui.State, fonts[i]);
        }
        if (!choices.IsListed)
            ui.MenuItem(t.DisplayOptionsFontsLoading, enabled: false, id: "fonts-loading");
    }

    /// <summary>As wide as the longest name offered or <paramref name="shown"/>, up to a limit past which
    /// names are shortened.</summary>
    private static float MeasureFontPicker(Ui<UiState> ui, string shown)
    {
        var offered = ui.SelectButtonWidth(ui.State.CodeFonts.Labels);
        var chosen = ui.SelectButtonWidth([shown]);
        var widest = MathF.Max(offered, chosen);
        return MathF.Min(widest, FontPickerMaxWidth);
    }

    private static void FontSizeStepper(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var display = ui.State.Display;
        var smaller = ui.Memo("smaller", t, static s => s.DisplayOptionsSmaller(Shortcuts.ZoomOut));
        if (ui.IconButton("smaller", ui.State.Icons.OptionsFontSmaller, smaller, display.CanZoomOut(theme), ButtonVariant.Neutral))
            ZoomCode(ui.State, -1);
        var size = display.FontSizeIn(theme);
        var text = ui.Memo("size", size, static s => FormatFontSize(s));
        ui.Text(text, new Box { Width = Sizing.Fixed(MeasureFontSizeSlot(ui)) }, align: Align.Center);
        var larger = ui.Memo("larger", t, static s => s.DisplayOptionsLarger(Shortcuts.ZoomIn));
        if (ui.IconButton("larger", ui.State.Icons.OptionsFontLarger, larger, display.CanZoomIn(theme), ButtonVariant.Neutral))
            ZoomCode(ui.State, 1);
    }

    /// <summary>A line of code text in the chosen font and size on the documents' background, under the
    /// font size, so every change to either shows as it is made.</summary>
    private static void FontPreview(Ui<UiState> ui)
    {
        var theme = ui.Theme;
        var size = ui.State.Display.FontSizeIn(theme);
        using var row = ui.Column(new Box
        {
            Width = Sizing.Grow(),
            Padding = new Spacing(theme.PanelPadding, 0f, theme.PanelPadding, theme.Gap),
        });
        using var sample = ui.Column(new Box
        {
            Width = Sizing.Grow(),
            Padding = new Spacing(theme.ControlPaddingX, theme.Gap),
            Background = theme.EditorBackground,
            BorderColor = theme.Separator,
            BorderWidth = theme.BorderWidth,
            Radius = theme.CornerRadius,
        });
        var color = ui.State.CodeViewPalette.Plain;
        ui.Text(PreviewText, new Box { Width = Sizing.Grow() }, color: color, size: size, family: MonoFonts.Code, wrap: true);
    }

    private static string FormatFontSize(float size) => size.ToString(FontSizeFormat, CultureInfo.CurrentCulture);

    /// <summary>As wide as the largest size, so the buttons beside it stay put as it changes.</summary>
    private static float MeasureFontSizeSlot(Ui<UiState> ui)
    {
        var widest = FormatFontSize(DisplayOptions.MaxFontSize);
        var width = ui.MeasureText(widest);
        return MathF.Ceiling(width) + ui.Theme.Gap * 2;
    }

    private static void TreeSection(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        using var section = Section(ui, ui.Memo("tree", t, static s => Heading(s.DisplayOptionsTree)));
        using var card = Card(ui);
        var changed = ui.State.Display.TreeTokens != DisplayOptions.Default.TreeTokens;
        using (OptionRow(ui, t.DisplayOptionsTokens, t.DisplayOptionsTokensDescription, changed))
            TokenFormatPicker(ui);
    }

    private static void TokenFormatPicker(Ui<UiState> ui)
    {
        var labels = TokenFormatLabels.GetValue(ui.State.Strings, ListTokenFormatLabels);
        var current = Array.IndexOf(TokenFormats, ui.State.Display.TreeTokens);
        var picked = PickFrom(ui, "tokens", labels, current);
        if (picked != NothingPicked)
            SetTreeTokens(ui.State, TokenFormats[picked]);
    }

    private static string[] ListTokenFormatLabels(Strings t)
    {
        return TokenFormats
            .Select(f => TokenFormatLabel(t, f))
            .ToArray();
    }

    private static string TokenFormatLabel(Strings t, TokenFormat format) => format switch
    {
        TokenFormat.Hex => t.DisplayOptionsTokensHex,
        TokenFormat.Decimal => t.DisplayOptionsTokensDecimal,
        _ => t.DisplayOptionsTokensHidden,
    };

    /// <summary>A row whose switch flips when it or the rest of the row is clicked.</summary>
    private static bool SwitchRow(Ui<UiState> ui, string title, ref bool value, string id, bool changed)
    {
        var press = ui.Interact(id + RowSuffix);
        if (press.Hovered)
            ui.SetCursor(MouseCursor.Hand);
        bool flipped;
        using (OptionRow(ui, press.Id, title, null, changed))
            flipped = ui.Switch(id, ref value);
        if (flipped)
            return true;
        if (!press.Clicked)
            return false;
        value = !value;
        return true;
    }

    /// <summary>The link-quiet button that puts every option back, at the end of the form.</summary>
    private static void ResetRow(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var changed = ui.State.Display != DisplayOptions.Default;
        var icon = new UiIcon(ui.State.Icons.OptionsResetDisplay);
        using (ui.Row(new Box { Width = Sizing.Grow(), AlignX = Align.End }))
        {
            if (ui.Button(t.DisplayOptionsReset, enabled: changed, id: "reset", variant: ButtonVariant.Ghost, icon: icon))
                AskToResetDisplayOptions(ui.State);
        }
    }

    private static string Heading(string text) => text.ToUpperInvariant();

    /// <summary>A section of the form: its heading in small muted capitals over what the caller adds.</summary>
    private static Ui.Scope Section(Ui<UiState> ui, string heading)
    {
        var theme = ui.Theme;
        var section = ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.GapTight });
        ui.Text(heading, color: theme.TextMuted, size: theme.FontSizeSmall, bold: true);
        return section;
    }

    /// <summary>A raised card holding a section's option rows, which the caller divides with separators.</summary>
    private static Ui.Scope Card(Ui<UiState> ui)
    {
        var theme = ui.Theme;
        return ui.Column(new Box
        {
            Width = Sizing.Grow(),
            Gap = 0f,
            Background = theme.Surface,
            BorderColor = theme.Separator,
            BorderWidth = theme.BorderWidth,
            Radius = theme.PanelRadius,
        });
    }

    /// <summary>A row of a card: <paramref name="title"/>, bold when changed, over its muted
    /// <paramref name="description"/> when it has one, then the control the caller adds at the right.</summary>
    private static Ui.Scope OptionRow(Ui<UiState> ui, string title, string? description, bool changed) =>
        OptionRow(ui, 0, title, description, changed);

    /// <summary>A row as <see cref="OptionRow(Ui{UiState}, string, string?, bool)"/>, which <paramref name="id"/>
    /// takes the pointer for when it is not 0.</summary>
    private static Ui.Scope OptionRow(Ui<UiState> ui, int id, string title, string? description, bool changed)
    {
        var theme = ui.Theme;
        var row = ui.Row(id, new Box
        {
            Width = Sizing.Grow(),
            Height = Sizing.Fit(min: theme.ControlHeight + theme.Gap * 2),
            Gap = theme.GroupGap,
            Padding = new Spacing(theme.PanelPadding, theme.Gap),
            AlignY = Align.Center,
        });
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.GapTight }))
        {
            ui.Text(title, new Box { Width = Sizing.Grow() }, bold: changed, overflow: TextOverflow.Ellipsis);
            if (description != null)
                ui.Text(description, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, size: theme.FontSizeSmall, wrap: true);
        }
        return row;
    }
}

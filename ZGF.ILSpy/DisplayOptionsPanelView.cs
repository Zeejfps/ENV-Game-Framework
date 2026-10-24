using System.Globalization;
using System.Runtime.CompilerServices;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.CodeDisplay;

namespace ZGF.ILSpy;

/// <summary>How the app is drawn, as settings cards: the interface's scale, then the code's font size
/// and switches for line numbers, the current line's highlight and matching brackets. Each option is a
/// row of its card, named on the left, with its control at the right. Every change shows at once; an
/// option changed from its default has a bold name.</summary>
internal static class DisplayOptionsPanelView
{
    private const string FontSizeFormat = "0.#";
    private const float FormMaxWidth = 560f;
    private const string RowSuffix = "-row";

    private static readonly ConditionalWeakTable<Strings, string[]> UiScaleLabels = new();

    public static void Render(Ui<UiState> ui)
    {
        var theme = ui.Theme;
        using var pane = ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f });
        using var form = ui.Column(new Box
        {
            Width = Sizing.Grow(max: FormMaxWidth),
            Gap = theme.GroupGap,
            Padding = theme.PanelPadding,
        });
        InterfaceSection(ui);
        CodeSection(ui);
        ResetRow(ui);
    }

    private static void InterfaceSection(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        using var section = Section(ui, ui.Memo("interface", t, static s => Heading(s.DisplayOptionsInterface)));
        using var card = Card(ui);
        var changed = ui.State.Display.UiScale != DisplayOptions.Default.UiScale;
        using (OptionRow(ui, t.DisplayOptionsUiScale, t.DisplayOptionsUiScaleDescription, changed))
            UiScalePicker(ui);
    }

    private static void UiScalePicker(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var current = ui.State.Display.UiScale;
        var labels = UiScaleLabels.GetValue(t, ListUiScaleLabels);
        var width = ui.SelectButtonWidth(labels);
        using var menu = ui.SelectButton("ui-scale", UiScaleLabel(t, current), width);
        if (!menu.IsOpen)
            return;
        var scales = DisplayOptions.UiScales;
        for (var i = 0; i < scales.Count; i++)
        {
            if (ui.MenuRadioItem(labels[i], scales[i] == current))
                SetUiScale(ui.State, scales[i]);
        }
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

    private static void CodeSection(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var display = ui.State.Display;
        var defaults = DisplayOptions.Default;
        using var section = Section(ui, ui.Memo("code", t, static s => Heading(s.DisplayOptionsCode)));
        using var card = Card(ui);
        var hint = ui.Memo("zoom-hint", t, static s => s.DisplayOptionsZoomHint(Shortcuts.ZoomReset));
        using (OptionRow(ui, t.DisplayOptionsFontSize, hint, display.FontSize != null))
            FontSizeStepper(ui);
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

    private static void FontSizeStepper(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var display = ui.State.Display;
        var smaller = ui.Memo("smaller", t, static s => s.DisplayOptionsSmaller(Shortcuts.ZoomOut));
        if (ui.IconButton("smaller", LucideIcons.Minus, LucideIcons.Family, smaller, display.CanZoomOut(theme), ButtonVariant.Neutral))
            ZoomCode(ui.State, -1);
        var size = display.FontSizeIn(theme);
        var text = ui.Memo("size", size, static s => FormatFontSize(s));
        ui.Text(text, new Box { Width = Sizing.Fixed(MeasureFontSizeSlot(ui)) }, align: Align.Center);
        var larger = ui.Memo("larger", t, static s => s.DisplayOptionsLarger(Shortcuts.ZoomIn));
        if (ui.IconButton("larger", LucideIcons.Plus, LucideIcons.Family, larger, display.CanZoomIn(theme), ButtonVariant.Neutral))
            ZoomCode(ui.State, 1);
    }

    private static string FormatFontSize(float size) => size.ToString(FontSizeFormat, CultureInfo.CurrentCulture);

    /// <summary>As wide as the largest size, so the buttons beside it stay put as it changes.</summary>
    private static float MeasureFontSizeSlot(Ui<UiState> ui)
    {
        var widest = FormatFontSize(DisplayOptions.MaxFontSize);
        var width = ui.MeasureText(widest);
        return MathF.Ceiling(width) + ui.Theme.Gap * 2;
    }

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
        var icon = new UiIcon(LucideIcons.RotateCcw, LucideIcons.Family);
        using (ui.Row(new Box { Width = Sizing.Grow(), AlignX = Align.End }))
        {
            if (ui.Button(t.DisplayOptionsReset, enabled: changed, id: "reset", variant: ButtonVariant.Ghost, icon: icon))
                ResetDisplayOptions(ui.State);
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

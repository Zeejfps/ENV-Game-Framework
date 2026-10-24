using System.Globalization;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using static ZGF.ILSpy.CodeDisplay;

namespace ZGF.ILSpy;

/// <summary>How the documents draw code: the font size, and toggles for line numbers, the current
/// line's highlight and matching brackets. Every change shows in the open documents at once; a
/// choice changed from its default is bold.</summary>
internal static class DisplayOptionsPanelView
{
    private const string FontSizeFormat = "0.#";

    public static void Render(Ui<UiState> ui)
    {
        var theme = ui.Theme;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui);
        ui.Separator();
        using var body = ui.Column(new Box
        {
            Width = Sizing.Grow(),
            Gap = theme.GapTight,
            Padding = new Spacing(theme.ShellPaddingX, theme.ToolbarPaddingY),
        });
        FontSize(ui);
        Toggles(ui);
        ZoomHint(ui);
    }

    private static void Toolbar(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var changed = ui.State.Display != DisplayOptions.Default;
        using (ui.PaneToolbar())
        {
            if (ui.IconButton("reset", LucideIcons.RotateCcw, LucideIcons.Family, t.DisplayOptionsReset, changed))
                ResetDisplayOptions(ui.State);
        }
    }

    private static void FontSize(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var display = ui.State.Display;
        using (ui.Row(new Box { Width = Sizing.Grow(), Height = theme.RowHeight, Gap = theme.GapTight, AlignY = Align.Center }))
        {
            ui.Text(t.DisplayOptionsFontSize, new Box { Width = Sizing.Grow() }, bold: display.FontSize != null);
            var smaller = ui.Memo("smaller", t, static s => s.DisplayOptionsSmaller(Shortcuts.ZoomOut));
            if (ui.IconButton("smaller", LucideIcons.Minus, LucideIcons.Family, smaller, display.CanZoomOut(theme)))
                ZoomCode(ui.State, -1);
            var size = display.FontSizeIn(theme);
            ui.Text(ui.Memo("size", size, static s => s.ToString(FontSizeFormat, CultureInfo.CurrentCulture)));
            var larger = ui.Memo("larger", t, static s => s.DisplayOptionsLarger(Shortcuts.ZoomIn));
            if (ui.IconButton("larger", LucideIcons.Plus, LucideIcons.Family, larger, display.CanZoomIn(theme)))
                ZoomCode(ui.State, 1);
        }
    }

    private static void Toggles(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var display = ui.State.Display;
        var defaults = DisplayOptions.Default;

        var lineNumbers = display.ShowLineNumbers;
        if (Toggle(ui, t.DisplayOptionsLineNumbers, ref lineNumbers, "line-numbers", lineNumbers != defaults.ShowLineNumbers))
            SetDisplayOptions(ui.State, display with { ShowLineNumbers = lineNumbers });

        var currentLine = display.HighlightCurrentLine;
        if (Toggle(ui, t.DisplayOptionsCurrentLine, ref currentLine, "current-line", currentLine != defaults.HighlightCurrentLine))
            SetDisplayOptions(ui.State, display with { HighlightCurrentLine = currentLine });

        var brackets = display.HighlightMatchingBrackets;
        if (Toggle(ui, t.DisplayOptionsMatchingBrackets, ref brackets, "matching-brackets", brackets != defaults.HighlightMatchingBrackets))
            SetDisplayOptions(ui.State, display with { HighlightMatchingBrackets = brackets });
    }

    private static bool Toggle(Ui<UiState> ui, string label, ref bool value, string id, bool changed)
    {
        return ui.Checkbox(label, ref value, id: id, width: Sizing.Grow(), bold: changed, compact: true);
    }

    private static void ZoomHint(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var hint = ui.Memo("zoom-hint", t, static s => s.DisplayOptionsZoomHint(Shortcuts.ZoomReset));
        ui.Text(hint, new Box { Width = Sizing.Grow() }, color: theme.TextMuted, wrap: true);
    }
}

using ZGF.Desktop;
using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.CodeDisplay;
using static ZGF.ILSpy.ScreenLayout;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy.App.Tests;

public sealed class DisplayOptionsTests
{
    private static CodeViewDriver Showing(DecompiledDocument document, DisplayOptions? display, int frames = 2)
    {
        var view = new CodeViewDriver(document) { Display = display };
        view.Run(frames);
        return view;
    }

    private static List<UiRect> FilledWith(CodeViewDriver view, uint color)
    {
        return view.Frames.Canvas.Rects
            .Where(r => r.Inputs.Style.BackgroundColor == color)
            .Select(r => view.Frames.ToUi(r.Inputs.Position))
            .ToList();
    }

    [Fact]
    public void Hidden_line_numbers_are_not_drawn_and_the_text_moves_left()
    {
        var numbered = Showing(CodeViewDriver.Numbered(5), DisplayOptions.Default);
        var hidden = Showing(CodeViewDriver.Numbered(5), new DisplayOptions(ShowLineNumbers: false));

        Assert.Contains(numbered.Frames.Canvas.GlyphRuns, r => r.Text == "1");
        Assert.DoesNotContain(hidden.Frames.Canvas.GlyphRuns, r => r.Text == "1");
        Assert.Contains(("line 0", CodeViewDriver.Origin), hidden.DrawnText());
        Assert.True(hidden.TextLeft < numbered.TextLeft);
    }

    [Fact]
    public void Code_is_drawn_at_the_chosen_font_size_in_taller_rows()
    {
        var view = Showing(CodeViewDriver.Numbered(5), new DisplayOptions(FontSize: 24f));

        var run = view.Frames.Canvas.GlyphRuns.First(r => r.Text == "line 0");
        Assert.Equal(24f, run.Style.FontSize.Value);
        Assert.True(view.State.RowHeight > CodeViewDriver.Row);
    }

    [Fact]
    public void Ctrl_with_equals_minus_or_0_in_a_focused_view_asks_to_change_the_size()
    {
        var view = Showing(CodeViewDriver.Numbered(5), DisplayOptions.Default);
        view.Click(0, 0);

        view.Key(KeyboardKey.Equals, KeyModifiers.Control);
        Assert.Equal(CodeZoom.In, view.Last.Zoom);
        view.Key(KeyboardKey.Minus, KeyModifiers.Control);
        Assert.Equal(CodeZoom.Out, view.Last.Zoom);
        view.Key(KeyboardKey.Alpha0, KeyModifiers.Control);
        Assert.Equal(CodeZoom.Reset, view.Last.Zoom);
    }

    [Fact]
    public void A_view_without_display_options_never_asks_to_change_the_size()
    {
        var view = Showing(CodeViewDriver.Numbered(5), null);
        view.Click(0, 0);

        view.Key(KeyboardKey.Equals, KeyModifiers.Control);

        Assert.Equal(CodeZoom.None, view.Last.Zoom);
    }

    [Fact]
    public void Ctrl_with_the_wheel_asks_to_change_the_size_instead_of_scrolling()
    {
        var view = Showing(CodeViewDriver.Numbered(200), DisplayOptions.Default);
        view.MoveTo(2, 0);

        view.Frames.Key(KeyboardKey.LeftControl, KeyModifiers.Control).Wheel(-1).Frame();
        Assert.Equal(CodeZoom.Out, view.Last.Zoom);
        Assert.Equal(0f, view.State.ScrollY);

        view.Frames.Key(KeyboardKey.F12).Wheel(-1).Frame();
        Assert.Equal(CodeZoom.None, view.Last.Zoom);
        Assert.True(view.State.ScrollY > 0f);
    }

    [Fact]
    public void The_caret_line_is_highlighted_when_asked()
    {
        var view = Showing(CodeViewDriver.Numbered(5), new DisplayOptions(HighlightCurrentLine: true));

        view.Click(2, 1);
        view.Run();

        var row = Assert.Single(FilledWith(view, CodeViewPalette.Dark.CurrentLine));
        Assert.Equal(CodeViewDriver.Origin + 2 * CodeViewDriver.Row, row.Y);
    }

    [Fact]
    public void The_bracket_at_the_caret_and_its_match_are_highlighted()
    {
        var view = Showing(DecompiledDocument.FromText("t", "f(a[0])"), DisplayOptions.Default);

        view.Click(0, 1);
        view.Run();

        var brackets = FilledWith(view, CodeViewPalette.Dark.MatchingBracket);
        Assert.Equal(2, brackets.Count);
        Assert.Equal(view.TextLeft + 1 * CodeViewDriver.Cell, brackets.Min(b => b.X));
        Assert.Equal(view.TextLeft + 6 * CodeViewDriver.Cell, brackets.Max(b => b.X));
    }

    [Fact]
    public void Zooming_steps_the_font_size_from_the_themes_and_resetting_returns_to_it()
    {
        var ui = Screens.Blank();
        var size = ui.Theme.MonoFontSize;

        ZoomCode(ui, 2);
        Assert.Equal(size + 2f, ui.Display.FontSize);
        ZoomCode(ui, -100);
        Assert.Equal(DisplayOptions.MinFontSize, ui.Display.FontSize);
        ResetCodeZoom(ui);
        Assert.Null(ui.Display.FontSize);
    }

    [Fact]
    public void A_saved_font_size_out_of_range_is_brought_within_it()
    {
        var restored = DisplayOptions.Restore(new DisplayOptions(FontSize: 100f));

        Assert.Equal(DisplayOptions.MaxFontSize, restored.FontSize);
        Assert.Same(DisplayOptions.Default, DisplayOptions.Restore(null));
    }

    [Fact]
    public void The_display_options_are_saved_and_restored()
    {
        using var temp = new TempDir();
        var app = new AppDriver(settings: temp.Settings());
        var options = new DisplayOptions(FontSize: 20f, ShowLineNumbers: false, HighlightCurrentLine: true);

        SetDisplayOptions(app.Ui, options);
        app.Run();
        SaveSettings(app.AppState);

        var restored = new AppStateDriver(temp.Settings());
        Assert.Equal(options, restored.Ui.Display);
    }

    [Fact]
    public void The_display_options_open_beside_the_decompiler_options()
    {
        var app = AppDriver.Opened(width: 1600f);

        OpenDecompilerOptions(app.Ui);
        OpenDisplayOptions(app.Ui);
        app.Run();

        Assert.Same(app.Ui.Layout.GroupOf(DockPanels.DecompilerOptions), app.Ui.Layout.GroupOf(DockPanels.DisplayOptions));
        Assert.True(app.Frames.Drew("Show line numbers"));
    }

    [Fact]
    public void The_panel_changes_the_size_and_the_toggles_and_resets_them()
    {
        var app = AppDriver.Opened(width: 1600f);
        OpenDisplayOptions(app.Ui);
        app.Run();
        var size = app.Ui.Theme.MonoFontSize;

        app.ClickLast(LucideIcons.Plus).Frame();
        app.Click("Show line numbers").Frame();
        app.Click("Highlight the current line").Frame();

        var changed = new DisplayOptions(FontSize: size + 1f, ShowLineNumbers: false, HighlightCurrentLine: true);
        Assert.Equal(changed, app.Ui.Display);
        Assert.True(IsBold(app, "Show line numbers"));
        Assert.False(IsBold(app, "Highlight matching brackets"));

        app.ClickLast(LucideIcons.RotateCcw).Frame();
        Assert.Equal(DisplayOptions.Default, app.Ui.Display);
    }

    [Fact]
    public void Ctrl_equals_in_a_document_makes_its_code_larger()
    {
        var app = AppDriver.Opened();
        app.Show(app.Fixture("Greeter"));
        app.Settle();
        app.Run(3);
        var size = app.Ui.Theme.MonoFontSize;

        var code = app.Frames.Canvas.GlyphRuns.First(r => r.Text == "Greeter");
        app.Frames.Click(code.Origin.X + 2f, app.Frames.Height - code.Origin.Y + 2f).Frame();
        app.Frames.Key(KeyboardKey.Equals, KeyModifiers.Control).Frame();

        Assert.Equal(size + 1f, app.Ui.Display.FontSize);
    }

    private static bool IsBold(AppDriver app, string text) =>
        app.Frames.Canvas.Texts.Single(t => t.Inputs.Text == text).Inputs.Style.FontWeight.Value == FontWeight.Bold;
}

public sealed class MatchingBracketsTests
{
    private static BracketPair? Find(DecompiledDocument document, int line, int column) =>
        new MatchingBrackets().Find(document, new TextPosition(line, column));

    private static BracketPair Pair(int openLine, int openColumn, int closeLine, int closeColumn) =>
        new(new TextPosition(openLine, openColumn), new TextPosition(closeLine, closeColumn));

    [Theory]
    [InlineData(1, 1, 6)]
    [InlineData(7, 1, 6)]
    [InlineData(3, 3, 5)]
    [InlineData(4, 3, 5)]
    [InlineData(6, 1, 6)]
    public void The_bracket_at_or_just_before_the_caret_is_matched(int caret, int open, int close)
    {
        var document = DecompiledDocument.FromText("t", "f(a[0])");

        Assert.Equal(Pair(0, open, 0, close), Find(document, 0, caret));
    }

    [Fact]
    public void Brackets_match_across_lines_past_nested_ones()
    {
        var document = DecompiledDocument.FromText("t", "{\n  { x; }\n}");

        Assert.Equal(Pair(0, 0, 2, 0), Find(document, 0, 0));
        Assert.Equal(Pair(0, 0, 2, 0), Find(document, 2, 1));
    }

    [Fact]
    public void Brackets_in_strings_do_not_count()
    {
        var builder = new DocumentBuilder(DecompilationLanguage.CSharp);
        builder.Write("if (", SpanKind.Plain);
        builder.Write("\")\"", SpanKind.String);
        builder.Write(")", SpanKind.Plain);
        var document = builder.Build("t");

        Assert.Equal(Pair(0, 3, 0, 7), Find(document, 0, 3));
        Assert.Null(Find(document, 0, 5));
    }

    [Fact]
    public void A_bracket_without_a_match_and_text_without_brackets_find_nothing()
    {
        var document = DecompiledDocument.FromText("t", "f(a b");

        Assert.Null(Find(document, 0, 1));
        Assert.Null(Find(document, 0, 4));
    }
}

using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.CodeDisplay;
using static ZGF.ILSpy.ScreenLayout;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.App.Tests.BuiltInGlyphs;

namespace ZGF.ILSpy.App.Tests;

public sealed class DisplayOptionsTests
{
    private const float TallWindow = 1100f;

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

        view.Key(Shortcuts.ZoomIn);
        Assert.Equal(CodeZoom.In, view.Last.Zoom);
        view.Key(Shortcuts.ZoomOut);
        Assert.Equal(CodeZoom.Out, view.Last.Zoom);
        view.Key(Shortcuts.ZoomReset);
        Assert.Equal(CodeZoom.Reset, view.Last.Zoom);
    }

    [Fact]
    public void A_view_without_display_options_never_asks_to_change_the_size()
    {
        var view = Showing(CodeViewDriver.Numbered(5), null);
        view.Click(0, 0);

        view.Key(Shortcuts.ZoomIn);

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

        var row = Assert.Single(FilledWith(view, BuiltInThemes.Dark.Code.CurrentLine));
        Assert.Equal(CodeViewDriver.Origin + 2 * CodeViewDriver.Row, row.Y);
    }

    [Fact]
    public void The_bracket_at_the_caret_and_its_match_are_highlighted()
    {
        var view = Showing(DecompiledDocument.FromText("t", "f(a[0])"), DisplayOptions.Default);

        view.Click(0, 1);
        view.Run();

        var brackets = FilledWith(view, BuiltInThemes.Dark.Code.MatchingBracket);
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
    public void The_ui_scale_starts_at_100_percent_and_takes_the_nearest_offered_scale()
    {
        var display = DisplayOptions.Default;

        Assert.Equal(1f, display.UiScale);
        Assert.Equal([0.75f, 0.8f, 0.9f, 1f, 1.1f, 1.25f, 1.5f, 1.75f, 2f], DisplayOptions.UiScales);
        Assert.Equal(1.25f, display.WithUiScale(1.2f).UiScale);
        Assert.Equal(2f, display.WithUiScale(5f).UiScale);
        Assert.Equal(0.75f, display.WithUiScale(0.1f).UiScale);
    }

    [Fact]
    public void A_saved_ui_scale_becomes_the_nearest_offered()
    {
        var restored = DisplayOptions.Restore(new DisplayOptions(UiScale: 1.3f));

        Assert.Equal(1.25f, restored.UiScale);
    }

    [Fact]
    public void The_panel_scales_the_whole_ui_through_the_window()
    {
        var app = AppDriver.Opened(width: 1600f);
        OpenDisplayOptions(app.Ui);
        app.Run();
        Assert.True(app.Frames.Drew("100%"));

        app.Click("100%").Frame();
        app.Click("125%").Frame();

        Assert.Equal(1.25f, app.Ui.Display.UiScale);
        Assert.True(app.Frames.Drew("125%"));
        Assert.Equal(1.25f, app.Frames.Frame(ui => ui.Scale));
        Assert.True(IsBold(app, "UI scale"));
    }

    [Fact]
    public void The_display_options_are_saved_and_restored()
    {
        using var temp = new TempDir();
        var app = new AppDriver(settings: temp.Settings());
        var font = new CodeFont("Test Mono", temp.Write("mono.ttf", FontBytes), 0);
        var options = new DisplayOptions(FontSize: 20f, ShowLineNumbers: false, HighlightCurrentLine: true, Font: font, TreeTokens: TokenFormat.Decimal);

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
        Assert.True(app.Frames.Drew("Line numbers"));
    }

    [Fact]
    public void The_panel_changes_the_size_and_the_toggles_and_resets_them()
    {
        var app = AppDriver.Opened(width: 1600f, height: TallWindow);
        OpenDisplayOptions(app.Ui);
        app.Run();
        var size = app.Ui.Theme.MonoFontSize;

        app.ClickLast(Icons.OptionsFontLarger).Frame();
        app.Click("Line numbers").Frame();
        app.Click("Highlight current line").Frame();

        var changed = new DisplayOptions(FontSize: size + 1f, ShowLineNumbers: false, HighlightCurrentLine: true);
        Assert.Equal(changed, app.Ui.Display);
        Assert.True(IsBold(app, "Line numbers"));
        Assert.False(IsBold(app, "UI scale"));

        app.Click("Reset to defaults").Frame(2);
        Assert.True(app.Ui.ConfirmingDisplayReset);
        Assert.Equal(changed, app.Ui.Display);
        app.Click("Reset").Frame(2);

        Assert.False(app.Ui.ConfirmingDisplayReset);
        Assert.Equal(DisplayOptions.Default, app.Ui.Display);
    }

    [Fact]
    public void The_panel_picks_how_the_tree_writes_metadata_tokens()
    {
        var app = AppDriver.Opened(width: 1600f, height: TallWindow);
        OpenDisplayOptions(app.Ui);
        app.Run();
        Assert.False(IsBold(app, "Metadata tokens"));

        app.Click("Hidden").Frame();
        app.Click("Hexadecimal").Frame();

        Assert.Equal(TokenFormat.Hex, app.Ui.Display.TreeTokens);
        Assert.True(IsBold(app, "Metadata tokens"));
    }

    [Fact]
    public void The_tree_lists_the_empty_metadata_tables_once_the_panel_switches_them_on()
    {
        var app = AppDriver.Opened(width: 1600f, height: TallWindow);
        var tables = app.Node("Metadata", "Tables");
        var empty = tables.Table("FieldPtr");
        OpenDisplayOptions(app.Ui);
        app.Run();
        Assert.DoesNotContain(empty, app.Ui.Tree.GetChildren(tables));

        app.Click("Show empty metadata tables").Frame(2);

        Assert.True(app.Ui.Display.ShowEmptyMetadataTables);
        Assert.True(IsBold(app, "Show empty metadata tables"));
        Assert.Contains(empty, app.Ui.Tree.GetChildren(tables));
    }

    [Fact]
    public void The_tree_nests_namespaces_once_the_panel_switches_it_on()
    {
        var app = AppDriver.Opened(width: 1600f, height: TallWindow);
        var root = app.Node();
        var outermost = app.Node("ZGF");
        var fixtures = app.Node(AppStateDriver.FixturesNamespace);
        OpenDisplayOptions(app.Ui);
        app.Run();
        Assert.Contains(fixtures, app.Ui.Tree.GetChildren(root));
        Assert.DoesNotContain(outermost, app.Ui.Tree.GetChildren(root));

        app.Click("Nested namespaces").Frame(2);

        Assert.True(app.Ui.Display.NestedNamespaces);
        Assert.True(IsBold(app, "Nested namespaces"));
        Assert.Contains(outermost, app.Ui.Tree.GetChildren(root));
        Assert.DoesNotContain(fixtures, app.Ui.Tree.GetChildren(root));
        Assert.Equal("Fixtures", app.Ui.Tree.RowLabel(fixtures));
    }

    [Fact]
    public void Switching_preview_tabs_off_in_the_panel_keeps_the_preview_tab_open_and_shows_picks_in_the_active_tab()
    {
        var app = AppDriver.Opened(width: 1600f, height: TallWindow);
        var shown = app.AppState.Tabs.Preview;
        OpenDisplayOptions(app.Ui);
        app.Run();

        app.Click("Preview tabs").Frame(2);

        Assert.NotNull(shown);
        Assert.False(app.Ui.Display.PreviewTabs);
        Assert.False(app.AppState.PreviewTabs);
        Assert.Null(app.AppState.Tabs.Preview);
        Assert.True(IsBold(app, "Preview tabs"));
    }

    [Fact]
    public void The_theme_is_picked_from_the_panel_and_draws_the_window()
    {
        var app = AppDriver.Opened(width: 1600f);
        OpenDisplayOptions(app.Ui);
        app.Run();
        Assert.Equal("dark", app.AppState.Theme);

        app.Click("Dark").Frame();
        app.Click("Dracula").Frame();

        Assert.Equal("dracula", app.AppState.Theme);
        Assert.Equal(app.AppState.Themes.Find("dracula").Ui.Background, app.Frames.WindowBackground);
    }

    [Fact]
    public void The_users_themes_are_listed_after_the_built_in_ones()
    {
        using var temp = new TempDir();
        temp.Write("themes/midnight.json", """{ "name": "Midnight", "extends": "dark", "ui": { "background": "#000010" } }""");
        var app = new AppDriver(width: 1600f, settings: temp.Settings());
        OpenDisplayOptions(app.Ui);
        app.Run();

        app.Click("Dark").Frame();
        app.Click("Midnight").Frame();

        Assert.Equal("midnight", app.AppState.Theme);
        Assert.Equal(0xFF000010u, app.Frames.WindowBackground);
    }

    [Fact]
    public void Themes_of_the_same_name_are_each_picked_as_themselves()
    {
        using var temp = new TempDir();
        temp.Write("themes/twin-a.json", """{ "name": "Twin", "extends": "dark", "ui": { "background": "#00000A" } }""");
        temp.Write("themes/twin-b.json", """{ "name": "Twin", "extends": "dark", "ui": { "background": "#00000B" } }""");
        var app = new AppDriver(width: 1600f, settings: temp.Settings());
        OpenDisplayOptions(app.Ui);
        app.Run();

        app.Click("Dark").Frame();
        app.Frames.Click(TwinRects(app)[1]).Frame();
        Assert.Equal("twin-b", app.AppState.Theme);

        app.Click("Twin").Frame();
        app.Frames.Click(TwinRects(app)[1]).Frame();
        Assert.Equal("twin-a", app.AppState.Theme);
        Assert.Equal(0xFF00000Au, app.Frames.WindowBackground);
    }

    [Fact]
    public void The_open_theme_menu_closes_when_the_themes_are_read_again()
    {
        var app = AppDriver.Opened(width: 1600f);
        OpenDisplayOptions(app.Ui);
        app.Run();
        app.Click("Dark").Frame();
        Assert.True(IsDrawn(app, "Dracula"));

        Theming.ReloadUserThemes(app.AppState);
        app.Run();

        Assert.False(IsDrawn(app, "Dracula"));
    }

    [Fact]
    public void Customizing_the_theme_switches_to_a_new_one_and_opens_its_file()
    {
        using var temp = new TempDir();
        var app = new AppDriver(width: 1600f, settings: temp.Settings());
        OpenDisplayOptions(app.Ui);
        app.Run();

        app.ClickLast(Icons.OptionsCustomizeTheme).Frame();

        Assert.Equal("dark-custom", app.AppState.Theme);
        Assert.Equal([temp.PathOf(Path.Combine("themes", "dark-custom.json"))], app.Frames.OpenedFiles);
    }

    [Fact]
    public void The_theme_row_ends_where_the_other_pickers_end_with_its_buttons_centred_on_the_picker()
    {
        using var temp = new TempDir();
        var app = new AppDriver(width: 3000f, settings: temp.Settings());
        OpenDisplayOptions(app.Ui);
        app.Run(2);

        var controls = FilledWith(app, app.Theme.Control);
        var language = ControlAround(controls, app.Frames.TextRect("English"));
        var picker = ControlAround(controls, app.Frames.TextRect("Dark"));
        var themeRow = controls
            .Where(r => Math.Abs(CentreY(r) - CentreY(picker)) < 0.5f)
            .ToList();

        Assert.Equal(4, themeRow.Count);
        Assert.Equal(language.X + language.Width, themeRow.Max(r => r.X + r.Width), 0.5f);
        Assert.All(themeRow, r => Assert.Equal(picker.Height, r.Height, 0.5f));
    }

    [Fact]
    public void In_a_narrow_pane_the_theme_pickers_give_up_room_before_their_titles_and_nothing_overflows_the_card()
    {
        using var temp = new TempDir();
        var app = NarrowThemeRows(temp, width: 1680f);

        AssertNothingOverflows(app);
        Assert.True(IsDrawn(app, "Theme"));
        Assert.True(IsDrawn(app, "Icon theme"));
        Assert.Contains(app.Frames.TextRects(), t => t.Text.StartsWith("Lu", StringComparison.Ordinal) && t.Text.EndsWith('…'));
    }

    [Fact]
    public void In_a_narrower_pane_a_title_is_cut_short_only_once_its_picker_can_give_up_no_more()
    {
        using var temp = new TempDir();
        var app = NarrowThemeRows(temp, width: 1600f);

        AssertNothingOverflows(app);
        Assert.True(IsDrawn(app, "Theme"));
        Assert.Contains(app.Frames.TextRects(), t => t.Text.StartsWith("Ico", StringComparison.Ordinal) && t.Text.EndsWith('…'));
    }

    [Fact]
    public void The_code_font_picker_never_grows_past_its_cap_for_a_long_font_name()
    {
        using var temp = new TempDir();
        var file = temp.Write("long.ttf", EmbeddedAssets.LoadBytes(typeof(IlSpyApp).Assembly, "JetBrainsMono-Regular.ttf"));
        var app = AppDriver.Opened(width: 3000f);
        OpenDisplayOptions(app.Ui);
        CodeDisplay.SetCodeFont(app.Ui, new CodeFont("A monospaced font with a name far too long to show whole", file, 0));
        app.Run(2);

        var controls = FilledWith(app, app.Theme.Control);
        var picker = controls.Single(r => app.Frames.TextRects().Any(t => t.Text.StartsWith("A monospaced", StringComparison.Ordinal) && Contains(r, t.Rect)));

        Assert.True(picker.Width <= 220f + 0.5f, $"{picker.Width}");
        Assert.Contains(app.Frames.TextRects(), t => t.Text.StartsWith("A monospaced", StringComparison.Ordinal) && t.Text.EndsWith('…'));
    }

    /// <summary>The display options of a window <paramref name="width"/> wide, with a user icon theme of
    /// a long name, so both theme pickers are sized for more than the pane leaves them.</summary>
    private static AppDriver NarrowThemeRows(TempDir temp, float width)
    {
        temp.Write("icon-themes/shapes.json", """{ "name": "Shapes with a long name" }""");
        var app = new AppDriver(width: width, settings: temp.Settings());
        OpenDisplayOptions(app.Ui);
        app.Run(2);
        return app;
    }

    /// <summary>Asserts that every control of the interface card ends inside it and that its six icon
    /// buttons keep their size.</summary>
    private static void AssertNothingOverflows(AppDriver app)
    {
        var theme = app.Theme;
        var card = InterfaceCard(app);
        var controls = FilledWith(app, theme.Control)
            .Where(r => r.Y > card.Y && r.Bottom < card.Bottom)
            .ToList();
        var buttons = controls
            .Where(r => Math.Abs(r.Width - theme.IconButtonSize) < 0.5f)
            .ToList();

        Assert.All(controls, r => Assert.True(r.Right <= card.Right - theme.PanelPadding + 0.5f, $"{r} overflows {card}"));
        Assert.Equal(6, buttons.Count);
        Assert.All(buttons, r => Assert.Equal(theme.IconButtonSize, r.Height, 0.5f));
    }

    /// <summary>The raised card of the interface section, around the color theme's picker.</summary>
    private static UiRect InterfaceCard(AppDriver app)
    {
        var picker = app.Frames.TextRect("Dark");
        return FilledWith(app, app.Theme.Surface)
            .Where(r => r.X <= picker.X && r.Y <= picker.Y && r.Bottom >= picker.Bottom)
            .MinBy(r => r.Width * r.Height);
    }

    private static List<UiRect> FilledWith(AppDriver app, uint color)
    {
        return app.Frames.Canvas.Rects
            .Where(r => r.Inputs.Style.BackgroundColor == color)
            .Select(r => app.Frames.ToUi(r.Inputs.Position))
            .ToList();
    }

    private static UiRect ControlAround(List<UiRect> controls, UiRect text) => controls.Single(r => Contains(r, text));

    private static bool Contains(UiRect outer, UiRect inner) =>
        inner.X >= outer.X && inner.X + inner.Width <= outer.X + outer.Width && Math.Abs(CentreY(inner) - CentreY(outer)) < outer.Height / 2;

    private static float CentreY(UiRect rect) => rect.Y + rect.Height / 2;

    private static bool IsDrawn(AppDriver app, string text) => app.Frames.TextRects().Any(t => t.Text == text);

    private static List<UiRect> TwinRects(AppDriver app)
    {
        return app.Frames.TextRects()
            .Where(t => t.Text == "Twin")
            .Select(t => t.Rect)
            .ToList();
    }

    [Fact]
    public void The_ui_language_is_picked_from_the_panel_too()
    {
        var app = AppDriver.Opened(width: 1600f);
        OpenDisplayOptions(app.Ui);
        app.Run();

        app.Click("English").Frame();
        app.Click("Deutsch").Frame();

        Assert.Equal(Localization.Locale.De, app.AppState.Locale);
        Assert.True(app.Frames.Drew("Sprache"));
    }

    [Fact]
    public void Cancelling_the_reset_question_keeps_the_options()
    {
        var app = AppDriver.Opened(width: 1600f, height: TallWindow);
        OpenDisplayOptions(app.Ui);
        app.Run();
        app.Click("Line numbers").Frame();

        app.Click("Reset to defaults").Frame(2);
        app.Click("Cancel").Frame(2);

        Assert.False(app.Ui.ConfirmingDisplayReset);
        Assert.False(app.Ui.Display.ShowLineNumbers);
    }

    [Fact]
    public void A_font_size_stepped_back_to_the_themes_is_no_longer_a_choice_of_its_own()
    {
        var app = AppDriver.Opened(width: 1600f);
        OpenDisplayOptions(app.Ui);
        app.Run();

        app.ClickLast(Icons.OptionsFontLarger).Frame();
        Assert.True(IsBold(app, "Font size"));
        app.ClickLast(Icons.OptionsFontSmaller).Frame();

        Assert.Null(app.Ui.Display.FontSize);
        Assert.False(IsBold(app, "Font size"));
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
        app.Frames.Key(Shortcuts.ZoomIn).Frame();

        Assert.Equal(size + 1f, app.Ui.Display.FontSize);
    }

    private static readonly byte[] FontBytes = [1, 2, 3, 4];

    private static SystemFontFamily Family(string name, string path, bool monospaced = true)
    {
        var file = new SystemFontFile(path, 0);
        var face = new SystemFontFace("Regular", file, 400, SystemFontFace.NormalStretch, SystemFontStyle.Normal, monospaced, false);
        return new SystemFontFamily(name, [face]);
    }

    private static AppDriver OpenedWithFonts(params SystemFontFamily[] installed)
    {
        var app = AppDriver.Opened(width: 1600f);
        app.Frames.SystemFonts.Families.AddRange(installed);
        OpenDisplayOptions(app.Ui);
        app.Run(2);
        return app;
    }

    [Fact]
    public void The_font_picker_offers_the_installed_monospaced_fonts_after_the_bundled_one()
    {
        var app = OpenedWithFonts(Family("Consolas", "consola.ttf"), Family("Segoe UI", "segoeui.ttf", monospaced: false));

        app.Click("JetBrains Mono").Frame();

        Assert.True(app.Frames.Drew("Consolas"));
        Assert.False(app.Frames.Drew("Segoe UI"));
    }

    [Fact]
    public void A_picked_font_draws_code_from_its_file_until_the_bundled_one_is_picked_again()
    {
        using var temp = new TempDir();
        var path = temp.Write("consola.ttf", FontBytes);
        var app = OpenedWithFonts(Family("Consolas", path));

        app.Click("JetBrains Mono").Frame();
        app.Click("Consolas").Frame(2);

        Assert.Equal(new CodeFont("Consolas", path, 0), app.Ui.Display.Font);
        Assert.Equal(MonoFonts.Code, app.Frames.RegisteredFonts[^1].Family);
        Assert.Equal(FontBytes, app.Frames.RegisteredFonts[^1].Data);
        Assert.True(IsBold(app, "Font"));

        app.Click("Consolas").Frame();
        app.Click("JetBrains Mono").Frame(2);

        Assert.Null(app.Ui.Display.Font);
        Assert.NotEqual(FontBytes, app.Frames.RegisteredFonts[^1].Data);
    }

    [Fact]
    public void A_chosen_font_whose_file_is_gone_gives_way_to_the_bundled_one()
    {
        using var temp = new TempDir();
        var app = AppDriver.Opened(width: 1600f);

        SetCodeFont(app.Ui, new CodeFont("Gone Mono", temp.PathOf("gone.ttf"), 0));
        app.Run(2);

        Assert.Null(app.Ui.Display.Font);
    }

    [Fact]
    public void The_preview_draws_in_the_code_font_at_the_code_size()
    {
        var app = OpenedWithFonts();

        app.ClickLast(Icons.OptionsFontLarger).Frame();

        var preview = app.Frames.Canvas.Texts.First(t => t.Inputs.Text.StartsWith("The quick brown fox"));
        Assert.Equal(MonoFonts.Code, preview.Inputs.Style.FontFamily.Value);
        Assert.Equal(app.Ui.Theme.MonoFontSize + 1f, preview.Inputs.Style.FontSize.Value);
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

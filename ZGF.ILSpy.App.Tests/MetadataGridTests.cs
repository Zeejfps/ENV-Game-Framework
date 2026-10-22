using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class MetadataGridTests
{
    private static BrowseNode Table(WorkspaceDriver d, string name) =>
        d.Node("Metadata", "Tables").Children.Single(c => c.Text.Split(' ')[1] == name);

    private static int Column(MetadataGrid grid, string name) => grid.Columns.Select(c => c.Name).ToList().IndexOf(name);

    private static int RowNamed(MetadataGrid grid, string name)
    {
        var column = Column(grid, "Name");
        return Enumerable.Range(0, grid.RowCount).First(r => grid.Text(r, column) == name);
    }

    private static (AppDriver App, DocumentTab Tab, MetadataGrid Grid, int Row) ShowGreeterRow()
    {
        var app = new AppDriver(width: 1800f);
        var d = app.Driver;
        d.OpenFixtures();
        d.Show(Table(d, "TypeDef"));
        var tab = d.Workspace.ActiveTab!;
        var grid = tab.Grid!;
        var row = RowNamed(grid, "Greeter");
        tab.GridView.Reveal(row);
        app.Run(3);
        return (app, tab, grid, row);
    }

    /// <summary>Where <paramref name="text"/> was drawn on the same line as <paramref name="besides"/>.</summary>
    private static UiRect TextBeside(AppDriver app, string text, string besides)
    {
        var line = app.TextRect(besides);
        var drawn = app.Frames.Canvas.Texts.Select(t => t.Inputs.Position)
            .Select(p => new UiRect(p.Left, app.Frames.Height - p.Top, p.Width, p.Height))
            .Zip(app.Frames.Canvas.Texts, (rect, t) => (rect, t.Inputs.Text))
            .First(x => x.Text == text && Math.Abs(x.rect.Y - line.Y) < 1f);
        return drawn.rect;
    }

    private static void Click(AppDriver app, UiRect at, int times = 1, KeyModifiers modifiers = KeyModifiers.None)
    {
        app.Frames.MovePointer(at.X + at.Width / 2, at.Y + at.Height / 2);
        app.Run();
        for (var i = 0; i < times; i++)
        {
            app.Frames.Press(modifiers);
            app.Run();
            app.Frames.Release(modifiers: modifiers);
            app.Run();
        }
    }

    [Fact]
    public void A_table_opens_as_a_grid_of_ILSpys_columns_that_switching_language_leaves_alone()
    {
        var app = new AppDriver(width: 1800f);
        var d = app.Driver;
        d.OpenFixtures();

        d.Show(Table(d, "TypeDef"));
        var tab = d.Workspace.ActiveTab!;
        var grid = tab.Grid!;
        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        d.Settle();
        app.Run(2);

        Assert.Same(grid, tab.Grid);
        Assert.Equal(["RID", "Token", "Offset", "Attributes", "Name", "Namespace", "BaseType", "FieldList", "MethodList"], grid.Columns.Select(c => c.Name));
        app.TextRect("RID");
        app.TextRect("<Module>");
    }

    [Fact]
    public void Columns_wider_than_the_view_scroll_sideways_with_their_headers()
    {
        var app = new AppDriver(width: 700f);
        var d = app.Driver;
        d.OpenFixtures();
        d.Show(Table(d, "TypeDef"));
        var tab = d.Workspace.ActiveTab!;
        var grid = tab.Grid!;
        app.Run(2);
        var methodList = grid.Text(0, Column(grid, "MethodList"));

        tab.GridView.ScrollX = 10000f;
        app.Run(3);

        var header = app.TextRect("MethodList");
        var cell = app.TextRect(methodList);
        Assert.Equal(header.X, cell.X, 0);
        Assert.InRange(header.X + header.Width, 0f, 700f);
    }

    [Fact]
    public void A_text_node_of_the_metadata_decompiles_to_comments()
    {
        var d = new WorkspaceDriver();
        d.OpenFixtures();

        d.Show(d.Node("Metadata", "Tables"));

        Assert.Null(d.Workspace.ActiveTab!.Grid);
        Assert.StartsWith("// Tables", d.Workspace.ActiveTab.Document.GetText());
    }

    [Fact]
    public void Ctrl_clicking_a_token_shows_its_row_and_going_back_returns_to_the_row_it_left()
    {
        var (app, tab, grid, row) = ShowGreeterRow();
        var d = app.Driver;
        var baseType = grid.Text(row, Column(grid, "BaseType"));
        Click(app, TextBeside(app, "Greeter", "Greeter"));

        Click(app, TextBeside(app, baseType, "Greeter"), modifiers: KeyModifiers.Control);
        d.Settle();
        app.Run(3);

        var typeRefs = tab.Grid!;
        Assert.Same(Table(d, "TypeRef"), tab.Node);
        Assert.Equal("Object", typeRefs.Text(tab.GridView.Selected, Column(typeRefs, "Name")));
        app.TextRect("Object");

        d.Workspace.GoBack();
        d.Settle();
        app.Run(3);

        Assert.Same(Table(d, "TypeDef"), tab.Node);
        Assert.Equal(row, tab.GridView.Selected);
    }

    [Fact]
    public void Double_clicking_a_row_decompiles_what_its_token_names()
    {
        var (app, tab, _, _) = ShowGreeterRow();

        Click(app, TextBeside(app, "Greeter", "Greeter"), times: 2);
        app.Driver.Settle();

        Assert.Same(app.Driver.Fixture("Greeter"), tab.Node);
        Assert.Null(tab.Grid);
    }

    [Fact]
    public void Enter_on_the_selected_row_decompiles_it_and_Ctrl_C_copies_the_current_cell()
    {
        var (app, tab, grid, row) = ShowGreeterRow();
        Click(app, TextBeside(app, "Greeter", "Greeter"));

        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control);
        app.Run();
        var copied = app.Frames.Clipboard.Text;
        app.Frames.Key(KeyboardKey.Enter);
        app.Run();
        app.Driver.Settle();

        Assert.Equal("Greeter", copied);
        Assert.Same(app.Driver.Fixture("Greeter"), tab.Node);
    }

    [Fact]
    public void Clicking_a_header_sorts_by_its_column_and_again_the_other_way()
    {
        var (app, tab, grid, _) = ShowGreeterRow();
        var name = Column(grid, "Name");

        Click(app, app.TextRect("Name"));
        var ascending = tab.GridView.Order!.Select(r => grid.Text(r, name)).ToList();
        Click(app, app.TextRect("Name"));
        var descending = tab.GridView.Order!.Select(r => grid.Text(r, name)).ToList();

        Assert.Equal(ascending.Order(StringComparer.CurrentCulture), ascending);
        Assert.Equal(descending.OrderDescending(StringComparer.CurrentCulture), descending);
    }

    [Fact]
    public void Hovering_a_flags_cell_breaks_its_bits_down()
    {
        var (app, _, grid, row) = ShowGreeterRow();
        var attributes = TextBeside(app, grid.Text(row, Column(grid, "Attributes")), "Greeter");

        app.Frames.MovePointer(attributes.X + 2f, attributes.Y + attributes.Height / 2);
        for (var i = 0; i < 90; i++)
            app.Run();

        app.TextRect("Visibility: ");
        app.TextRect("Public (0001)");
        app.TextRect("BeforeFieldInit (100000)");
    }

    private static (AppDriver App, DocumentTab Tab, MetadataGrid Grid) ShowTable(string name)
    {
        var app = new AppDriver(width: 1800f);
        app.Driver.OpenFixtures();
        app.Driver.Show(Table(app.Driver, name));
        app.Run(3);
        var tab = app.Workspace.ActiveTab!;
        return (app, tab, tab.Grid!);
    }

    /// <summary>The funnel drawn in the header of <paramref name="column"/>.</summary>
    private static UiRect FunnelOf(AppDriver app, string column)
    {
        var header = app.TextRect(column);
        return app.Frames.Canvas.Texts
            .Where(t => t.Inputs.Text == LucideIcons.Filter)
            .Select(t => new UiRect(t.Inputs.Position.Left, app.Frames.Height - t.Inputs.Position.Top, t.Inputs.Position.Width, t.Inputs.Position.Height))
            .Where(r => r.X > header.X && Math.Abs(r.Y + r.Height / 2 - (header.Y + header.Height / 2)) < 4f)
            .MinBy(r => r.X);
    }

    [Fact]
    public void Typing_in_a_columns_filter_keeps_the_rows_that_match_and_Escape_clears_it()
    {
        var (app, tab, grid) = ShowTable("TypeDef");
        var name = Column(grid, "Name");

        Click(app, FunnelOf(app, "Name"));
        app.Frames.Type("greet");
        app.Run(2);
        var filtered = tab.GridView.View.Select(r => grid.Text(r, name)).ToList();
        app.Frames.Key(KeyboardKey.Escape);
        app.Run(2);

        Assert.Contains("Greeter", filtered);
        Assert.All(filtered, n => Assert.Contains("greet", n, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(grid.RowCount, tab.GridView.View.Length);
    }

    [Fact]
    public void A_flags_filter_picks_values_in_its_popup_and_is_kept_for_that_column_of_every_table_of_the_kind()
    {
        var (app, tab, grid) = ShowTable("TypeDef");
        var attributes = Column(grid, "Attributes");

        Click(app, FunnelOf(app, "Attributes"));
        app.ClickMenuItem("Public (0001)");
        app.Run(2);

        var visibility = System.Reflection.TypeAttributes.VisibilityMask;
        Assert.NotEmpty(tab.GridView.View);
        Assert.All(tab.GridView.View, r =>
            Assert.Equal(1u, Convert.ToUInt32(grid.Text(r, attributes), 16) & (uint)visibility));
        app.TextRect("Visibility = Public");

        var saved = app.Workspace.MetadataFilters.Save();
        var restored = new MetadataFilters(saved).For(grid, attributes);
        Assert.Equal("Visibility = Public", FilterStatePresenter.Describe(restored));
    }

    [Fact]
    public void Hovering_a_headers_flags_value_breaks_its_bits_down()
    {
        var app = new AppDriver(width: 1800f, height: 1200f);
        app.Driver.OpenFixtures();
        app.Driver.Show(app.Driver.Node("Metadata", "COFF Header"));
        app.Run(3);
        var grid = app.Workspace.ActiveTab!.Grid!;
        var row = Enumerable.Range(0, grid.RowCount).Single(r => grid.Text(r, Column(grid, "Member")) == "Characteristics");
        var value = TextBeside(app, grid.Text(row, Column(grid, "Value")), "Characteristics");

        app.Frames.MovePointer(value.X + 2f, value.Y + value.Height / 2);
        for (var i = 0; i < 90; i++)
            app.Run();

        app.TextRect("<0002> File is executable");
        app.TextRect("<2000> DLL");
    }

    private static void Drag(AppDriver app, float fromX, float toX, float y)
    {
        app.Frames.Wait(1);
        app.Frames.MovePointer(fromX, y);
        app.Run();
        app.Frames.Press();
        app.Run();
        app.Frames.MovePointer(toX, y);
        app.Run();
        app.Frames.Release();
        app.Run();
    }

    [Fact]
    public void Dragging_across_a_cell_selects_part_of_its_text_for_Ctrl_C()
    {
        var (app, tab, _, row) = ShowGreeterRow();
        var name = TextBeside(app, "Greeter", "Greeter");
        var advance = name.Width / "Greeter".Length;

        Drag(app, name.X + 1f, name.X + advance * 4, name.Y + name.Height / 2);
        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control);
        app.Run();

        Assert.Equal("Gree", app.Frames.Clipboard.Text);
        Assert.Equal(row, tab.GridView.Selected);
    }

    [Fact]
    public void The_selected_cells_outline_is_drawn_over_the_text_selected_in_it()
    {
        var (app, _, _, _) = ShowGreeterRow();
        var theme = AppThemes.For(app.Workspace.DarkMode).Ui;
        var name = TextBeside(app, "Greeter", "Greeter");

        Drag(app, name.X + 1f, name.X + name.Width - 1f, name.Y + name.Height / 2);
        app.Run();

        var rects = app.Frames.Canvas.Rects;
        var highlight = rects.Last(r => r.Inputs.Style.BackgroundColor == theme.Selection).Sequence;
        var outline = rects.Last(r => r.Inputs.Style.BorderColor.Top.Value == theme.FocusRing).Sequence;
        Assert.True(outline > highlight);
    }

    [Fact]
    public void Clicking_a_token_selects_all_its_text_instead_of_following_it()
    {
        var (app, tab, grid, row) = ShowGreeterRow();
        var baseType = grid.Text(row, Column(grid, "BaseType"));

        Click(app, TextBeside(app, baseType, "Greeter"));
        app.Driver.Settle();
        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control);
        app.Run();

        Assert.Same(Table(app.Driver, "TypeDef"), tab.Node);
        Assert.Equal(baseType, app.Frames.Clipboard.Text);
    }

    [Fact]
    public void Moving_to_another_cell_drops_the_text_selected_in_the_last()
    {
        var (app, _, _, _) = ShowGreeterRow();
        var name = TextBeside(app, "Greeter", "Greeter");
        Drag(app, name.X + 1f, name.X + name.Width / 2, name.Y + name.Height / 2);

        app.Frames.Key(KeyboardKey.LeftArrow);
        app.Run();
        app.Frames.Key(KeyboardKey.RightArrow);
        app.Run();
        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control);
        app.Run();

        Assert.Equal("Greeter", app.Frames.Clipboard.Text);
    }

    [Fact]
    public void Selecting_debug_information_shows_its_decoded_value_under_it()
    {
        var app = new AppDriver(width: 1800f, height: 900f);
        var d = app.Driver;
        d.OpenFixtures();
        var tables = d.Node("Debug Metadata (From portable PDB)", "Tables");
        d.Show(tables.Children.Single(c => c.Text.Split(' ')[1] == "CustomDebugInformation"));
        var tab = app.Workspace.ActiveTab!;
        var grid = tab.Grid!;
        var kind = Column(grid, "KindString");
        var options = Enumerable.Range(0, grid.RowCount).Single(r => grid.Text(r, kind).StartsWith("Compilation Options"));

        tab.GridView.Reveal(options);
        app.Run(4);

        Assert.Equal(options, tab.GridView.Selected);
        Assert.True(app.TextRect("language").Y > TextBeside(app, grid.Text(options, kind), grid.Text(options, kind)).Y);
    }
}

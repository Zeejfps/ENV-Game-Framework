using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class ResourceViewTests
{
    private static (AppDriver App, DocumentTab Tab) ShowResource(params string[] path)
    {
        var app = new AppDriver(width: 1400f);
        app.Driver.OpenFixtures();
        app.Driver.Show(app.Driver.Node(["Resources", .. path]));
        app.Run(3);
        return (app, app.Workspace.ActiveTab!);
    }

    [Fact]
    public void A_picture_shows_at_its_size_under_what_it_is()
    {
        var (app, tab) = ShowResource("logo.png");

        var drawn = Assert.Single(app.Frames.Canvas.Images, i => i.Inputs.ImageId == tab.ImageId);

        Assert.NotNull(tab.Image);
        Assert.Equal((2f, 2f), (drawn.Inputs.Position.Width, drawn.Inputs.Position.Height));
        app.TextRect("PNG, 2 × 2 pixels");
        var export = app.TextRect("Export");
        var icon = app.Frames.Canvas.Texts.Select(t => t.Inputs).Single(t => t.Text == LucideIcons.Upload).Position;
        Assert.True(icon.Left + icon.Width <= export.X);
        Assert.Equal(export.Y + export.Height / 2, app.Frames.Height - icon.Top + icon.Height / 2, 2f);
    }

    [Fact]
    public void A_picture_in_a_resources_file_shows_too()
    {
        var (app, tab) = ShowResource("Mixed.resources", "Picture.png");

        Assert.Equal((2, 2), (tab.Image!.Width, tab.Image.Height));
        Assert.Contains(app.Frames.Canvas.Images, i => i.Inputs.ImageId == tab.ImageId);
    }

    [Fact]
    public void Closing_its_tab_frees_the_picture()
    {
        var (app, tab) = ShowResource("logo.png");
        Assert.True(app.Frames.Canvas.HasImage(tab.ImageId));

        app.Workspace.CloseTab(tab);
        app.Run(2);

        Assert.False(app.Frames.Canvas.HasImage(tab.ImageId));
    }

    [Fact]
    public void A_picture_that_cannot_be_read_shows_why_and_its_bytes()
    {
        var (_, tab) = ShowResource("broken.png");

        Assert.Null(tab.Image);
        var text = tab.Document.GetText();
        Assert.StartsWith("// This is no PNG, JPEG, BMP, GIF, ICO or CUR picture.", text);
        Assert.Contains("not a picture", text);
    }

    [Fact]
    public void A_resources_file_shows_its_entries_as_a_grid()
    {
        var (app, tab) = ShowResource("Mixed.resources");

        Assert.Equal(["Name", "Value", "Type"], tab.Grid!.Columns.Select(c => c.Name));
        app.TextRect("Greeting");
        app.TextRect("Hello");
    }

    [Fact]
    public void A_multi_line_string_shows_its_first_line_and_how_many_more_follow()
    {
        var (app, _) = ShowResource("Mixed.resources");
        var first = app.TextRect("first line");
        var more = app.TextRect("+1 lines");

        Assert.Throws<InvalidOperationException>(() => app.TextRect("second line"));
        Assert.True(more.X > first.X + first.Width);
        Assert.Equal(first.Y, more.Y, 4f);
    }

    private static void Press(AppDriver app, UiRect at, int times = 1)
    {
        app.Frames.Wait(1);
        app.Frames.MovePointer(at.X + at.Width / 2, at.Y + at.Height / 2);
        app.Run();
        for (var i = 0; i < times; i++)
        {
            app.Frames.Press();
            app.Run();
            app.Frames.Release();
            app.Run();
        }
        app.Run(2);
    }

    [Fact]
    public void Clicking_a_multi_line_string_grows_its_cell_to_show_every_line_as_the_cell_shows_its_first()
    {
        var (app, _) = ShowResource("Mixed.resources");
        var theme = AppThemes.For(app.Workspace.DarkMode).Ui;
        var before = app.TextRect("first line");
        var color = app.Frames.TextColor("first line");

        Press(app, before);

        var first = app.TextRect("first line");
        var second = app.TextRect("second line");
        Assert.Equal((before.X, before.Y), (first.X, first.Y));
        Assert.Equal(first.X, second.X);
        Assert.Equal(theme.RowHeight, MathF.Abs(second.Y - first.Y), 0.5f);
        Assert.Equal(color, app.Frames.TextColor("second line"));
        Assert.Equal(2 * theme.RowHeight, MathF.Abs(app.TextRect("Notes.xml").Y - app.TextRect("Multiline").Y), 0.5f);
        Assert.Throws<InvalidOperationException>(() => app.TextRect("+1 lines"));
    }

    [Fact]
    public void Escape_drops_the_text_selected_in_an_open_string_then_closes_it()
    {
        var (app, _) = ShowResource("Mixed.resources");

        Press(app, app.TextRect("first line"));
        app.Frames.Key(KeyboardKey.Escape);
        app.Run(2);
        Assert.True(app.Frames.Drew("second line"));
        app.Frames.Key(KeyboardKey.Escape);
        app.Run(2);
        Assert.False(app.Frames.Drew("second line"));
    }

    [Fact]
    public void Several_strings_stay_open_while_other_cells_are_selected_and_no_row_moves_under_the_pointer()
    {
        var (app, _) = ShowResource("Mixed.resources");

        Press(app, app.TextRect("first line"));
        var roses = app.TextRect("roses");
        Press(app, roses);
        Assert.Equal(roses.Y, app.TextRect("roses").Y, 0.5f);
        var greeting = app.TextRect("Greeting");
        Press(app, greeting);

        Assert.Equal(greeting.Y, app.TextRect("Greeting").Y, 0.5f);
        Assert.True(app.Frames.Drew("second line"));
        Assert.True(app.Frames.Drew("violets"));
    }

    [Fact]
    public void A_click_selects_a_whole_multi_line_string_for_Ctrl_C_and_a_double_click_a_word_of_a_name()
    {
        var (app, _) = ShowResource("Mixed.resources");

        Press(app, app.TextRect("first line"));
        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control);
        app.Run();
        Assert.Equal("first line\nsecond line", app.Frames.Clipboard.Text);

        var name = app.TextRect("Notes.xml");
        Press(app, name with { Width = name.Width / 3 }, times: 2);
        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control);
        app.Run();
        Assert.Equal("Notes", app.Frames.Clipboard.Text);
    }

    [Fact]
    public void Saving_a_resource_writes_its_bytes()
    {
        var (app, _) = ShowResource("data.json");
        var node = app.Driver.Node("Resources", "data.json");
        var path = Path.Combine(Path.GetTempPath(), $"zgf-ilspy-{Guid.NewGuid():N}.json");
        try
        {
            app.Workspace.SaveResource(node, path);

            Assert.Equal(node.ReadResource(), File.ReadAllBytes(path));
            Assert.Empty(app.Workspace.Errors.Entries);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Docking;
using static ZGF.ILSpy.Exports;

namespace ZGF.ILSpy.App.Tests;

public sealed class ResourceViewTests
{
    private static (AppDriver App, DocumentTab Tab) ShowResource(params string[] path)
    {
        var app = AppDriver.Opened(width: 1400f);
        app.Show(app.Node(["Resources", .. path]), 3);
        return (app, app.AppState.Tabs.Active!);
    }

    private static string ImageOf(AppDriver app, DocumentTab tab) => app.Ui.ViewOf(tab).ImageId;

    [Fact]
    public void A_picture_shows_at_its_size_under_what_it_is()
    {
        var (app, tab) = ShowResource("logo.png");

        var drawn = Assert.Single(app.Frames.Canvas.Images, i => i.Inputs.ImageId == ImageOf(app, tab));

        Assert.NotNull(tab.Image);
        Assert.Equal((2f, 2f), (drawn.Inputs.Position.Width, drawn.Inputs.Position.Height));
        app.TextRect("PNG, 2 × 2 pixels");
        var export = app.TextRect("Export");
        var icon = app.Frames.TextRect(LucideIcons.Upload);
        Assert.True(icon.Right <= export.X);
        Assert.Equal(export.Y + export.Height / 2, icon.Y + icon.Height / 2, 2f);
    }

    [Fact]
    public void A_picture_in_a_resources_file_shows_too()
    {
        var (app, tab) = ShowResource("Mixed.resources", "Picture.png");

        Assert.Equal((2, 2), (tab.Image!.Width, tab.Image.Height));
        Assert.Contains(app.Frames.Canvas.Images, i => i.Inputs.ImageId == ImageOf(app, tab));
    }

    private static (float Width, float Height) DrawnSize(AppDriver app, DocumentTab tab)
    {
        var drawn = Assert.Single(app.Frames.Canvas.Images, i => i.Inputs.ImageId == ImageOf(app, tab));
        return (drawn.Inputs.Position.Width, drawn.Inputs.Position.Height);
    }

    private static (int Width, int Height) UploadedSize(AppDriver app, DocumentTab tab) =>
        (app.Frames.Canvas.GetImageWidth(ImageOf(app, tab)), app.Frames.Canvas.GetImageHeight(ImageOf(app, tab)));

    [Fact]
    public void Zoom_buttons_draw_the_picture_larger_and_back_at_its_own_size()
    {
        var (app, tab) = ShowResource("logo.png");

        app.ClickLast(LucideIcons.ZoomIn).Frame();
        app.ClickLast(LucideIcons.ZoomIn).Frame();
        var zoomed = DrawnSize(app, tab);
        app.TextRect("400%");
        app.ClickLast(LucideIcons.RotateCcw).Frame();

        Assert.Equal((8f, 8f), zoomed);
        Assert.Equal((2f, 2f), DrawnSize(app, tab));
        Assert.Equal((2, 2), UploadedSize(app, tab));
    }

    [Fact]
    public void A_zoomed_svg_is_drawn_again_at_the_larger_size()
    {
        var (app, tab) = ShowResource("shape.svg");

        app.ClickLast(LucideIcons.ZoomIn).Frame();

        Assert.Equal((8f, 4f), DrawnSize(app, tab));
        Assert.Equal((8, 4), UploadedSize(app, tab));
    }

    [Fact]
    public void A_zoom_lasts_until_another_picture_arrives_in_the_tab()
    {
        var (app, tab) = ShowResource("shape.svg");
        app.ClickLast(LucideIcons.ZoomIn).Frame();
        var zoomedBefore = !app.Ui.ViewOf(tab).Picture.Zoom.IsOwnSize;

        app.Show(app.Node(["Resources", "logo.png"]), 3);

        Assert.True(zoomedBefore);
        Assert.True(app.Ui.ViewOf(tab).Picture.Zoom.IsOwnSize);
        Assert.Equal((2f, 2f), DrawnSize(app, tab));
    }

    [Fact]
    public void An_svg_switches_to_its_markup_and_back()
    {
        var (app, tab) = ShowResource("shape.svg");

        app.Click("Source").Frame(2);
        var showsSource = app.Ui.ViewOf(tab).Picture.ShowsSource;
        var drewPicture = app.Frames.Canvas.Images.Any(i => i.Inputs.ImageId == ImageOf(app, tab));
        app.Click("Picture").Frame(2);

        Assert.True(showsSource);
        Assert.False(drewPicture);
        Assert.Contains("<svg", tab.Document.GetText());
        Assert.False(app.Ui.ViewOf(tab).Picture.ShowsSource);
        Assert.Equal((4f, 2f), DrawnSize(app, tab));
    }

    [Fact]
    public void A_raster_picture_offers_no_markup()
    {
        var (app, _) = ShowResource("logo.png");

        Assert.False(app.Frames.Drew("Source"));
    }

    [Fact]
    public void Closing_its_tab_frees_the_picture()
    {
        var (app, tab) = ShowResource("logo.png");
        var image = ImageOf(app, tab);
        Assert.True(app.Frames.Canvas.HasImage(image));

        CloseTab(app.AppState, tab);
        app.Run(2);

        Assert.False(app.Frames.Canvas.HasImage(image));
    }

    [Fact]
    public void A_picture_that_cannot_be_read_shows_why_and_its_bytes()
    {
        var (_, tab) = ShowResource("broken.png");

        Assert.Null(tab.Image);
        var text = tab.Document.GetText();
        Assert.StartsWith("// This is no PNG, JPEG, BMP, GIF, ICO, CUR or SVG picture.", text);
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

    private static void Click(AppDriver app, UiRect at) => app.Frames.Wait(1).Click(at).Frame(2);

    [Fact]
    public void Clicking_a_multi_line_string_grows_its_cell_to_show_every_line_as_the_cell_shows_its_first()
    {
        var (app, _) = ShowResource("Mixed.resources");
        var theme = app.Theme;
        var before = app.TextRect("first line");
        var color = app.Frames.TextColor("first line");

        Click(app, before);

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

        Click(app, app.TextRect("first line"));
        app.Frames.Key(KeyboardKey.Escape).Frame(2);
        Assert.True(app.Frames.Drew("second line"));
        app.Frames.Key(KeyboardKey.Escape).Frame(2);
        Assert.False(app.Frames.Drew("second line"));
    }

    [Fact]
    public void Several_strings_stay_open_while_other_cells_are_selected_and_no_row_moves_under_the_pointer()
    {
        var (app, _) = ShowResource("Mixed.resources");

        Click(app, app.TextRect("first line"));
        var roses = app.TextRect("roses");
        Click(app, roses);
        Assert.Equal(roses.Y, app.TextRect("roses").Y, 0.5f);
        var greeting = app.TextRect("Greeting");
        Click(app, greeting);

        Assert.Equal(greeting.Y, app.TextRect("Greeting").Y, 0.5f);
        Assert.True(app.Frames.Drew("second line"));
        Assert.True(app.Frames.Drew("violets"));
    }

    [Fact]
    public void A_click_selects_a_whole_multi_line_string_for_Ctrl_C_and_a_double_click_a_word_of_a_name()
    {
        var (app, _) = ShowResource("Mixed.resources");

        Click(app, app.TextRect("first line"));
        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control).Frame();
        Assert.Equal("first line\nsecond line", app.Frames.Clipboard.Text);

        var name = app.TextRect("Notes.xml");
        app.Frames.Wait(1).DoubleClick(name with { Width = name.Width / 3 }).Frame(2);
        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control).Frame();
        Assert.Equal("Notes", app.Frames.Clipboard.Text);
    }

    [Fact]
    public void Saving_a_resource_writes_its_bytes()
    {
        var (app, _) = ShowResource("data.json");
        var node = app.Node("Resources", "data.json");
        using var temp = new TempDir();
        Directory.CreateDirectory(temp.Path);
        var path = temp.PathOf("data.json");

        SaveResource(app.AppState, node, path);

        Assert.Equal(node.ReadResource(), File.ReadAllBytes(path));
        Assert.Empty(app.AppState.Errors.Entries);
    }
}

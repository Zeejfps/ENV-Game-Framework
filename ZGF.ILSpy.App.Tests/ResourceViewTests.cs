using ZGF.ILSpy.Core;

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
    public void A_multi_line_string_shows_its_first_line_and_folds_out_in_full_from_its_chevron()
    {
        var (app, tab) = ShowResource("Mixed.resources");
        var first = app.TextRect("first line");
        var more = app.TextRect("+1 lines");
        Assert.Throws<InvalidOperationException>(() => app.TextRect("second line"));

        var chevron = app.Frames.Canvas.Texts.Select(t => t.Inputs)
            .Single(t => t.Text == LucideIcons.ChevronRight && Math.Abs(app.Frames.Height - t.Position.Top - first.Y) < 4f).Position;
        app.Frames.MovePointer(chevron.Left + chevron.Width / 2, app.Frames.Height - chevron.Top + chevron.Height / 2);
        app.Run();
        app.Frames.Press();
        app.Run();
        app.Frames.Release();
        app.Run(3);

        Assert.True(more.X > first.X + first.Width);
        Assert.Equal(first.Y, more.Y, 4f);
        Assert.Contains(app.Frames.Canvas.GlyphRuns, r => r.Text == "second line");
    }

    private static void Unfold(AppDriver app, string firstLine)
    {
        var row = app.TextRect(firstLine);
        var chevron = app.Frames.Canvas.Texts.Select(t => t.Inputs)
            .Single(t => t.Text == LucideIcons.ChevronRight && Math.Abs(app.Frames.Height - t.Position.Top - row.Y) < 4f).Position;
        app.Frames.MovePointer(chevron.Left + chevron.Width / 2, app.Frames.Height - chevron.Top + chevron.Height / 2);
        app.Run();
        app.Frames.Press();
        app.Run();
        app.Frames.Release();
        app.Run(3);
    }

    [Fact]
    public void Several_strings_fold_out_at_once_each_with_its_own_text()
    {
        var (app, _) = ShowResource("Mixed.resources");

        Unfold(app, "first line");
        Unfold(app, "roses");

        Assert.Contains(app.Frames.Canvas.GlyphRuns, r => r.Text == "second line");
        Assert.Contains(app.Frames.Canvas.GlyphRuns, r => r.Text == "violets");
    }

    [Fact]
    public void Clicking_a_rows_name_folds_it_out_and_in()
    {
        var (app, _) = ShowResource("Mixed.resources");
        var name = app.TextRect("Multiline");

        void ClickName()
        {
            app.Frames.MovePointer(name.X + name.Width / 2, name.Y + name.Height / 2);
            app.Run();
            app.Frames.Press();
            app.Run();
            app.Frames.Release();
            app.Run(3);
        }

        ClickName();
        Assert.Contains(app.Frames.Canvas.GlyphRuns, r => r.Text == "second line");
        app.Frames.Wait(1);
        ClickName();
        Assert.DoesNotContain(app.Frames.Canvas.GlyphRuns, r => r.Text == "second line");
    }

    [Fact]
    public void A_folded_out_string_is_as_tall_as_its_lines()
    {
        var (app, _) = ShowResource("Mixed.resources");
        var theme = AppThemes.For(app.Workspace.DarkMode).Ui;

        Unfold(app, "first line");
        var row = app.TextRect("first line").Y;
        var lines = app.Frames.Canvas.GlyphRuns.Where(r => r.Text is "first line" or "second line").Select(r => app.Frames.Height - r.Origin.Y).Order().ToArray();
        var next = app.TextRect("Notes.xml").Y;

        Assert.Equal(2, lines.Length);
        Assert.Equal(theme.RowHeight + 2 * MathF.Abs(lines[1] - lines[0]) + theme.GapTight, next - row, 0.5f);
    }

    [Fact]
    public void A_folded_out_string_shows_only_its_text_without_line_numbers()
    {
        var (app, _) = ShowResource("Mixed.resources");

        Unfold(app, "first line");

        Assert.Contains(app.Frames.Canvas.GlyphRuns, r => r.Text == "second line");
        Assert.DoesNotContain(app.Frames.Canvas.GlyphRuns, r => r.Text.Trim() is "1" or "2");
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

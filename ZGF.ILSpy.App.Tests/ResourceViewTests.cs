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
        app.TextRect("Save…");
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

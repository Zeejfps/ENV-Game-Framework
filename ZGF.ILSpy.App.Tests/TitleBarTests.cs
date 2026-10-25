using ZGF.Desktop;
using ZGF.Gui.Immediate;
using static ZGF.ILSpy.CodeDisplay;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy.App.Tests;

public sealed class TitleBarTests : IDisposable
{
    private const string SystemTitleBarOption = "Use system title bar";
    private const string AppIcon = "title-bar-icon";
    private const float OptionsWidth = 1600f;
    private const float OptionsHeight = 1000f;
    private static readonly UiRect CaptionButtons = new(862f, 0f, 138f, 32f);

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    private static AppDriver WithTitleBarToDrawIn(float width = 1000f, float height = 700f)
    {
        var app = new AppDriver(width, height);
        app.Frames.CanDrawTitleBar = true;
        app.Frames.CaptionButtons = CaptionButtons with { X = width - CaptionButtons.Width };
        app.Frames.WindowIcon = new WindowIcon([new WindowIconImage(16, 16, new byte[16 * 16 * 4])]);
        app.Run(2);
        return app;
    }

    [Fact]
    public void Where_the_window_allows_it_the_menus_are_drawn_in_its_title_bar_and_the_rest_of_it_moves_the_window()
    {
        var app = WithTitleBarToDrawIn();

        Assert.Equal(app.Theme.Background, app.Frames.TitleBarBackground);
        Assert.True(app.TextRect("File").Bottom <= CaptionButtons.Bottom);
        var drag = Assert.Single(app.Frames.TitleBarDragAreas);
        Assert.True(drag.X >= app.TextRect("View").Right);
        Assert.Equal(CaptionButtons.X, drag.Right);
        Assert.Equal(CaptionButtons.Height, drag.Height);
    }

    [Fact]
    public void The_app_icon_comes_first_in_the_title_bar_before_the_menus()
    {
        var app = WithTitleBarToDrawIn();

        var icon = app.Frames.RectOf(AppIcon);
        Assert.True(icon.Right <= app.TextRect("File").X);
        Assert.True(icon.Bottom <= CaptionButtons.Bottom);
    }

    [Fact]
    public void Maximize_in_the_app_icon_menu_maximizes_the_window()
    {
        var app = WithTitleBarToDrawIn();

        app.Frames.ClickOn(AppIcon);
        app.Run();
        app.ClickMenuItem("Maximize");

        Assert.Equal(WindowState.Maximized, app.Frames.RequestedWindowState);
    }

    [Fact]
    public void Restore_in_the_app_icon_menu_restores_the_window_only_while_maximized()
    {
        var app = WithTitleBarToDrawIn();

        app.Frames.ClickOn(AppIcon);
        app.Run();
        app.ClickMenuItem("Restore");
        Assert.Null(app.Frames.RequestedWindowState);

        app.Frames.WindowBounds = new WindowBounds(0, 0, 1000, 700, Maximized: true);
        app.Frames.ClickOn(AppIcon);
        app.Run();
        app.ClickMenuItem("Restore");
        Assert.Equal(WindowState.Normal, app.Frames.RequestedWindowState);
    }

    [Fact]
    public void Use_system_title_bar_in_the_display_options_puts_the_menus_in_a_bar_of_their_own_and_back()
    {
        var app = WithTitleBarToDrawIn(OptionsWidth, OptionsHeight);
        OpenDisplayOptions(app.Ui);
        app.Run();

        app.Click(SystemTitleBarOption).Frame(2);

        Assert.True(app.Ui.Display.SystemTitleBar);
        Assert.Null(app.Frames.TitleBarBackground);
        Assert.Empty(app.Frames.TitleBarDragAreas);

        app.Click(SystemTitleBarOption).Frame(2);

        Assert.False(app.Ui.Display.SystemTitleBar);
        Assert.NotNull(app.Frames.TitleBarBackground);
    }

    [Fact]
    public void Where_the_window_keeps_its_title_bar_to_itself_the_menus_have_a_bar_of_their_own_and_no_title_bar_option()
    {
        var app = new AppDriver(OptionsWidth, OptionsHeight);
        OpenDisplayOptions(app.Ui);
        app.Run(2);

        Assert.Null(app.Frames.TitleBarBackground);
        Assert.True(app.Frames.Drew("Language"));
        Assert.False(app.Frames.Drew(SystemTitleBarOption));
    }

    [Fact]
    public void The_system_title_bar_is_still_kept_after_a_restart()
    {
        var d = new AppStateDriver(_temp.Settings());
        SetDisplayOptions(d.Ui, d.Ui.Display with { SystemTitleBar = true });
        d.NextFrame();
        SaveSettings(d.AppState);

        var next = new AppStateDriver(_temp.Settings());

        Assert.True(next.Ui.Display.SystemTitleBar);
    }
}

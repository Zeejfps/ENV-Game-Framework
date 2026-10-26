using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class AppIconMenuTests
{
    private const string Icon = "app-icon";
    private readonly UiFrames _frames;

    public AppIconMenuTests() => _frames = new UiFrames(new DefaultUiState(), body: Body);

    private static void Body(Ui ui)
    {
        using (ui.MenuBar("bar"))
        {
            using (var window = ui.AppIconMenu(Icon))
            {
                if (window.IsOpen)
                    ui.MenuItem("Minimize");
            }
            using (var file = ui.Menu("File"))
            {
                if (file.IsOpen)
                    ui.MenuItem("Open");
            }
        }
    }

    private static WindowIcon IconOf16() => new([new WindowIconImage(16, 16, new byte[16 * 16 * 4])]);

    private void HoverOver(string id)
    {
        var title = _frames.RectOf(id);
        _frames.MovePointer(title.X + title.Width / 2f, title.Y + title.Height / 2f).Frame(2);
    }

    [Fact]
    public void Clicking_the_app_icon_opens_its_menu_below_it()
    {
        _frames.WindowIcon = IconOf16();
        _frames.Frame();

        _frames.ClickOn(Icon).Frame();

        Assert.True(_frames.Drew("Minimize"));
        Assert.True(_frames.TextRect("Minimize").Y >= _frames.RectOf(Icon).Bottom);
    }

    [Fact]
    public void Once_a_menu_is_open_hovering_the_app_icon_switches_to_its_menu_and_back()
    {
        _frames.WindowIcon = IconOf16();
        _frames.Frame().ClickOn("File").Frame();

        HoverOver(Icon);
        var iconMenu = _frames.Drew("Minimize") && !_frames.Drew("Open");
        HoverOver("File");

        Assert.True(iconMenu);
        Assert.True(_frames.Drew("Open"));
        Assert.False(_frames.Drew("Minimize"));
    }

    [Fact]
    public void Without_a_window_icon_the_bar_has_no_app_icon_title()
    {
        _frames.Frame(2);

        Assert.True(_frames.Drew("File"));
        Assert.Throws<InvalidOperationException>(() => _frames.RectOf(Icon));
    }
}

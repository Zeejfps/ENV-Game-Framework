using ZGF.Gui.Hosting;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SystemMenuTests
{
    private readonly UiFrames _frames;
    private readonly TestSystemMenuBar _bar = new();
    private readonly List<string> _picks = new();
    private bool _dark;
    private bool _canSave;
    private string _openLabel = "Open";

    public SystemMenuTests() => _frames = new UiFrames(new DefaultUiState(), body: Body).UseSystemMenuBar(_bar);

    private void Body(Ui ui)
    {
        using (ui.MenuBar("bar"))
        {
            using (var file = ui.Menu("File", id: "file"))
            {
                if (file.IsOpen)
                {
                    if (ui.MenuItem(_openLabel, "Ctrl+O", id: "open")) _picks.Add("Open");
                    ui.MenuSeparator();
                    if (ui.MenuItem("Save", enabled: _canSave)) _picks.Add("Save");
                    using (var recent = ui.SubMenu("Recent"))
                    {
                        if (recent.IsOpen && ui.MenuItem("a.dll")) _picks.Add("a.dll");
                    }
                }
            }
            using (var view = ui.Menu("View"))
            {
                if (view.IsOpen)
                    ui.MenuItem("Dark", ref _dark);
            }
            using (ui.Menu("Window", role: AppMenuRole.Window))
                ui.SystemMenuItem("Minimize", AppMenuSystemAction.Minimize, "Cmd+M");
        }
    }

    [Fact]
    public void The_menus_go_to_the_system_bar_and_nothing_is_drawn()
    {
        _frames.Frame();

        Assert.False(_frames.Drew("File"));
        Assert.Equal(["File", "View", "Window"], _bar.Menus.Select(m => m.Title));
        Assert.Equal(AppMenuRole.Window, _bar.Menu("Window").Role);
        Assert.Equal("Ctrl+O", _bar.Item("File", "Open").Shortcut);
        Assert.IsType<AppMenuSeparator>(_bar.Menu("File").Entries[1]);
        Assert.False(_bar.Item("File", "Save").IsEnabled);
        Assert.Equal("a.dll", _bar.Item("File", "Recent", "a.dll").Title);
        Assert.Equal(AppMenuSystemAction.Minimize, _bar.Item("Window", "Minimize").SystemAction);
    }

    [Fact]
    public void A_picked_item_is_told_on_the_next_frame_only()
    {
        _frames.Frame();

        _bar.Pick("File", "Recent", "a.dll");
        _frames.Frame().Frame();

        Assert.Equal(["a.dll"], _picks);
    }

    [Fact]
    public void A_disabled_item_is_not_picked()
    {
        _frames.Frame();

        _bar.Pick("File", "Save");
        _frames.Frame();

        Assert.Empty(_picks);
    }

    [Fact]
    public void Enabling_and_checking_change_the_items_in_place_without_showing_the_menus_again()
    {
        _frames.Frame();

        _bar.Pick("View", "Dark");
        _canSave = true;
        _frames.Frame().Frame();

        Assert.True(_dark);
        Assert.True(_bar.Item("View", "Dark").IsChecked);
        Assert.True(_bar.Item("File", "Save").IsEnabled);
        Assert.Equal(1, _bar.Showings);
    }

    [Fact]
    public void Renaming_an_item_shows_the_menus_again()
    {
        _frames.Frame();

        _openLabel = "Öffnen";
        _frames.Frame();

        Assert.Equal(2, _bar.Showings);
        Assert.Equal("Ctrl+O", _bar.Item("File", "Öffnen").Shortcut);
    }

    [Fact]
    public void Separators_at_the_ends_or_in_a_row_are_left_out()
    {
        var bar = new TestSystemMenuBar();
        var frames = new UiFrames(new DefaultUiState()).UseSystemMenuBar(bar);

        frames.Run(0f, ui =>
        {
            using (ui.MenuBar("bar"))
            using (ui.Menu("Edit"))
            {
                ui.MenuSeparator();
                ui.MenuItem("Copy");
                ui.MenuSeparator();
                ui.MenuSeparator();
                ui.MenuItem("Paste");
                ui.MenuSeparator();
            }
        });

        Assert.Equal([typeof(AppMenuItem), typeof(AppMenuSeparator), typeof(AppMenuItem)], bar.Menu("Edit").Entries.Select(e => e.GetType()));
    }

    [Fact]
    public void Items_the_system_acts_on_are_not_drawn_in_a_window_menu()
    {
        var frames = new UiFrames(new DefaultUiState());

        frames.Run(0f, ui =>
        {
            using (ui.MenuBar("bar"))
            using (var window = ui.Menu("Window"))
            {
                if (window.IsOpen)
                    ui.SystemMenuItem("Minimize", AppMenuSystemAction.Minimize);
            }
        });

        Assert.True(frames.Drew("Window"));
        Assert.False(frames.Drew("Minimize"));
    }
}

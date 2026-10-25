using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TitleBarTests
{
    private const uint Background = 0xFF202428;
    private static readonly UiRect Buttons = new(262f, 0f, 138f, 30f);

    private static void TitleBarWithMenus(Ui ui)
    {
        using (ui.TitleBar("bar", new Box { Background = Background, Gap = 0f }))
        {
            ui.Leaf(ui.GetId("menus"), new Box { Width = 100f, Height = Sizing.Grow() });
            ui.TitleBarDragArea("drag");
        }
    }

    private static void TitleBarWithIcon(Ui ui)
    {
        using (ui.TitleBar("bar", new Box { Background = Background, Gap = 0f }))
        {
            ui.TitleBarIcon("icon");
            ui.TitleBarDragArea("drag");
        }
    }

    private static WindowIcon IconOfSizes(params int[] sizes)
    {
        var images = sizes
            .Select(size => new WindowIconImage(size, size, new byte[size * size * 4]))
            .ToList();
        return new WindowIcon(images);
    }

    private static UiFrames InTitleBar(Action<Ui> body)
    {
        return new UiFrames(new DefaultUiState(), width: 400f, body: body)
        {
            CanDrawTitleBar = true,
            CaptionButtons = Buttons,
        };
    }

    [Fact]
    public void A_title_bar_is_as_tall_as_the_caption_buttons_and_leaves_their_room_free()
    {
        var frames = InTitleBar(TitleBarWithMenus);

        frames.Frame();

        var bar = frames.RectOf("bar");
        Assert.Equal(new UiRect(0f, 0f, 400f, 30f), bar);
        Assert.Equal(Buttons.X, frames.RectOf("drag").Right);
    }

    [Fact]
    public void A_drag_area_takes_the_room_between_what_comes_before_it_and_the_caption_buttons()
    {
        var frames = InTitleBar(TitleBarWithMenus);

        frames.Frame(2);

        var area = Assert.Single(frames.TitleBarDragAreas);
        Assert.Equal(new UiRect(100f, 0f, Buttons.X - 100f, 30f), area);
    }

    [Fact]
    public void A_frame_with_a_title_bar_reports_its_color_and_one_without_reports_none()
    {
        var recordsTitleBar = true;
        var frames = InTitleBar(ui =>
        {
            if (recordsTitleBar)
                TitleBarWithMenus(ui);
        });

        frames.Frame();
        Assert.Equal(Background, frames.TitleBarBackground);

        recordsTitleBar = false;
        frames.Frame();
        Assert.Null(frames.TitleBarBackground);
        Assert.Empty(frames.TitleBarDragAreas);
    }

    [Fact]
    public void Until_the_caption_buttons_are_known_a_title_bar_is_as_tall_as_a_menu_bar_and_spans_the_window()
    {
        var frames = InTitleBar(TitleBarWithMenus);
        frames.CaptionButtons = null;

        frames.Frame();

        var bar = frames.RectOf("bar");
        Assert.Equal(new DefaultUiState().Theme.MenuBarHeight, bar.Height);
        Assert.Equal(400f, frames.RectOf("drag").Right);
    }

    [Fact]
    public void The_window_icon_is_drawn_at_the_theme_icon_size_centered_in_the_title_bar()
    {
        var frames = InTitleBar(TitleBarWithIcon);
        frames.WindowIcon = IconOfSizes(16, 32);

        frames.Frame(2);

        var theme = new DefaultUiState().Theme;
        var side = theme.IconSize + 2 * theme.GapTight;
        Assert.Equal(new UiRect(0f, (Buttons.Height - side) / 2f, side, side), frames.RectOf("icon"));
        Assert.Equal(16, frames.Canvas.GetImageWidth("window-icon"));
    }

    [Fact]
    public void Pressing_the_window_icon_reports_it_for_the_app_to_open_its_window_menu()
    {
        var pressed = false;
        var frames = InTitleBar(ui =>
        {
            using (ui.TitleBar("bar"))
                pressed |= ui.TitleBarIcon("icon");
        });
        frames.WindowIcon = IconOfSizes(16);
        frames.Frame(2);

        frames.ClickOn("icon");

        Assert.True(pressed);
    }

    [Fact]
    public void Without_a_window_icon_the_title_bar_has_no_icon()
    {
        var frames = InTitleBar(TitleBarWithIcon);

        frames.Frame(2);

        Assert.Equal(0f, Assert.Single(frames.TitleBarDragAreas).X);
    }

    [Theory]
    [InlineData(16, 16)]
    [InlineData(20, 32)]
    [InlineData(32, 32)]
    [InlineData(300, 256)]
    public void A_window_icon_picks_the_smallest_image_that_covers_the_size_or_else_its_largest(int pixels, int picked)
    {
        var icon = IconOfSizes(256, 16, 32, 48);

        Assert.Equal(picked, icon.PickImage(pixels)!.Value.Width);
    }
}

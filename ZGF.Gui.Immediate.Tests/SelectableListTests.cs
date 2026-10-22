using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SelectableListTests
{
    private static readonly UiTheme Theme = UiTheme.Dark;

    private sealed class List
    {
        public readonly UiFrames Frames;
        public readonly ListSelection Selection = new();
        public int Count;
        public bool SelectOnClick;
        public bool Activated;
        public bool Menu;
        public PointF MenuAt;
        public bool Focused;
        public int Clicks;
        public int First;

        public List(int count = 100)
        {
            Count = count;
            Frames = new UiFrames(body: Body) { FrameTime = 1f / 60f };
        }

        private void Body(Ui ui)
        {
            using var screen = ui.Screen();
            ui.Button("other", id: "other");
            using var list = ui.SelectableList(ui.GetId("list"), Count, Selection, selectOnClick: SelectOnClick);
            for (var i = list.First; i < list.End; i++)
            {
                using (var row = list.Row(i, ui.GetId(i)))
                {
                    if (row.Interaction.Clicked) Clicks++;
                    ui.Text($"row {i}");
                }
            }
            Activated |= list.Activated;
            Menu |= list.ContextMenuRequested;
            if (list.ContextMenuRequested)
                MenuAt = list.ContextMenuAt;
            Focused = list.Focused;
            First = list.First;
        }

        public UiRect RowRect(int index) => Frames.RectOf(ui => ui.GetId(index));

        public UiRect ListRect => Frames.RectOf("list");

        public void Click(int index, PointerButton button = PointerButton.Left) => Frames.ClickOn(ui => ui.GetId(index), button);

        public void Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => Frames.Key(key, modifiers).Frame();

        public int VisibleRows => (int)(ListRect.Height / Theme.RowHeight);

        public bool Shows(int index)
        {
            Frames.Frame();
            var rect = RowRect(index);
            var list = ListRect;
            return rect.Y >= list.Y - 0.01f && rect.Bottom <= list.Bottom + 0.01f;
        }

        public uint BackgroundOf(int index)
        {
            var row = RowRect(index);
            foreach (var rect in Frames.Canvas.Rects)
            {
                if (SameBox(Frames.ToUi(rect.Inputs.Position), row))
                    return rect.Inputs.Style.BackgroundColor;
            }
            return 0u;
        }

        private static bool SameBox(UiRect a, UiRect b) => Near(a.Y, b.Y) && Near(a.Width, b.Width) && Near(a.Height, b.Height);

        private static bool Near(float a, float b) => MathF.Abs(a - b) < 0.5f;
    }

    [Fact]
    public void Pressing_a_row_selects_it_and_focuses_the_list()
    {
        var list = new List();
        list.Frames.Frame();

        list.Click(3);

        Assert.Equal(3, list.Selection.Index);
        Assert.True(list.Focused);
    }

    [Fact]
    public void The_selected_row_is_drawn_in_the_selection_color_while_the_list_is_focused_and_inactive_otherwise()
    {
        var list = new List();
        list.Frames.Frame();
        list.Click(2);

        Assert.Equal(Theme.Selection, list.BackgroundOf(2));
        Assert.Equal(0u, list.BackgroundOf(3));

        list.Frames.ClickOn("other").Frame();

        Assert.False(list.Focused);
        Assert.Equal(Theme.SelectionInactive, list.BackgroundOf(2));
    }

    [Fact]
    public void A_selection_made_while_unfocused_shows_inactive()
    {
        var list = new List();
        list.Selection.Select(1, list.Count);

        list.Frames.Frame(2);

        Assert.Equal(Theme.SelectionInactive, list.BackgroundOf(1));
    }

    [Fact]
    public void Keys_move_the_selection_only_while_the_list_has_focus()
    {
        var list = new List();
        list.Frames.Frame();

        list.Key(KeyboardKey.DownArrow);
        Assert.Equal(-1, list.Selection.Index);

        list.Click(0);
        list.Key(KeyboardKey.DownArrow);
        list.Key(KeyboardKey.DownArrow);
        Assert.Equal(2, list.Selection.Index);
        list.Key(KeyboardKey.UpArrow);
        Assert.Equal(1, list.Selection.Index);
        list.Key(KeyboardKey.End);
        Assert.Equal(99, list.Selection.Index);
        list.Key(KeyboardKey.Home);
        Assert.Equal(0, list.Selection.Index);
        list.Key(KeyboardKey.End, KeyModifiers.Control);
        Assert.Equal(99, list.Selection.Index);
        list.Key(KeyboardKey.DownArrow, KeyModifiers.Control);
        Assert.Equal(99, list.Selection.Index);
    }

    [Fact]
    public void Paging_moves_one_fewer_rows_than_show_and_stops_at_the_ends()
    {
        var list = new List();
        list.Frames.Frame();
        list.Click(0);
        var page = list.VisibleRows - 1;

        list.Key(KeyboardKey.PageDown);
        Assert.Equal(page, list.Selection.Index);
        list.Key(KeyboardKey.PageDown);
        Assert.Equal(page * 2, list.Selection.Index);
        list.Key(KeyboardKey.PageUp);
        list.Key(KeyboardKey.PageUp);
        list.Key(KeyboardKey.PageUp);
        Assert.Equal(0, list.Selection.Index);
    }

    [Fact]
    public void Moving_the_selection_with_keys_scrolls_it_into_view()
    {
        var list = new List();
        list.Frames.Frame();
        list.Click(0);

        list.Key(KeyboardKey.End);

        Assert.True(list.Shows(99));
        list.Key(KeyboardKey.Home);
        Assert.True(list.Shows(0));
    }

    [Fact]
    public void A_selection_made_before_the_list_is_ever_laid_out_scrolls_into_view_once_it_is()
    {
        var list = new List();
        list.Selection.Select(80, list.Count);

        list.Frames.Frame(3);

        Assert.True(list.Shows(80));
        Assert.False(list.Selection.TakeReveal());
    }

    [Fact]
    public void A_selection_while_the_list_is_not_recorded_waits_for_it()
    {
        var list = new List();
        list.Frames.Frame(2);
        list.Selection.Select(60, list.Count);
        list.Frames.Run(1f / 60f, ui => { using var screen = ui.Screen(); });

        list.Frames.Frame(2);

        Assert.True(list.Shows(60));
    }

    [Fact]
    public void Selecting_without_revealing_leaves_the_scroll_alone()
    {
        var list = new List();
        list.Frames.Frame(2);

        list.Selection.Select(70, list.Count, reveal: false);
        list.Frames.Frame(2);

        Assert.Equal(0, list.First);
    }

    [Fact]
    public void Enter_and_a_double_click_activate_the_selection()
    {
        var list = new List();
        list.Frames.Frame();
        list.Click(1);
        Assert.False(list.Activated);

        list.Key(KeyboardKey.Enter);
        Assert.True(list.Activated);

        list.Activated = false;
        list.Frames.Press().Frame();
        list.Frames.Release().Frame();
        Assert.True(list.Activated);
        Assert.Equal(1, list.Selection.Index);
    }

    [Fact]
    public void A_right_click_selects_the_row_and_asks_for_a_menu_at_the_pointer()
    {
        var list = new List();
        list.Frames.Frame();
        var row = list.RowRect(4);

        list.Click(4, PointerButton.Right);

        Assert.Equal(4, list.Selection.Index);
        Assert.True(list.Menu);
        Assert.Equal(row.X + row.Width / 2, list.MenuAt.X, 0.01f);
        list.Frames.Frame();
        Assert.True(list.Focused);
    }

    [Fact]
    public void The_menu_key_asks_for_a_menu_under_the_selected_row()
    {
        var list = new List();
        list.Frames.Frame();
        list.Click(2);
        var row = list.RowRect(2);

        list.Key(KeyboardKey.Menu);

        Assert.True(list.Menu);
        Assert.Equal(row.Bottom, list.MenuAt.Y, 0.01f);
    }

    [Fact]
    public void A_list_that_selects_on_click_leaves_the_selection_until_the_release()
    {
        var list = new List { SelectOnClick = true };
        list.Frames.Frame();
        var row = list.RowRect(5);

        list.Frames.MovePointer(row.X + 4f, row.Y + 4f).Frame();
        list.Frames.Press().Frame();
        Assert.Equal(-1, list.Selection.Index);
        list.Frames.Frame();
        Assert.True(list.Focused);

        list.Frames.Release().Frame();
        Assert.Equal(5, list.Selection.Index);
        Assert.Equal(1, list.Clicks);
    }

    [Fact]
    public void A_selection_past_the_last_row_is_cleared()
    {
        var list = new List();
        list.Selection.Select(50, list.Count);
        list.Count = 10;

        list.Frames.Frame();

        Assert.Equal(-1, list.Selection.Index);
    }

    [Fact]
    public void The_list_is_a_tab_stop()
    {
        var list = new List();
        list.Frames.Frame();

        list.Key(KeyboardKey.Tab);
        list.Key(KeyboardKey.Tab);
        list.Frames.Frame();

        Assert.True(list.Focused);
    }

    [Fact]
    public void Rows_allocate_no_more_than_plain_virtual_rows()
    {
        Assert.Equal(AllocatedPerRun(PlainRows), AllocatedPerRun(SelectableRows));

        static void PlainRows(Ui ui)
        {
            using var list = ui.VirtualScroll(ui.GetId("plain"), 1000, Theme.RowHeight);
            for (var i = list.First; i < list.End; i++)
            {
                using (ui.Row(ui.GetId(i), new Box { Height = Theme.RowHeight, Padding = new Spacing(Theme.ShellPaddingX, 0f), Background = i == 3 ? Theme.SelectionInactive : 0u }))
                    ui.Text("row");
            }
        }
    }

    private static readonly ListSelection Shared = new();

    private static void SelectableRows(Ui ui)
    {
        using var list = ui.SelectableList(ui.GetId("selectable"), 1000, Shared);
        for (var i = list.First; i < list.End; i++)
        {
            using (list.Row(i, ui.GetId(i)))
                ui.Text("row");
        }
    }

    private static long AllocatedPerRun(Action<Ui> body)
    {
        var frames = new UiFrames();
        Shared.Select(3, 1000);
        for (var i = 0; i < 4; i++)
            frames.Run(0f, body);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
            frames.Run(0f, body);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}

public sealed class ListKeyTests
{
    private static int After(KeyboardKey key, int index, int count = 50, int page = 10, KeyModifiers modifiers = KeyModifiers.None)
    {
        UiWidgets.ApplyListKey(new KeyStroke(key, false, modifiers), ref index, count, page);
        return index;
    }

    [Fact]
    public void With_nothing_selected_up_and_down_start_at_the_top()
    {
        Assert.Equal(0, After(KeyboardKey.DownArrow, -1));
        Assert.Equal(0, After(KeyboardKey.UpArrow, -1));
        Assert.Equal(10, After(KeyboardKey.PageDown, -1));
        Assert.Equal(0, After(KeyboardKey.PageUp, -1));
    }

    [Fact]
    public void Moves_stay_within_the_rows()
    {
        Assert.Equal(0, After(KeyboardKey.UpArrow, 0));
        Assert.Equal(49, After(KeyboardKey.DownArrow, 49));
        Assert.Equal(49, After(KeyboardKey.PageDown, 45));
        Assert.Equal(0, After(KeyboardKey.PageUp, 5));
        Assert.Equal(49, After(KeyboardKey.End, 3));
        Assert.Equal(0, After(KeyboardKey.Home, 30));
        Assert.Equal(9, After(KeyboardKey.UpArrow, 30, count: 10));
    }

    [Fact]
    public void Only_home_and_end_take_ctrl_and_nothing_takes_alt()
    {
        Assert.Equal(49, After(KeyboardKey.End, 3, modifiers: KeyModifiers.Control));
        Assert.Equal(3, After(KeyboardKey.DownArrow, 3, modifiers: KeyModifiers.Control));
        Assert.Equal(3, After(KeyboardKey.Home, 3, modifiers: KeyModifiers.Alt));
        Assert.Equal(4, After(KeyboardKey.DownArrow, 3, modifiers: KeyModifiers.Shift));
    }

    [Fact]
    public void Keys_that_are_not_moves_are_left_for_the_caller()
    {
        var index = 3;
        Assert.False(UiWidgets.ApplyListKey(new KeyStroke(KeyboardKey.Enter, false, KeyModifiers.None), ref index, 10, 5));
        Assert.False(UiWidgets.ApplyListKey(new KeyStroke(KeyboardKey.DownArrow, false, KeyModifiers.None), ref index, 0, 5));
        Assert.True(UiWidgets.ApplyListKey(new KeyStroke(KeyboardKey.UpArrow, false, KeyModifiers.None), ref index, 10, 5));
        Assert.Equal(2, index);
    }

    [Fact]
    public void A_row_is_drawn_selected_in_a_focused_list_inactive_in_another_and_hovered_otherwise()
    {
        var theme = UiTheme.Dark;
        Assert.Equal(theme.Selection, theme.PickRowBackground(true, true, false));
        Assert.Equal(theme.SelectionInactive, theme.PickRowBackground(true, false, true));
        Assert.Equal(theme.RowHover, theme.PickRowBackground(false, true, true));
        Assert.Equal(0u, theme.PickRowBackground(false, false, false));
    }

    [Fact]
    public void A_selection_move_with_nothing_selected_starts_at_the_first_row_going_back()
    {
        var selection = new ListSelection();

        selection.Move(-3, 10);
        Assert.Equal(0, selection.Index);
        selection.Move(4, 10);
        Assert.Equal(4, selection.Index);
        selection.Move(100, 10);
        Assert.Equal(9, selection.Index);
        selection.Select(10, 10);
        Assert.False(selection.HasSelection);
    }

    [Fact]
    public void Selecting_quietly_keeps_a_reveal_already_asked_for()
    {
        var selection = new ListSelection();

        selection.Select(2, 10);
        selection.Select(5, 10, reveal: false);

        Assert.True(selection.TakeReveal());
        Assert.False(selection.TakeReveal());
    }
}

using ZGF.Geometry;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class TableTests
{
    private const float Padding = 10f;

    private readonly UiFrames _frames;
    private readonly TableColumns _columns = new(new TableColumn(Fixed: 20f), new TableColumn(1f, 40f), new TableColumn(2f, 40f));

    public TableTests() => _frames = new UiFrames(body: Body);

    private void Body(Ui ui)
    {
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = 0f }))
        {
            ui.TableHeader("header", _columns, ui.GetId("body"), ["", "Name", "At"], Padding);
            using (ui.Scroll("body", new Box { Gap = 0f }))
            {
                using (ui.Row(new Box { Height = 24f, Gap = 0f, Padding = new Spacing(Padding, 0f) }))
                {
                    using (ui.TableCell(_columns, 0)) ui.Text("*");
                    using (ui.TableCell(_columns, 1)) ui.Text("first");
                    using (ui.TableCell(_columns, 2)) ui.Text("here");
                }
            }
        }
    }

    private float LeftOf(string text) => _frames.TextRect(text).X;

    private static int BorderOf(Ui ui, int column) => UiWidgets.TableBorderId(ui.GetId("header"), column);

    [Fact]
    public void Fixed_columns_keep_their_width_the_rest_share_by_weight_and_headers_line_up_with_cells()
    {
        _frames.Frame(2);

        var flexible = _columns.Width(1) + _columns.Width(2);

        Assert.Equal(20f, _columns.Width(0));
        Assert.Equal(2f, _columns.Width(2) / _columns.Width(1), 1);
        Assert.InRange(flexible, 400f - 2 * Padding - 20f - 2f, 400f - 2 * Padding - 20f);
        Assert.Equal(LeftOf("Name"), LeftOf("first"));
        Assert.Equal(LeftOf("At"), LeftOf("here"));
    }

    [Fact]
    public void Dragging_a_border_moves_width_between_its_neighbors_down_to_their_minimum()
    {
        _frames.Frame(2);
        var border = _frames.RectOf(ui => BorderOf(ui, 1));
        var pair = _columns.Width(1) + _columns.Width(2);
        var x = border.X + border.Width / 2;
        var y = border.Y + border.Height / 2;

        _frames.Drag(new PointF(x, y), new PointF(x + 30f, y));
        var widened = _columns.Width(1);
        _frames.MovePointer(x + 1000f, y).Frame();
        _frames.Release().Frame(2);

        Assert.Equal(pair, _columns.Width(1) + _columns.Width(2), 0);
        Assert.Equal(40f, _columns.Width(2), 0);
        Assert.True(widened > pair / 3 + 25f, $"{widened}");
        Assert.Equal(LeftOf("At"), LeftOf("here"));
    }

    [Fact]
    public void Sized_columns_keep_their_widths_scroll_with_the_header_and_resize_alone()
    {
        var columns = TableColumns.Sized([300f, 250f, 200f]);
        var scroll = 0f;
        var clicked = -1;
        _frames.Body = ui =>
        {
            using (ui.Column(new Box { Width = Sizing.Grow(), Gap = 0f }))
            {
                var at = ui.TableHeader("header", columns, ui.GetId("body"), ["A", "B", "C"], Padding, scroll, new TableSort(1));
                if (at >= 0) clicked = at;
                using (ui.Scroll("body", new Box { Gap = 0f }))
                {
                    using (ui.Row(new Box { Width = columns.TotalWidth + Padding * 2, Height = 24f, Gap = 0f, Padding = new Spacing(Padding, 0f), ScrollX = scroll }))
                    {
                        using (ui.TableCell(columns, 0)) ui.Text("a");
                        using (ui.TableCell(columns, 1)) ui.Text("b");
                        using (ui.TableCell(columns, 2)) ui.Text("c");
                    }
                }
            }
        };
        _frames.Frame(2);
        Assert.Equal(750f, columns.TotalWidth);
        var unscrolled = LeftOf("B");

        scroll = 200f;
        _frames.Frame();
        Assert.Equal(unscrolled - 200f, LeftOf("B"), 0);
        Assert.Equal(LeftOf("b"), LeftOf("B"));
        Assert.Equal(LeftOf("c"), LeftOf("C"));

        var border = _frames.RectOf(ui => BorderOf(ui, 0));
        var x = border.X + border.Width / 2;
        var y = border.Y + border.Height / 2;
        _frames.Drag(new PointF(x, y), new PointF(x + 40f, y)).Release().Frame();

        Assert.Equal(340f, columns.Width(0), 0);
        Assert.Equal(250f, columns.Width(1));
        Assert.Equal([340f, 250f, 200f], columns.Weights.Select(w => MathF.Round(w)));

        var heading = _frames.TextRect("B");
        _frames.Click(heading.X + 1f, heading.Y + heading.Height / 2);
        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Saved_weights_are_restored_and_mismatched_ones_ignored()
    {
        _columns.Restore([1f, 3f, 1f]);
        _frames.Frame(2);
        var restored = _columns.Width(1) / _columns.Width(2);

        _columns.Restore([1f, 1f]);
        _columns.Restore([1f, -1f, 1f]);
        _frames.Frame();

        Assert.Equal(3f, restored, 1);
        Assert.Equal(3f, _columns.Width(1) / _columns.Width(2), 1);
    }
}

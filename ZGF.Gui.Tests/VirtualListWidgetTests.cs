using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// End-to-end coverage of the declarative <see cref="VirtualList{TRow}"/>: rows are real widgets whose
/// own controllers handle hover/click (no hit-testing in the list), only a bounded window is
/// materialized, and the wheel scrolls through the real dispatch path. Driven headlessly through
/// <see cref="GuiTestHarness"/>.
/// </summary>
public class VirtualListWidgetTests
{
    // A row that fills itself with one button. The button owns hover/click; the row records the
    // current bound index, so a recycled row clicks through to its present identity.
    private sealed class TestRow : View
    {
        public readonly RectView Button = new();
        public int BoundIndex = -1;
        public bool ButtonHovered;
        public Action<int>? Clicked;

        public TestRow(InputSystem input)
        {
            AddChildToSelf(Button);
            Button.UseController(input, new KbmHandlers
            {
                OnHoverEnter = () => ButtonHovered = true,
                OnHoverExit = () => ButtonHovered = false,
                OnClick = () => Clicked?.Invoke(BoundIndex),
            });
        }

        protected override void OnLayoutChildren()
        {
            var p = Position;
            Button.LeftConstraint = p.Left;
            Button.BottomConstraint = p.Bottom;
            Button.WidthConstraint = p.Width;
            Button.HeightConstraint = p.Height;
            Button.LayoutSelf();
        }
    }

    private static GuiTestHarness BuildList(
        List<TestRow> created, List<int> clicked, int itemCount = 1000, float rowHeight = 25f)
    {
        return GuiTestHarness.Create(ctx =>
        {
            var input = ctx.Require<InputSystem>();
            return new VirtualList<TestRow>
            {
                ItemCount = itemCount,
                RowHeight = rowHeight,
                CreateRow = () =>
                {
                    var row = new TestRow(input) { Clicked = clicked.Add };
                    created.Add(row);
                    return row;
                },
                BindRow = (row, index) => row.BoundIndex = index,
            }.BuildView(ctx);
        }, width: 300, height: 200);
    }

    private static TestRow VisibleRowAt(List<TestRow> created, int index) =>
        created.Single(r => r.IsVisible && r.BoundIndex == index);

    [Fact]
    public void OnlyAViewportOfRowsIsMaterialized()
    {
        var created = new List<TestRow>();
        using var h = BuildList(created, new List<int>(), itemCount: 1000);

        // 200px / 25px ≈ 8 rows on screen; a small overscan pool — nowhere near 1000.
        Assert.InRange(created.Count, 8, 16);
    }

    [Fact]
    public void HoveringARowButton_HighlightsItForFree()
    {
        var created = new List<TestRow>();
        using var h = BuildList(created, new List<int>());

        var row = VisibleRowAt(created, 0);
        var c = row.Button.Position.Center;
        h.MoveTo(c.X, c.Y);

        Assert.True(row.ButtonHovered);
        Assert.All(created.Where(r => r != row), other => Assert.False(other.ButtonHovered));
    }

    [Fact]
    public void ClickingARowButton_FiresWithItsBoundIndex()
    {
        var created = new List<TestRow>();
        var clicked = new List<int>();
        using var h = BuildList(created, clicked);

        var row = VisibleRowAt(created, 3);
        var c = row.Button.Position.Center;
        h.Click(c.X, c.Y);

        Assert.Equal(new List<int> { 3 }, clicked);
    }

    [Fact]
    public void WheelScroll_RecyclesRowsToLaterIndices()
    {
        var created = new List<TestRow>();
        using var h = BuildList(created, new List<int>());

        // Hover inside the list so the wheel routes to it, then scroll down.
        var top = VisibleRowAt(created, 0);
        var c = top.Button.Position.Center;
        h.MoveTo(c.X, c.Y);
        h.Scroll(0f, -10f);
        h.Layout();

        var visible = created.Where(r => r.IsVisible).Select(r => r.BoundIndex).ToList();
        Assert.DoesNotContain(0, visible);
        Assert.Contains(visible, i => i >= 20);
        // Recycled, not re-created: the pool stayed bounded across the scroll.
        Assert.InRange(created.Count, 8, 18);
    }
}

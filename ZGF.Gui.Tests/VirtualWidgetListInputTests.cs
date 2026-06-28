using ZGF.Gui.Desktop.Components.VirtualWidgetList;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// Interaction routing for <see cref="VirtualWidgetListController{TRow}"/>: clicks become row events, a
/// double-tap activates, right-click requests context — and crucially, an interactive child widget inside
/// a row consumes its own click so the list's row-click handler does <b>not</b> also fire. This is what
/// lets the ledger nav button live inside a recycled row without triggering row selection. Driven through
/// the real dispatch path with <see cref="GuiTestHarness"/>.
/// </summary>
public class VirtualWidgetListInputTests
{
    // A row whose right half is a button (its own controller, consumes clicks); the left half is plain.
    private sealed class TestRow : View
    {
        public readonly RectView Button = new();
        public int Bound = -1;
        public bool ButtonClicked;

        public TestRow(InputSystem input)
        {
            AddChildToSelf(Button);
            Button.UseController(input, new KbmHandlers { OnClick = () => ButtonClicked = true });
        }

        protected override void OnLayoutChildren()
        {
            var p = Position;
            Button.LeftConstraint = p.Left + p.Width * 0.5f;
            Button.BottomConstraint = p.Bottom;
            Button.WidthConstraint = p.Width * 0.5f;
            Button.HeightConstraint = p.Height;
            Button.LayoutSelf();
        }
    }

    private sealed class Harness
    {
        public readonly List<int> Clicks = new();
        public readonly List<int> Activations = new();
        public readonly List<int> Contexts = new();
        public readonly List<TestRow> Created = new();
        public required GuiTestHarness H;

        public TestRow Row(int index) => Created.Single(r => r.IsVisible && r.Bound == index);
    }

    private static Harness Build()
    {
        var state = new Harness { H = null! };
        state.H = GuiTestHarness.Create(ctx =>
        {
            var input = ctx.Require<InputSystem>();
            var list = new VirtualWidgetListView<TestRow>
            {
                ItemCount = 100,
                RowHeight = 25f,
                CreateRow = () =>
                {
                    var r = new TestRow(input);
                    state.Created.Add(r);
                    return r;
                },
                BindRow = (r, i) => r.Bound = i,
            };
            list.RowClicked += (i, _, _) => state.Clicks.Add(i);
            list.RowActivated += i => state.Activations.Add(i);
            list.RowContextRequested += (i, _) => state.Contexts.Add(i);
            list.UseController(input, () => new VirtualWidgetListController<TestRow>(list));
            return list;
        }, width: 300, height: 200);
        return state;
    }

    [Fact]
    public void ClickingARowBody_FiresRowClicked()
    {
        var s = Build();
        var c = s.Row(0).Position.Center; // center is the left (plain) half? center.X = 150 = button edge
        s.H.Click(75f, c.Y);              // left half: plain row body

        Assert.Equal(new List<int> { 0 }, s.Clicks);
        Assert.False(s.Row(0).ButtonClicked);
    }

    [Fact]
    public void ClickingAChildButton_ConsumesIt_NoRowClick()
    {
        var s = Build();
        var y = s.Row(0).Position.Center.Y;
        s.H.Click(225f, y); // right half: the button

        Assert.True(s.Row(0).ButtonClicked);
        Assert.Empty(s.Clicks); // the list's row-click handler must not fire
    }

    [Fact]
    public void DoubleClickingARowBody_Activates()
    {
        var s = Build();
        var y = s.Row(2).Position.Center.Y;
        s.H.Click(75f, y);
        s.H.Click(75f, y);

        Assert.Contains(2, s.Activations);
    }

    [Fact]
    public void RightClickingARow_RequestsContext()
    {
        var s = Build();
        var y = s.Row(1).Position.Center.Y;
        s.H.Click(75f, y, MouseButton.Right);

        Assert.Equal(new List<int> { 1 }, s.Contexts);
    }
}

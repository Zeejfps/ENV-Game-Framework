using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.Gui.Views;

namespace ZGF.Gui.Tests;

/// <summary>
/// Hit-testing ranks views by the z they actually composite at (<see cref="View.DrawZIndex"/> — the view's own
/// <see cref="View.ZIndex"/> plus every ancestor's), so a click lands on whatever is painted on top.
///
/// The case that matters is a popover: the lift is set on the popover's ROOT, while the interactive widgets
/// inside it (a calendar's day cells, a dropdown's rows) carry a local ZIndex of 0. Comparing only the local
/// value scored those 0 against a plain sibling's 0 and fell through to sibling order — so a field painted
/// underneath, but added to the parent later, stole the click.
/// </summary>
public class HitTestZOrderTests
{
    private sealed class ClickSpy : KeyboardMouseController
    {
        public int Clicks { get; private set; }

        public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
        {
            if (e.Phase != EventPhase.Bubbling || e.State != InputState.Pressed) return;
            Clicks++;
            e.Consume();
        }
    }

    // A lifted popover (root ZIndex 40) whose clickable content sits at a local ZIndex of 0, overlapping a
    // sibling field that was added to the parent afterwards.
    private static (GuiTestHarness H, ClickSpy Popover, ClickSpy Field) Build()
    {
        var popoverSpy = new ClickSpy();
        var fieldSpy = new ClickSpy();

        var h = GuiTestHarness.Create(ctx =>
        {
            var input = ctx.Require<InputSystem>();

            var popoverContent = new RectView(); // a day cell: no ZIndex of its own
            popoverContent.UseController(input, popoverSpy);
            var popover = new RectView { ZIndex = 40, Children = { popoverContent } };

            var field = new RectView();
            field.UseController(input, fieldSpy);

            // The field is added LAST, so it wins any sibling-order tiebreak.
            return new StackHost(popover, popoverContent, field);
        }, width: 300, height: 200);
        h.Layout();
        return (h, popoverSpy, fieldSpy);
    }

    // Lays the popover and the field over the same rect, so a click hits both and only z decides.
    private sealed class StackHost : View
    {
        private readonly View _popover;
        private readonly View _popoverContent;
        private readonly View _field;

        public StackHost(View popover, View popoverContent, View field)
        {
            _popover = popover;
            _popoverContent = popoverContent;
            _field = field;
            AddChildToSelf(popover);
            AddChildToSelf(field); // added after the popover: higher sibling index
        }

        protected override void OnLayoutChildren()
        {
            foreach (var v in new[] { _popover, _popoverContent, _field })
            {
                v.LeftConstraint = Position.Left;
                v.BottomConstraint = Position.Bottom;
                v.WidthConstraint = Position.Width;
                v.HeightConstraint = Position.Height;
                v.LayoutSelf();
            }
        }
    }

    [Fact]
    public void ALiftedPopover_WinsTheClick_OverALaterSiblingPaintedUnderneath()
    {
        var (h, popover, field) = Build();
        using var _ = h;

        h.Click(150f, 100f);

        Assert.Equal(1, popover.Clicks);
        Assert.Equal(0, field.Clicks);
    }

    [Fact]
    public void DrawZIndex_AccumulatesThroughAncestors()
    {
        var child = new RectView { ZIndex = 3 };
        var parent = new RectView { ZIndex = 40, Children = { child } };
        var root = new RectView { ZIndex = 5, Children = { parent } };

        Assert.Equal(5, root.DrawZIndex);
        Assert.Equal(45, parent.DrawZIndex);
        Assert.Equal(48, child.DrawZIndex);
    }
}

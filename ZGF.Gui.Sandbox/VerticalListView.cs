using ZGF.Gui;
using ZGF.Gui.Desktop.Components.VerticalScrollBar;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.VerticalScrollBar;
using ZGF.Gui.Views;

namespace ZGF.Gui.Sandbox;

public sealed class VerticalListView : View
{
    public StyleValue<int> Gap
    {
        get => ScrollPaneView.Gap;
        set => ScrollPaneView.Gap = value;
    }
    
    public new ChildrenCollection Children => ScrollPaneView.Children;

    public VerticalScrollPane ScrollPaneView { get; }
    public VerticalScrollBarView ScrollBarView { get; }

    public VerticalListView(InputSystem input)
    {
        ScrollPaneView = new VerticalScrollPane();
        ScrollBarView = new VerticalScrollBarView();

        AddChildToSelf(new BorderLayoutView
        {
            Center = ScrollPaneView,
            East = ScrollBarView,
        });

        var thumb = ScrollBarView.Thumb;
        var hovered = false;
        DragRecognizer? drag = null;
        thumb.UseController(input, () => drag = new DragRecognizer(input)
        {
            DragStarted = () => thumb.IsSelected = true,
            Dragged = delta => thumb.Move(delta.Y),
            DragEnded = () =>
            {
                if (!hovered) thumb.IsSelected = false;
            },
        });
        thumb.UseController(input, new KbmHandlers
        {
            OnHoverEnter = () =>
            {
                hovered = true;
                thumb.IsSelected = true;
            },
            OnHoverExit = () =>
            {
                hovered = false;
                if (drag is not { IsDragging: true }) thumb.IsSelected = false;
            },
        });
        ScrollBarView.UseController(input, new KbmHandlers
        {
            OnMouseButton = (ref MouseButtonEvent e) =>
            {
                if (e.Phase == EventPhase.Bubbling
                    && e.Button == MouseButton.Left
                    && e.State == InputState.Pressed)
                {
                    ScrollBarView.ScrollToPoint(e.Mouse.Point);
                    e.Consume();
                }
            },
        });
    }

    public void Scroll(float delta)
    {
        ScrollPaneView.Scroll(delta);
    }

    public void ScrollUp(float delta)
    {
        ScrollPaneView.ScrollUp(delta);
    }
    
    public void ScrollDown(float delta)
    {
        ScrollPaneView.ScrollDown(delta);
    }

    public void ScrollTo(View view)
    {
        
    }

    public void ScrollToBottom()
    {
        ScrollPaneView.ScrollToBottom();
    }
    
    public void ScrollToTop()
    {
        ScrollPaneView.ScrollToTop();
    }
    
    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
        ScrollBarView.Scale = ScrollPaneView.Scale;
    }
}
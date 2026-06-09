using ZGF.Gui;
using ZGF.Gui.Desktop.Components.VerticalScrollBar;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.VerticalScrollBar;
using ZGF.Gui.Views;

namespace ZGF.Gui.Sandbox;

public sealed class VerticalListView : MultiChildView
{
    public StyleValue<int> Gap
    {
        get => ScrollPaneView.Gap;
        set => ScrollPaneView.Gap = value;
    }
    
    public override IChildrenCollection Children => ScrollPaneView.Children;

    public VerticalScrollPane ScrollPaneView { get; }
    public VerticalScrollBarView ScrollBarView { get; }

    public VerticalListView()
    {
        ScrollPaneView = new VerticalScrollPane();
        ScrollBarView = new VerticalScrollBarView();

        AddChildToSelf(new BorderLayoutView
        {
            Center = ScrollPaneView,
            East = ScrollBarView,
        });

        ScrollBarView.UseController(_ => new VerticalScrollBarViewController(ScrollBarView));
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

    public void ScrollTo(MultiChildView view)
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
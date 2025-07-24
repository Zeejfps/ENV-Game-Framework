using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class VerticalListView : View
{
    public StyleValue<int> Gap
    {
        get => ScrollPaneView.Gap;
        set => ScrollPaneView.Gap = value;
    }
    
    public override IComponentCollection Children => ScrollPaneView.Children;

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

        Controller = new DefaultVerticalListViewKbmController(this);
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
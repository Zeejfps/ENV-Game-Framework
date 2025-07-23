using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class VerticalListView : View
{
    public override IComponentCollection Children => _scrollPane.Children;

    private readonly BorderLayoutView _borderLayoutView;
    private readonly VerticalScrollPane _scrollPane;
    private readonly VerticalScrollBarView _scrollBarView;

    public VerticalListView()
    {
        _scrollPane = new VerticalScrollPane();
        _scrollBarView = new VerticalScrollBarView();

        _borderLayoutView = new BorderLayoutView
        {
            Center = _scrollPane,
            East = _scrollBarView,
        };
        
        AddChildToSelf(_borderLayoutView);
    }

    public void Scroll(float delta)
    {
        _scrollPane.Scroll(delta);
    }

    public void ScrollUp(float delta)
    {
        _scrollPane.ScrollUp(delta);
    }
    
    public void ScrollDown(float delta)
    {
        _scrollPane.ScrollDown(delta);
    }

    public void ScrollTo(View view)
    {
        
    }

    public void ScrollToBottom()
    {
        _scrollPane.ScrollToBottom();
    }
    
    public void ScrollToTop()
    {
        _scrollPane.ScrollToTop();
    }
    
    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
        _scrollBarView.Scale = _scrollPane.Scale;       
    }
}
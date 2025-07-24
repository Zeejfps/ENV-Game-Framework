using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class VerticalListView : View
{
    public StyleValue<int> Gap
    {
        get => _scrollPane.Gap;
        set => _scrollPane.Gap = value;
    }
    
    public override IComponentCollection Children => _scrollPane.Children;

    private readonly VerticalScrollPane _scrollPane;
    private readonly VerticalScrollBarView _scrollBarView;

    public VerticalListView()
    {
        _scrollPane = new VerticalScrollPane();
        _scrollBarView = new VerticalScrollBarView();

        AddChildToSelf(new BorderLayoutView
        {
            Center = _scrollPane,
            East = _scrollBarView,
        });

        Controller = new DefaultVerticalListViewKbmController(this, _scrollBarView, _scrollPane);
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
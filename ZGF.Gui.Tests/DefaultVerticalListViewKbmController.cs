using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class DefaultVerticalListViewKbmController : KeyboardMouseController
{
    public override View View => _view;
    
    private readonly VerticalListView _view;
    private readonly VerticalScrollPane _viewPortView;
    private readonly VerticalScrollBarView _scrollBarView;

    public DefaultVerticalListViewKbmController(VerticalListView view)
    {
        _view = view;
        _viewPortView = view.ScrollPaneView;
        _scrollBarView = view.ScrollBarView;
    }


    public override void OnEnabled(Context context)
    {
        base.OnEnabled(context);
        
        _viewPortView.ScrollToTop();
        _scrollBarView.ScrollToTop();

        _scrollBarView.ScrollPositionChanged += OnScrollBarScrollPositionChanged;
        _viewPortView.ScrollPositionChanged += OnScrollPaneScrollPositionChanged;
    }

    public override void OnDisabled(Context context)
    {
        base.OnDisabled(context);
        _scrollBarView.ScrollPositionChanged -= OnScrollBarScrollPositionChanged;
        _viewPortView.ScrollPositionChanged -= OnScrollPaneScrollPositionChanged;
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        _view.Scroll(e.DeltaY * -10);
        e.Consume();
    }

    private void OnScrollPaneScrollPositionChanged(float normalizedScrollPosition)
    {
        _scrollBarView.SetNormalizedScrollPosition(normalizedScrollPosition);
    }

    private void OnScrollBarScrollPositionChanged(float normalizedScrollPosition)
    {
        _viewPortView.SetNormalizedScrollPosition(normalizedScrollPosition);
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (e.State == InputState.Pressed)
        {
            if (e.Key == KeyboardKey.UpArrow)
            {
                _view.ScrollUp(10f);
            }
            else if (e.Key == KeyboardKey.DownArrow)
            {
                _view.ScrollDown(10f);
            }
            else if (e.Key == KeyboardKey.Space)
            {
                _view.ScrollToBottom();
            }
            else if (e.Key == KeyboardKey.W)
            {
                _view.ScrollToTop();
            }
        }
    }
}
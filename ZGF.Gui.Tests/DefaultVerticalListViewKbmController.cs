using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class DefaultVerticalListViewKbmController : IKeyboardMouseController
{
    public View View => _view;
    
    private readonly VerticalListView _view;
    private readonly VerticalScrollPane _viewPortView;
    private readonly VerticalScrollBarView _scrollBarView;

    public DefaultVerticalListViewKbmController(VerticalListView view)
    {
        _view = view;
        _viewPortView = view.ScrollPaneView;
        _scrollBarView = view.ScrollBarView;
    }

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        
    }

    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        _view.Scroll(e.DeltaY * -10);
        e.Consume();
    }

    public void OnMouseMoved(ref MouseMoveEvent e)
    {
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
        _viewPortView.ScrollToTop();
        _scrollBarView.ScrollToTop();

        _scrollBarView.ScrollPositionChanged += OnScrollBarScrollPositionChanged;
        _viewPortView.ScrollPositionChanged += OnScrollPaneScrollPositionChanged;
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
        _scrollBarView.ScrollPositionChanged -= OnScrollBarScrollPositionChanged;
        _viewPortView.ScrollPositionChanged -= OnScrollPaneScrollPositionChanged;
    }

    private void OnScrollPaneScrollPositionChanged(float normalizedScrollPosition)
    {
        _scrollBarView.SetNormalizedScrollPosition(normalizedScrollPosition);
    }

    private void OnScrollBarScrollPositionChanged(float normalizedScrollPosition)
    {
        _viewPortView.SetNormalizedScrollPosition(normalizedScrollPosition);
    }

    public void OnMouseEnter(ref MouseEnterEvent e)
    {
    }

    public void OnMouseExit(ref MouseExitEvent e)
    {
    }

    public void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
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

    public void OnFocusLost()
    {
    }

    public void OnFocusGained()
    {
    }
}
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class DefaultVerticalListViewKbmController : IKeyboardMouseController
{
    public View View => _view;
    
    private readonly VerticalListView _view;
    private readonly VerticalScrollPane _scrollPaneView;
    private readonly VerticalScrollBarView _scrollBarView;

    public DefaultVerticalListViewKbmController(VerticalListView view, VerticalScrollBarView scrollBarView, VerticalScrollPane scrollPaneView)
    {
        _view = view;
        _scrollBarView = scrollBarView;
        _scrollPaneView = scrollPaneView;
    }

    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        _view.Scroll(e.DeltaY * -10);
        e.Consume();
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
        _scrollPaneView.ScrollToTop();
        _scrollBarView.ScrollToTop();

        _scrollBarView.ScrollPositionChanged += OnScrollBarScrollPositionChanged;
        _scrollPaneView.ScrollPositionChanged += OnScrollPaneScrollPositionChanged;
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
        _scrollBarView.ScrollPositionChanged -= OnScrollBarScrollPositionChanged;
        _scrollPaneView.ScrollPositionChanged -= OnScrollPaneScrollPositionChanged;
    }

    private void OnScrollPaneScrollPositionChanged(float normalizedScrollPosition)
    {
        _scrollBarView.SetNormalizedScrollPosition(normalizedScrollPosition);
    }

    private void OnScrollBarScrollPositionChanged(float normalizedScrollPosition)
    {
        _scrollPaneView.SetNormalizedScrollPosition(normalizedScrollPosition);
    }


    public void OnMouseEnter(in MouseEnterEvent e)
    {
        this.RequestFocus();
    }

    public void OnMouseExit(in MouseExitEvent e)
    {
        this.Blur();
    }

    public bool OnKeyboardKeyStateChanged(in KeyboardKeyEvent e)
    {
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
        return true;
    }
}
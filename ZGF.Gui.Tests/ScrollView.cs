using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class ScrollView : View, IKeyboardMouseController
{
    private readonly VerticalScrollPane _viewPort;
    
    private View? _content;
    public View? Content
    {
        get => _content;
        set
        {
            var prevContent = _content;
            _content = value;

            if (prevContent != null)
            {
                _viewPort.Children.Remove(prevContent);
            }
            
            if (_content != null)
            {
                _viewPort.Children.Add(_content);
            }
        }
    }
    
    public ScrollView()
    {
        _viewPort = new VerticalScrollPane();
        AddChildToSelf(_viewPort);
        
        Controller = this;
    }

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
    }

    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        _viewPort.YOffset += (int)e.DeltaY * -6;
        e.Consume();
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public View View => this;
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
                _viewPort.YOffset += 10f;
            }
            else if (e.Key == KeyboardKey.DownArrow)
            {
                _viewPort.YOffset -= 10f;
            }
        }
        return true;
    }
}
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class ScrollView : Component, IKeyboardMouseController
{
    private readonly VerticalScrollPane _viewPort;
    
    private Component? _content;
    public Component? Content
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
        Add(_viewPort);
        
        Controller = this;
    }

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();
    }
    
    
    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public Component Component => this;
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
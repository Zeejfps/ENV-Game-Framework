using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class TextInputDefaultController : IKeyboardMouseController
{
    private readonly TextInput _textInput;
    
    public TextInputDefaultController(TextInput textInput)
    {
        _textInput = textInput;
    }
    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public void OnMouseEnter()
    {
        this.RequestFocus();
    }

    public void OnMouseExit()
    {
        if (!_textInput.IsEditing)
            this.Blur();
    }

    public void OnFocusLost()
    {
        _textInput.StopEditing();
    }

    public bool CanReleaseFocus()
    {
        return !_textInput.IsEditing;
    }

    public bool OnMouseMoved(InputSystem inputSystem, in MouseMoveEvent e)
    {
        var isLeftMouseButtonPressed = inputSystem.IsMouseButtonPressed(MouseButton.Left);
        if (!isLeftMouseButtonPressed)
            return false;

        _textInput.MoveCaretTo(e.MousePoint, true);
        return true;
    }

    public bool OnMouseButtonStateChanged(in MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed && e.Button == MouseButton.Left)
        {
            var isEditing = _textInput.IsEditing;
            var containsPoint = _textInput.Position.ContainsPoint(e.MousePoint);

            if (isEditing && !containsPoint)
            {
                _textInput.StopEditing();
                return false;
            }

            if (!isEditing && containsPoint)
            {
                _textInput.StartEditing();
            }
            
            var mousePoint = e.MousePoint;
            _textInput.MoveCaretTo(mousePoint);

            return true;
        }

        return false;
    }

    public bool HandleKeyboardKeyEvent(in KeyboardKeyEvent e)
    {
        if (!_textInput.IsEditing)
            return false;

        if (e.State != InputState.Pressed) 
            return false;
        
        if (e.Key == KeyboardKey.A && e.Modifiers.HasFlag(InputModifiers.Control))
        {
            _textInput.SelectAll();
            return true;
        }

        if (e.Key == KeyboardKey.C && e.Modifiers.HasFlag(InputModifiers.Control))
        {
            Copy();
            return true;
        }
            
        if (e.Key == KeyboardKey.V && e.Modifiers.HasFlag(InputModifiers.Control))
        {
            Paste();
            return true;
        }
            
        if (e.Key == KeyboardKey.X && e.Modifiers.HasFlag(InputModifiers.Control))
        {
            Cut();
            return true;
        }
            
        var isShiftPressed = (e.Modifiers & InputModifiers.Shift) > 0;
        if (e.Key == KeyboardKey.LeftArrow)
        {
            _textInput.MoveCaretLeft(isShiftPressed);
            return true;
        }
            
        if (e.Key == KeyboardKey.RightArrow)
        {
            _textInput.MoveCaretRight(isShiftPressed);
            return true;
        }
            
        if (e.Key == KeyboardKey.Backspace)
        {
            _textInput.Delete();
            return true;
        }
        

        var c = e.Key.ToChar(isShiftPressed);
        if (c == '\0')
        {
            return true;
        }

        _textInput.Enter(c);
        return true;
    }

    private void Cut()
    {
        Copy();
        _textInput.Delete();
    }

    private void Paste()
    {
        var clipboard = _textInput.Context?.Get<IClipboard>();
        var text = clipboard?.GetText();
        if (text == null) 
            return;
        
        _textInput.Enter(text);
    }

    private void Copy()
    {
        
    }

    public Component Component => _textInput;
}
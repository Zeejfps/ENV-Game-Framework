using System.Runtime.InteropServices;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class TextInputDefaultKbmController : IKeyboardMouseController
{
    private readonly TextInput _textInput;
    
    public TextInputDefaultKbmController(TextInput textInput)
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

    public void OnMouseEnter(ref MouseEnterEvent e)
    {
        
    }

    public void OnMouseExit(ref MouseExitEvent e)
    {
        
    }
    
    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
    }

    public void OnFocusLost()
    {
        
    }

    public void OnFocusGained()
    {
    }

    public bool CanReleaseFocus()
    {
        return !_textInput.IsEditing;
    }

    public void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        var isLeftMouseButtonPressed = e.Mouse.IsButtonPressed(MouseButton.Left);
        if (!isLeftMouseButtonPressed)
            return;

        if (_textInput.IsEditing)
            return;
        
        _textInput.MoveCaretTo(e.Mouse.Point, true);
    }

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (e.State == InputState.Pressed && e.Button == MouseButton.Left)
        {
            var isEditing = _textInput.IsEditing;
            var containsPoint = _textInput.Position.ContainsPoint(e.Mouse.Point);

            if (isEditing && !containsPoint)
            {
                _textInput.StopEditing();
                this.Blur();
                return;
            }

            if (!isEditing && containsPoint)
            {
                _textInput.StartEditing();
                this.RequestFocus();
            }
            
            var mousePoint = e.Mouse.Point;
            _textInput.MoveCaretTo(mousePoint);
            e.Consume();
        }
    }

    public void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (!_textInput.IsEditing)
            return;

        if (e.State != InputState.Pressed) 
            return;

        var ctrlModifier = InputModifiers.Control;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ctrlModifier = InputModifiers.Super;
        }
        
        if (e.Key == KeyboardKey.A && e.Modifiers.HasFlag(ctrlModifier))
        {
            _textInput.SelectAll();
            e.Consume();
            return;
        }

        if (e.Key == KeyboardKey.C && e.Modifiers.HasFlag(ctrlModifier))
        {
            Copy();
            e.Consume();
            return;
        }
            
        if (e.Key == KeyboardKey.V && e.Modifiers.HasFlag(ctrlModifier))
        {
            Paste();
            e.Consume();
            return;
        }
            
        if (e.Key == KeyboardKey.X && e.Modifiers.HasFlag(ctrlModifier))
        {
            Cut();
            e.Consume();
            return;
        }
            
        var isShiftPressed = (e.Modifiers & InputModifiers.Shift) > 0;
        if (e.Key == KeyboardKey.LeftArrow)
        {
            _textInput.MoveCaretLeft(isShiftPressed);
            e.Consume();
            return;
        }
            
        if (e.Key == KeyboardKey.RightArrow)
        {
            _textInput.MoveCaretRight(isShiftPressed);
            e.Consume();
            return;
        }
            
        if (e.Key == KeyboardKey.Backspace)
        {
            _textInput.Delete();
            e.Consume();
            return;
        }

        var c = e.Key.ToChar(isShiftPressed);
        if (c == '\0')
        {
            return;
        }

        _textInput.Enter(c);
        e.Consume();
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
        var selectedText = _textInput.GetSelectedText();
        if (string.IsNullOrEmpty(selectedText))
            return;
        
        var clipboard = _textInput.Context?.Get<IClipboard>();
        if (clipboard == null)
            return;
   
        clipboard.SetText(selectedText);
    }

    public View View => _textInput;
}
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
        this.RequestFocus();
    }

    public void OnMouseExit(ref MouseExitEvent e)
    {
        if (!_textInput.IsEditing)
            this.Blur();
    }
    
    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
    }

    public void OnFocusLost()
    {
        _textInput.StopEditing();
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
        var isLeftMouseButtonPressed = e.Mouse.IsButtonPressed(MouseButton.Left);
        if (!isLeftMouseButtonPressed)
            return;

        _textInput.MoveCaretTo(e.Mouse.Point, true);
    }

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
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
            }
            
            var mousePoint = e.Mouse.Point;
            _textInput.MoveCaretTo(mousePoint);
        }
    }

    public void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
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
            return;
        }

        if (e.Key == KeyboardKey.C && e.Modifiers.HasFlag(ctrlModifier))
        {
            Copy();
            return;
        }
            
        if (e.Key == KeyboardKey.V && e.Modifiers.HasFlag(ctrlModifier))
        {
            Paste();
            return;
        }
            
        if (e.Key == KeyboardKey.X && e.Modifiers.HasFlag(ctrlModifier))
        {
            Cut();
            return;
        }
            
        var isShiftPressed = (e.Modifiers & InputModifiers.Shift) > 0;
        if (e.Key == KeyboardKey.LeftArrow)
        {
            _textInput.MoveCaretLeft(isShiftPressed);
            return;
        }
            
        if (e.Key == KeyboardKey.RightArrow)
        {
            _textInput.MoveCaretRight(isShiftPressed);
            return;
        }
            
        if (e.Key == KeyboardKey.Backspace)
        {
            _textInput.Delete();
            return;
        }
        

        var c = e.Key.ToChar(isShiftPressed);
        if (c == '\0')
        {
            return;
        }

        _textInput.Enter(c);
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
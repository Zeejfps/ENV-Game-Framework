using System.Runtime.InteropServices;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public abstract class BaseTextInputKbmController : KeyboardMouseController
{
    private readonly TextInputView _textInput;

    public bool IsMultiLine { get; set; }

    public int DoubleClickThresholdMs { get; set; } = 400;

    private bool _hasLastClick;
    private int _lastClickTickMs;

    public BaseTextInputKbmController(TextInputView textInput)
    {
        _textInput = textInput;
    }

    // Use this from outside the controller (e.g. dialog auto-focus) to enter an edit
    // session. Pairing StartEditing with StealFocus is what makes the input the actual
    // _focusedComponent — without it, keys only flow while the cursor happens to hover
    // the input and silently stop the moment the mouse moves elsewhere.
    public void BeginEditing()
    {
        _textInput.StartEditing();
        _textInput.Context?.Get<InputSystem>()?.StealFocus(this);
    }
    
    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        var isLeftMouseButtonPressed = e.Mouse.IsButtonPressed(MouseButton.Left);
        if (!isLeftMouseButtonPressed)
            return;

        if (!_textInput.IsEditing)
            return;
        
        _textInput.MoveCaretTo(e.Mouse.Point, true);
        e.Consume();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (e.State == InputState.Pressed && e.Button == MouseButton.Left)
        {
            var isEditing = _textInput.IsEditing;
            var containsPoint = _textInput.Position.ContainsPoint(e.Mouse.Point);
            var inputSystem = _textInput.Context?.Get<InputSystem>();

            if (isEditing && !containsPoint)
            {
                _textInput.StopEditing();
                inputSystem?.Blur(this);
                _hasLastClick = false;
                return;
            }

            if (!isEditing && containsPoint)
            {
                _textInput.StartEditing();
                inputSystem?.StealFocus(this);
            }

            var now = Environment.TickCount;
            var isDoubleClick = _hasLastClick
                && unchecked(now - _lastClickTickMs) <= DoubleClickThresholdMs;
            if (isDoubleClick)
            {
                _textInput.SelectAll();
                _hasLastClick = false;
                e.Consume();
                return;
            }

            _lastClickTickMs = now;
            _hasLastClick = true;

            var mousePoint = e.Mouse.Point;
            _textInput.MoveCaretTo(mousePoint);
            e.Consume();
        }
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (!_textInput.IsEditing)
            return;

        if (e.State != InputState.Pressed) 
            return;

        OnKeyboardKeyPressed(ref e);   
    }

    protected virtual void OnKeyboardKeyPressed(ref KeyboardKeyEvent e)
    {
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
        
        if (e.Modifiers == InputModifiers.Shift && e.Key == KeyboardKey.Enter && IsMultiLine)
        {
            Enter('\n');
            e.Consume();
            return;
        }
        
        var isShiftPressed = (e.Modifiers & InputModifiers.Shift) > 0;
        if (e.Key == KeyboardKey.UpArrow)
        {
            _textInput.MoveCaretUp(isShiftPressed);
            e.Consume();
            return;
        }
        
        if (e.Key == KeyboardKey.DownArrow)
        {
            _textInput.MoveCaretDown(isShiftPressed);
            e.Consume();
            return;
        }
        
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

        Enter(c);
        e.Consume();
    }

    protected virtual void Enter(char c)
    {
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
}
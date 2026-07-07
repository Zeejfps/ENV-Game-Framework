using System.Runtime.InteropServices;
using ZGF.Desktop;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Components.TextInput;

public abstract class BaseTextInputKbmController : KeyboardMouseController, IProvidesCursor
{
    public MouseCursor Cursor => MouseCursor.Text;

    private readonly TextInputView _textInput;
    private readonly InputSystem _inputSystem;
    private readonly IClipboard? _clipboard;

    public bool IsMultiLine { get; set; }

    // Optional focus-traversal hooks. When set, Tab / Shift+Tab move focus instead of
    // inserting a tab character — the owner wires these into a focus ring.
    public Action? OnTab { get; set; }
    public Action? OnShiftTab { get; set; }

    /// <summary>The scrolling viewport this input lives in, if any. When set, every caret-moving
    /// interaction (typing, arrows, clicks, drag-selection, paste) asks it to keep the caret's
    /// line in view, so a multi-line editor follows the caret instead of letting it leave the
    /// viewport.</summary>
    public IScrollScope? ScrollScope { get; set; }

    public int DoubleClickThresholdMs { get; set; } = 400;

    private bool _hasLastClick;
    private int _lastClickTickMs;

    public BaseTextInputKbmController(TextInputView textInput, InputSystem inputSystem, IClipboard? clipboard = null)
    {
        _textInput = textInput;
        _inputSystem = inputSystem;
        _clipboard = clipboard;
    }

    // Use this from outside the controller (e.g. dialog auto-focus) to enter an edit
    // session. Pairing StartEditing with StealFocus is what makes the input the actual
    // _focusedComponent — without it, keys only flow while the cursor happens to hover
    // the input and silently stop the moment the mouse moves elsewhere.
    public void BeginEditing()
    {
        _textInput.StartEditing();
        _inputSystem.StealFocus(this);
    }

    // Ends the edit session and releases focus — the counterpart to BeginEditing, used
    // when focus moves elsewhere (e.g. a focus ring tabbing away) so the caret doesn't
    // linger in an unfocused field.
    public void EndEditing()
    {
        _textInput.StopEditing();
        _inputSystem.Blur(this);
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
        RevealCaret();
        e.Consume();
    }

    // Ask the enclosing scroll scope (if any) to bring the caret's line into view. Guarded on
    // IsEditing so a blur (click-away, Tab traversal) never scrolls back to a field being left.
    private void RevealCaret()
    {
        if (ScrollScope == null || !_textInput.IsEditing)
            return;
        ScrollScope.EnsureVisible(_textInput.GetCaretRect());
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (e.State == InputState.Pressed && e.Button == MouseButton.Left)
        {
            var isEditing = _textInput.IsEditing;
            var containsPoint = _textInput.Position.ContainsPoint(e.Mouse.Point);
            var inputSystem = _inputSystem;

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
            RevealCaret();
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
        // After any handled key: edits and caret moves both flow through here, and for keys that
        // moved nothing (Ctrl+C, say) the reveal is a no-op since the caret is already in view.
        RevealCaret();
    }

    protected virtual void OnKeyboardKeyPressed(ref KeyboardKeyEvent e)
    {
        if (e.Key == KeyboardKey.Tab && (OnTab != null || OnShiftTab != null))
        {
            if ((e.Modifiers & InputModifiers.Shift) != 0) OnShiftTab?.Invoke();
            else OnTab?.Invoke();
            e.Consume();
            return;
        }

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
        
        // Word-jump modifier: Ctrl on Windows/Linux, Option (Alt) on macOS — Cmd/Super is
        // reserved there for line-start/end, so it's a separate variable from ctrlModifier.
        var wordModifier = InputModifiers.Control;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            wordModifier = InputModifiers.Alt;
        }
        var isWordJump = e.Modifiers.HasFlag(wordModifier);

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
        
        if (e.Key == KeyboardKey.LeftArrow || e.Key == KeyboardKey.RightArrow)
        {
            // Arrow keys move the caret visually. In an RTL field "visually left" is the logically
            // later character, so the key's logical direction flips. LTR is unchanged.
            var visualLeft = e.Key == KeyboardKey.LeftArrow;
            var moveForward = _textInput.IsContentRtl ? visualLeft : !visualLeft;
            if (isWordJump)
            {
                if (moveForward) _textInput.MoveCaretRightWord(isShiftPressed);
                else _textInput.MoveCaretLeftWord(isShiftPressed);
            }
            else
            {
                if (moveForward) _textInput.MoveCaretRight(isShiftPressed);
                else _textInput.MoveCaretLeft(isShiftPressed);
            }
            e.Consume();
            return;
        }
            
        if (e.Key == KeyboardKey.Backspace)
        {
            if (isWordJump)
                _textInput.DeleteWord();
            else
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
        var text = _clipboard?.GetText();
        if (text == null) 
            return;
        
        _textInput.Enter(text);
    }

    private void Copy()
    {
        var selectedText = _textInput.GetSelectedText();
        if (string.IsNullOrEmpty(selectedText))
            return;
        
        if (_clipboard == null)
            return;

        _clipboard.SetText(selectedText);
    }
}
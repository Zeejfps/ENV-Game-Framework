using System.Runtime.InteropServices;
using System.Text;
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
        // The IME is off outside a text field, so that a CJK layout doesn't start composing on the
        // keys that navigate the commit list.
        _inputSystem.ImeHost?.SetImeEnabled(true);
        UpdateImeCaretRect();
    }

    // Ends the edit session and releases focus — the counterpart to BeginEditing, used
    // when focus moves elsewhere (e.g. a focus ring tabbing away) so the caret doesn't
    // linger in an unfocused field.
    public void EndEditing()
    {
        // Reset before disabling: a composition must never outlive the field it was being typed
        // into, and the text it holds is discarded rather than committed.
        _inputSystem.ImeHost?.ResetComposition();
        _textInput.ClearComposition();
        _inputSystem.ImeHost?.SetImeEnabled(false);
        _textInput.StopEditing();
        _inputSystem.Blur(this);
    }

    // Focus can be taken away without anyone calling EndEditing (a focus ring moving on, another
    // component stealing it), and a composition left behind would keep rendering in a field that no
    // longer has the keyboard. Sealed so that a subclass cannot drop the IME cleanup by overriding
    // and forgetting to call base — it hooks OnFocusLostCore instead.
    public sealed override void OnFocusLost()
    {
        _inputSystem.ImeHost?.ResetComposition();
        _textInput.ClearComposition();
        _inputSystem.ImeHost?.SetImeEnabled(false);
        OnFocusLostCore();
    }

    /// <summary>What this field does when it loses focus — commit, revert, close. Runs after the IME
    /// has been torn down, so the buffer holds committed text only.</summary>
    protected virtual void OnFocusLostCore()
    {
    }

    /// <summary>Updates the in-flight composition and keeps the OS candidate window on the caret.</summary>
    public override void OnComposition(ref CompositionEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;

        if (!_textInput.IsEditing)
            return;

        _textInput.SetComposition(e.Preedit);
        UpdateImeCaretRect();
        RevealCaret();
        e.Consume();
    }

    private void UpdateImeCaretRect()
    {
        if (!_textInput.IsEditing)
            return;
        _inputSystem.ImeHost?.SetImeCaretRect(_textInput.GetCaretRect());
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

            if (isEditing && !containsPoint)
            {
                EndEditing();
                _hasLastClick = false;
                return;
            }

            if (!isEditing && containsPoint)
            {
                BeginEditing();
            }

            // Clicking elsewhere in the field abandons the composition rather than committing it —
            // the caret is about to move out from under it.
            if (_textInput.IsComposing)
            {
                _inputSystem.ImeHost?.ResetComposition();
                _textInput.ClearComposition();
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

        // While a composition is live, Enter/Escape/Space/arrows are the IME's — they pick a
        // candidate, they do not edit and they must not reach the app. Enter is the dangerous one:
        // leaked, it submits the commit in the middle of a half-typed word. A patched GLFW already
        // withholds the keys the IME consumed, so this is a second line of defence against a future
        // GLFW that doesn't — and the failure it guards is silent and destructive. Consuming the
        // event also stops it bubbling to the app's own keybindings.
        if (_textInput.IsComposing)
        {
            e.Consume();
            return;
        }

        OnKeyboardKeyPressed(ref e);
        // After any handled key: edits and caret moves both flow through here, and for keys that
        // moved nothing (Ctrl+C, say) the reveal is a no-op since the caret is already in view.
        RevealCaret();
        UpdateImeCaretRect();
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
        
        // Enter breaks the line in a multi-line editor. Ctrl/Cmd+Enter is deliberately left alone —
        // that's the owner's submit shortcut (commit, save) and it has to bubble past us.
        var isEnter = e.Key == KeyboardKey.Enter || e.Key == KeyboardKey.NumpadEnter;
        var isSubmitChord = e.Modifiers.HasFlag(InputModifiers.Control)
            || e.Modifiers.HasFlag(InputModifiers.Super);
        if (isEnter && IsMultiLine && !isSubmitChord)
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
    }

    /// <summary>Inserts a character committed by the OS text-input pipeline. This is the only path
    /// that types: key events carry physical, layout-independent positions, so decoding them into
    /// characters would hard-code a US layout and make Cyrillic, accented Latin and every other
    /// non-ASCII script untypable.</summary>
    public override void OnTextInput(ref TextInputEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;

        if (!_textInput.IsEditing)
            return;

        // Enter and Tab are key gestures, handled above; the OS never commits them as text, but the
        // harness can synthesize anything, so don't let a control code reach the buffer.
        if (Rune.IsControl(e.Rune))
            return;

        Span<char> utf16 = stackalloc char[2];
        var length = e.Rune.EncodeToUtf16(utf16);
        Enter(utf16[..length]);
        e.Consume();
        RevealCaret();
    }

    protected virtual void Enter(char c)
    {
        _textInput.Enter(c);
    }

    protected virtual void Enter(ReadOnlySpan<char> text)
    {
        _textInput.Enter(text);
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
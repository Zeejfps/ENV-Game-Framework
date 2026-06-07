namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// Ties the keyboard size (<see cref="KeyboardInsets"/>) to a scroll container
/// (<see cref="IKeyboardScrollable"/>): mirrors the inset onto the container and keeps the focused
/// field scrolled into the visible region. Registered in the <see cref="Context"/> so editable
/// controls can report focus via <see cref="Focus"/> / <see cref="Blur"/> without knowing about
/// scrolling. The platform parallel that feeds it is the host's keyboard reporting.
/// </summary>
public sealed class KeyboardAvoidanceController
{
    // A downward drag past this many canvas points dismisses the keyboard — the onDrag behavior
    // native scroll views use (Mail, Notes, Messages).
    private const float SwipeDismissThreshold = 24f;

    private readonly IKeyboardScrollable _scrollable;
    private readonly KeyboardInsets _insets;
    private readonly MobileInputSystem _input;
    private readonly ITextInputService _textInput;
    private View? _focused;
    private float _pressY;

    public KeyboardAvoidanceController(IKeyboardScrollable scrollable, KeyboardInsets insets,
        MobileInputSystem input, ITextInputService textInput)
    {
        _scrollable = scrollable;
        _insets = insets;
        _input = input;
        _textInput = textInput;
        _insets.Changed += Apply;
        _input.PointerPressed += OnPointerPressed;
        _input.PointerDragged += OnPointerDragged;
    }

    /// <summary>A field gained focus; reserve keyboard space and bring it into view.</summary>
    public void Focus(View view)
    {
        _focused = view;
        Apply();
    }

    /// <summary>A field lost focus; stop tracking it (the inset itself drives the keyboard hide).</summary>
    public void Blur(View view)
    {
        if (ReferenceEquals(_focused, view))
            _focused = null;
        Apply();
    }

    private void Apply()
    {
        // The framework Done bar sits above the keyboard, so reserve room for both — a focused field
        // is scrolled clear of the bar as well as the keyboard.
        _scrollable.BottomInset = _insets.Bottom > 0f ? _insets.Bottom + KeyboardAccessoryBar.BarHeight : 0f;
        if (_focused != null && _insets.Bottom > 0f)
            _scrollable.ScrollIntoView(_focused);
    }

    // Tap-outside-to-dismiss: a tap on the edited field (or another text field, which just moves
    // focus) is left alone; anything else commits the field, like a native form.
    private void OnPointerPressed(View? hit)
    {
        _pressY = _input.Pointer.Point.Y;
        if (_focused == null)
            return;
        if (hit != null && (IsWithin(hit, _focused) || hit is ITextInputClient))
            return;
        Dismiss();
    }

    // Swipe-down-to-dismiss. The canvas is Y-up, so a downward finger drag lowers the canvas Y;
    // once it passes the threshold, treat the gesture as a dismissal.
    private void OnPointerDragged()
    {
        if (_focused != null && _pressY - _input.Pointer.Point.Y > SwipeDismissThreshold)
            Dismiss();
    }

    /// <summary>Commit and dismiss the field currently being edited, if any (e.g. the Done bar).</summary>
    public void Dismiss()
    {
        if (_focused is ITextInputClient client)
            _textInput.EndEdit(client);
    }

    private static bool IsWithin(View view, View ancestor)
    {
        for (View? v = view; v != null; v = v.Parent)
            if (ReferenceEquals(v, ancestor))
                return true;
        return false;
    }
}

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
    private readonly IKeyboardScrollable _scrollable;
    private readonly KeyboardInsets _insets;
    private View? _focused;

    public KeyboardAvoidanceController(IKeyboardScrollable scrollable, KeyboardInsets insets)
    {
        _scrollable = scrollable;
        _insets = insets;
        _insets.Changed += Apply;
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
        _scrollable.BottomInset = _insets.Bottom;
        if (_focused != null && _insets.Bottom > 0f)
            _scrollable.ScrollIntoView(_focused);
    }
}

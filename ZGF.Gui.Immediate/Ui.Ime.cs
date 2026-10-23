namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    private int _composer;

    /// <summary>Where the caret of the text field holding focus sits this frame, in UI points, so
    /// the platform can put the IME candidate window against it. Null while no field reported one.</summary>
    internal UiRect? ImeCaret { get; private set; }

    /// <summary>The in-flight composition was abandoned this frame and the platform should discard
    /// it: the field composing lost text focus, or asked with <see cref="ResetComposition"/>.</summary>
    internal bool CompositionReset { get; private set; }

    /// <summary>Reports the focused text field's caret, in UI points, for the IME candidate window.
    /// Call it every frame the field holds focus.</summary>
    public void SetImeCaret(UiRect caret) => ImeCaret = caret;

    /// <summary>Discards the in-flight composition rather than committing it — for a press that
    /// moves the caret out from under it.</summary>
    public void ResetComposition() => CompositionReset = true;

    private void BeginIme()
    {
        ImeCaret = null;
        CompositionReset = false;
    }

    private void EndIme()
    {
        var composer = WantsTextInput ? _focused : 0;
        if (_composer != 0 && composer != _composer)
            CompositionReset = true;
        _composer = composer;
        if (composer == 0)
            ImeCaret = null;
    }
}

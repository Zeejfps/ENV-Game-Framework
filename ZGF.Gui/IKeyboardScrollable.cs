namespace ZGF.Gui;

/// <summary>
/// A scroll container that can reserve space at the bottom (e.g. for an on-screen keyboard) and
/// scroll a descendant into the remaining visible region. Platform-neutral: a mobile host reports
/// the keyboard size as <see cref="BottomInset"/> and the framework does the scrolling, so keyboard
/// avoidance needs no per-platform view shifting.
/// </summary>
public interface IKeyboardScrollable
{
    /// <summary>Points reserved at the bottom of the viewport (the keyboard's covered height).</summary>
    float BottomInset { get; set; }

    /// <summary>Scroll so <paramref name="descendant"/> sits within the visible region above the inset.</summary>
    void ScrollIntoView(View descendant);
}

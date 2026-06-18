using ZGF.Observable;

namespace ZGF.Gui.Desktop.Widgets;

/// <summary>
/// A snapshot of an input surface's interaction flags, handed to a per-property style resolver —
/// the framework's counterpart to Flutter's <c>Set&lt;WidgetState&gt;</c>. Reading it inside a
/// tracked binding subscribes to the underlying <see cref="Interaction"/>, so the property
/// re-resolves on transition.
/// </summary>
public readonly record struct InteractionState(bool Hovered);

/// <summary>
/// View-lifetime interaction state for an input surface, owned by <see cref="Interactive"/> and
/// driven by <see cref="KbmInput"/>. Style props resolve against <see cref="Snapshot"/> instead of
/// the widget hand-managing a <see cref="State{T}"/> off hover callbacks — hover (and later
/// press/focus) is a fact the input layer already knows, exposed as a source.
/// </summary>
public sealed class Interaction
{
    private readonly State<bool> _hovered = new(false);

    /// <summary>True while the pointer is over the surface. Written by <see cref="Interactive"/>.</summary>
    public IReadable<bool> Hovered => _hovered;

    /// <summary>The current flags, read inside a tracked binding so it re-fires on change.</summary>
    public InteractionState Snapshot() => new(_hovered.Value);

    internal void SetHovered(bool value) => _hovered.Value = value;
}

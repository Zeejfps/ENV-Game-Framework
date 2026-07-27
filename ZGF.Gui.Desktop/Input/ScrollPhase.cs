namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// Where a scroll event sits in a continuous scrolling gesture. Mirrors Cocoa's <c>NSEventPhase</c>,
/// which is the only platform that currently reports it — everywhere else, and for a classic mouse
/// wheel on any platform, every value is <see cref="None"/>.
/// <para>A wheel notch is a discrete event with no phase; a trackpad swipe is a *stream* of events with
/// one, which is what lets a consumer tell one gesture from the next instead of guessing from timing.</para>
/// </summary>
public enum ScrollPhase
{
    /// <summary>No phase reported: a classic wheel, or a platform that doesn't supply one.</summary>
    None,

    /// <summary>The gesture has just started — fingers touched down, or inertia began.</summary>
    Began,

    /// <summary>The gesture is in progress.</summary>
    Changed,

    /// <summary>Touching but not moving.</summary>
    Stationary,

    /// <summary>The gesture finished normally.</summary>
    Ended,

    /// <summary>The gesture was cancelled by the system.</summary>
    Cancelled,

    /// <summary>Fingers are resting and a gesture may be about to start.</summary>
    MayBegin,
}

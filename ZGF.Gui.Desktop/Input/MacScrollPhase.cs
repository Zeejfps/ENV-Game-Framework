using ZGF.Rendering.Metal;

namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// Reads the scroll phases of the NSEvent currently being dispatched.
/// <para>GLFW's scroll callback hands over only the two deltas — it has no notion of gesture phase, and
/// exposes no accessor for the event behind the callback. But the callback runs inside
/// <c>[NSApp sendEvent:]</c>, so the scroll event is still the application's <c>currentEvent</c> while we
/// are in there, and its <c>phase</c> / <c>momentumPhase</c> can be read directly. That's what separates a
/// user's swipe from the inertia the system sends afterwards — information no amount of delta-timing
/// heuristics can reliably reconstruct.</para>
/// <para>Reuses the Objective-C runtime interop already in <see cref="Objc"/> rather than duplicating the
/// P/Invokes. Every entry point degrades to <see cref="ScrollPhase.None"/> off macOS, or if AppKit isn't
/// loaded, or when there's no current event — so a caller never has to platform-check.</para>
/// </summary>
internal static class MacScrollPhase
{
    // NSEventPhase is a bitmask (NSEventPhaseNone is 0, the rest are single bits).
    private const nuint PhaseNone = 0;
    private const nuint PhaseBegan = 1 << 0;
    private const nuint PhaseStationary = 1 << 1;
    private const nuint PhaseChanged = 1 << 2;
    private const nuint PhaseEnded = 1 << 3;
    private const nuint PhaseCancelled = 1 << 4;
    private const nuint PhaseMayBegin = 1 << 5;

    private static readonly bool Available = OperatingSystem.IsMacOS();

    // Resolved once. Class() returns Zero when AppKit isn't in the process (a headless test host), which
    // Read() treats the same as "no phase information".
    private static readonly IntPtr NsApplicationClass = Available ? Objc.Class("NSApplication") : IntPtr.Zero;
    private static readonly IntPtr SharedApplicationSel = Available ? Objc.Sel("sharedApplication") : IntPtr.Zero;
    private static readonly IntPtr CurrentEventSel = Available ? Objc.Sel("currentEvent") : IntPtr.Zero;
    private static readonly IntPtr PhaseSel = Available ? Objc.Sel("phase") : IntPtr.Zero;
    private static readonly IntPtr MomentumPhaseSel = Available ? Objc.Sel("momentumPhase") : IntPtr.Zero;

    /// <summary>
    /// The gesture and momentum phases of the event being dispatched right now. Call from inside a scroll
    /// callback — outside one there is no relevant current event and the result is
    /// <see cref="ScrollPhase.None"/> for both.
    /// </summary>
    public static (ScrollPhase Gesture, ScrollPhase Momentum) Read()
    {
        if (!Available || NsApplicationClass == IntPtr.Zero)
            return (ScrollPhase.None, ScrollPhase.None);

        var app = Objc.msg_IntPtr(NsApplicationClass, SharedApplicationSel);
        if (app == IntPtr.Zero)
            return (ScrollPhase.None, ScrollPhase.None);

        var currentEvent = Objc.msg_IntPtr(app, CurrentEventSel);
        if (currentEvent == IntPtr.Zero)
            return (ScrollPhase.None, ScrollPhase.None);

        // Both selectors are declared on NSEvent and valid for scroll-wheel events; for a classic wheel
        // Cocoa itself reports NSEventPhaseNone, which is exactly what we want to pass through.
        var gesture = Map(Objc.msg_NUInt(currentEvent, PhaseSel));
        var momentum = Map(Objc.msg_NUInt(currentEvent, MomentumPhaseSel));
        return (gesture, momentum);
    }

    private static ScrollPhase Map(nuint phase) => phase switch
    {
        PhaseNone => ScrollPhase.None,
        PhaseBegan => ScrollPhase.Began,
        PhaseStationary => ScrollPhase.Stationary,
        PhaseChanged => ScrollPhase.Changed,
        PhaseEnded => ScrollPhase.Ended,
        PhaseCancelled => ScrollPhase.Cancelled,
        PhaseMayBegin => ScrollPhase.MayBegin,
        // A bitmask in principle, so an unrecognised combination is reported as in-progress rather than
        // as "no phase" — a consumer keying off Began/momentum still behaves sensibly.
        _ => ScrollPhase.Changed,
    };
}

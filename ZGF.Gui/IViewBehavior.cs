namespace ZGF.Gui;

/// <summary>
/// A unit of view-attached lifetime: <see cref="Attach"/> fires when the view mounts into a
/// window's live tree, <see cref="Detach"/> when it unmounts. Subscriptions, input
/// registrations and other per-mount resources are created in Attach and disposed in Detach.
/// Dependencies are captured at construction time (resolved from the window's build
/// <see cref="Context"/>) — nothing is resolved at attach time.
/// </summary>
public interface IViewBehavior
{
    void Attach(View view);
    void Detach(View view);
}

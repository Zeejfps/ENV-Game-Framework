using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(ZGF.Gui.Desktop.HotReloadService))]

namespace ZGF.Gui.Desktop;

/// <summary>
/// Bridges .NET Hot Reload (dotnet watch / Rider) to the GUI. After the runtime applies a
/// metadata delta to edited method bodies, it invokes the static handler methods below by
/// reflection — discovered via the assembly-level <see cref="MetadataUpdateHandlerAttribute"/>,
/// which fires for edits in any loaded assembly, not just this one. The running view tree still
/// reflects the pre-edit code, so <see cref="UpdateApplication"/> raises <see cref="UpdateApplied"/>;
/// <see cref="GuiApp"/> subscribes and rebuilds its tree from the (now-patched) content factory.
/// The event fires on the hot-reload agent's background thread, so subscribers must marshal onto
/// the UI thread themselves.
/// </summary>
internal static class HotReloadService
{
    public static event Action<Type[]?>? UpdateApplied;

    // A handler may expose ClearCache to drop memoized state before the update is observed.
    // Nothing here caches across an edit, so it is intentionally empty — present only so the
    // handler shape is complete.
    public static void ClearCache(Type[]? updatedTypes) { }

    public static void UpdateApplication(Type[]? updatedTypes) => UpdateApplied?.Invoke(updatedTypes);
}

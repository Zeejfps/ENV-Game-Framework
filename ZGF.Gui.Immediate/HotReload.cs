using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(ZGF.Gui.Immediate.HotReload))]

namespace ZGF.Gui.Immediate;

/// <summary>Render runs every frame, so an edited method body is live as soon as the next frame
/// draws; this only asks for that frame. Raised on the hot-reload agent's thread.</summary>
internal static class HotReload
{
    public static event Action? Applied;

    public static void ClearCache(Type[]? updatedTypes) { }

    public static void UpdateApplication(Type[]? updatedTypes) => Applied?.Invoke();
}

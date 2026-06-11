using ZGF.Gui.Components;

namespace ZGF.Gui.Desktop;

public static class ComponentAppExtensions
{
    /// <summary>Mounts a component as the main window's root content.</summary>
    public static GuiAppBuilder UseContent(this GuiAppBuilder builder, IComponent root) =>
        builder.UseContent(root.BuildView);
}

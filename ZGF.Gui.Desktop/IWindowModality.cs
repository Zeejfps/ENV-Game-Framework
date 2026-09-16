using ZGF.Desktop;

namespace ZGF.Gui.Desktop;

/// <summary>Platform hooks for native window ownership and input disabling, which GLFW does not
/// expose. The secondary-window host also gates GUI input independently of these native hooks.</summary>
public interface IWindowModality
{
    void SetInputEnabled(IWindow window, bool enabled);
    void SetOwner(IWindow window, IWindow owner);
}

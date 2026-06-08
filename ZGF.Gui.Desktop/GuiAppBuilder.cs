using ZGF.Desktop;

namespace ZGF.Gui.Desktop;

/// <summary>
/// Fluent builder for a <see cref="GuiApp"/>. Register application services on
/// <see cref="Services"/> and supply the root <see cref="View"/> via <see cref="UseContent"/>;
/// <see cref="Build"/> resolves the platform backend, wires the framework services (input,
/// dispatcher, popups, clipboard, ...) into the same <see cref="Context"/>, then mounts the content.
/// Because mounting happens after the framework services are registered, the content's
/// <c>OnAttachedToContext</c> sees a fully-wired context and can build itself from it — there is no
/// "register, then resolve in a specific order" dance for callers to get wrong.
/// </summary>
public sealed class GuiAppBuilder
{
    private readonly StartupConfig _config;
    private View? _content;

    internal GuiAppBuilder(StartupConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// The service container shared with the GUI. Register application services here before
    /// <see cref="Build"/>; the framework adds its own services into this same container during the
    /// build, so the root content can resolve both from its <c>OnAttachedToContext</c>.
    /// </summary>
    public Context Services { get; } = new();

    /// <summary>Sets the root content mounted into the main window.</summary>
    public GuiAppBuilder UseContent(View content)
    {
        _content = content;
        return this;
    }

    /// <summary>Resolves the backend, wires framework services, and mounts the content.</summary>
    public GuiApp Build()
    {
        if (_content is null)
            throw new InvalidOperationException(
                "No root content set. Call UseContent(...) before Build().");
        return GuiApp.Create(_config, Services, _content);
    }
}

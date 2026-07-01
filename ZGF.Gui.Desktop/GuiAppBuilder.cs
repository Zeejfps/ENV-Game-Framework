using ZGF.Desktop;
using ZGF.Gui.Widgets;

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
    private Func<Context, View>? _contentFactory;
    private GuiRenderBackendKind _backendKind = GuiRenderBackendKind.Auto;
    private Action? _renderHook;
    private Action<Context>? _startup;
    private int? _mcpServerPort;

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
        _contentFactory = _ => content;
        return this;
    }

    public GuiAppBuilder UseContent<T>() where T : Widget
    {
        _contentFactory = ctx => ctx.Require<T>().BuildView(ctx);
        return this;
    }

    /// <summary>
    /// Sets the root content as a factory invoked with the fully-wired <see cref="Context"/> —
    /// framework services (input, canvas, popups, ...) are registered before it runs, so the
    /// factory can resolve them at build time instead of waiting for attach.
    /// </summary>
    public GuiAppBuilder UseContent(Func<Context, View> contentFactory)
    {
        _contentFactory = contentFactory;
        return this;
    }

    /// <summary>
    /// Forces a specific graphics backend instead of the platform default. Apps that embed
    /// the GUI over their own engine rendering pick the backend their engine code targets
    /// (e.g. <see cref="GuiRenderBackendKind.OpenGl"/> for raw GL on any platform).
    /// </summary>
    public GuiAppBuilder UseRenderBackend(GuiRenderBackendKind kind)
    {
        _backendKind = kind;
        return this;
    }

    /// <summary>
    /// Runs at the start of every main-window frame, with the window's graphics context
    /// current, before the GUI clears and draws — the seam for engine rendering underneath
    /// or alongside the GUI (e.g. render a scene into a frame buffer shown by an ImageView).
    /// </summary>
    public GuiAppBuilder UseRenderHook(Action renderFrame)
    {
        _renderHook = renderFrame;
        return this;
    }

    /// <summary>
    /// Runs once during <see cref="Build"/>, after all framework and backend services are
    /// registered but before the content factory — with the main window's graphics context
    /// current. The place to create engine resources (meshes, shaders, frame buffers) that
    /// the content needs at build time.
    /// </summary>
    public GuiAppBuilder UseStartup(Action<Context> startup)
    {
        _startup = startup;
        return this;
    }

    /// <summary>
    /// Starts a localhost-only Model Context Protocol server (Streamable HTTP) on
    /// <paramref name="port"/> once the app is built, at <c>http://127.0.0.1:{port}/mcp</c>. It
    /// exposes the live window to an MCP client through tools: read the view tree (<c>gui_snapshot</c>),
    /// inject input (<c>gui_click</c>, <c>gui_type</c>, <c>gui_key</c>), and capture a screenshot
    /// (<c>gui_screenshot</c>). A debugging aid for driving the running window from an LLM or an agent.
    /// The server also auto-starts (without this call) when the <c>ZGF_GUI_MCP</c> environment variable
    /// is set, reading <c>ZGF_GUI_MCP_PORT</c> (default 5577).
    /// </summary>
    public GuiAppBuilder UseMcpServer(int port = 5577)
    {
        _mcpServerPort = port;
        return this;
    }

    /// <summary>Resolves the backend, wires framework services, and mounts the content.</summary>
    public GuiApp Build()
    {
        if (_contentFactory is null)
            throw new InvalidOperationException(
                "No root content set. Call UseContent(...) before Build().");
        return GuiApp.Create(_config, Services, _contentFactory, _backendKind, _renderHook, _startup, _mcpServerPort);
    }
}

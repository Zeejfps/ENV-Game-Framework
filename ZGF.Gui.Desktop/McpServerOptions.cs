using McpSdk.Server;

namespace ZGF.Gui.Desktop;

/// <summary>
/// What a <see cref="GuiMcpServer"/> starts from: where it listens, how it introduces itself to the
/// client, and which tools and prompts it serves. The framework's own <c>gui_*</c> debug tools are
/// off unless asked for, so an app-facing server exposes only what the app registers.
/// </summary>
public sealed record McpServerOptions
{
    private readonly int _port = 5577;

    /// <summary>Localhost port to bind (1–65535).</summary>
    public int Port
    {
        get => _port;
        init => _port = value is >= 1 and <= 65535
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Port), value, "Port must be between 1 and 65535.");
    }

    /// <summary>The <c>serverInfo.name</c> the client sees on initialize.</summary>
    public string ServerName { get; init; } = "ZGF GUI";

    /// <summary>
    /// Model-facing guidance sent as <c>instructions</c> on initialize — how to use this server as a
    /// whole — which the client may feed to the model as a system prompt. Null sends none.
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>
    /// Gates the endpoint: when set it is <c>/mcp/{token}</c> and every other path, bare <c>/mcp</c>
    /// included, answers 404 — so only a client handed the full URL reaches the server.
    /// </summary>
    public McpPathToken? PathToken { get; init; }

    /// <summary>
    /// Whether the <c>gui_*</c> view-tree and input-injection tools are served. A debug aid: they
    /// let a client click and type into the live window, so a server a user turns on from a
    /// preference leaves them off.
    /// </summary>
    public bool IncludeGuiTools { get; init; }

    /// <summary>App tool sources, each asked to register its tools once per client session.</summary>
    public IReadOnlyList<IMcpToolSource> ToolSources { get; init; } = [];

    /// <summary>Prompts to advertise; null leaves the prompts capability off.</summary>
    public IPromptController? Prompts { get; init; }
}

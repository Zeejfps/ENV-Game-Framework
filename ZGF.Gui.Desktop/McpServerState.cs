namespace ZGF.Gui.Desktop;

/// <summary>Whether a <see cref="GuiApp"/>'s MCP server is up, and where.</summary>
public abstract record McpServerState
{
    private McpServerState() { }

    public sealed record Stopped : McpServerState;

    public sealed record Running(Uri Endpoint) : McpServerState;
}

/// <summary>The outcome of <see cref="GuiApp.StartMcpServer"/>.</summary>
public abstract record McpServerStart
{
    private McpServerStart() { }

    public sealed record Started(Uri Endpoint) : McpServerStart;

    /// <summary>A server was already up; it is left as it was, and its endpoint is reported.</summary>
    public sealed record AlreadyRunning(Uri Endpoint) : McpServerStart;

    /// <summary>The port could not be bound — typically another process holds it.</summary>
    public sealed record Failed(string Message) : McpServerStart;
}

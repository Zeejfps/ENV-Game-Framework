using System.Net;
using McpSdk.Adapter.StreamableHttpServer;
using McpSdk.Adapter.System.Text.Json;
using McpSdk.Protocol;
using McpSdk.Server;
using McpSdk.Shared;

namespace ZGF.Gui.Mcp;

/// <summary>
/// An in-process Model Context Protocol server (Streamable HTTP, bound to 127.0.0.1) through which an
/// MCP client — an LLM, an agent, a script — reaches the running app. What it serves comes from
/// <see cref="McpServerOptions"/>: its tool sources, prompts and model-facing instructions, and
/// nothing else. Tools run where the transport calls them and may be genuinely asynchronous. A
/// constructed server is listening; <see cref="Dispose"/> ends every open session and stops it.
/// </summary>
public sealed class GuiMcpServer : IDisposable
{
    private readonly McpServerOptions _options;
    private readonly SystemJson _json = new();
    private readonly McpConsoleLogger _loggerFactory = new();
    private readonly StreamableHttpListener _listener;

    private readonly object _sessionsGate = new();
    private readonly HashSet<HttpServerTransport> _sessions = new();
    private bool _disposed;

    /// <summary>Binds <see cref="McpServerOptions.Port"/> and starts accepting sessions. Throws
    /// <see cref="HttpListenerException"/> when the port cannot be bound.</summary>
    public GuiMcpServer(McpServerOptions options)
    {
        _options = options;
        var baseUrl = $"http://127.0.0.1:{options.Port}";
        var path = options.PathToken is { } token ? $"/mcp/{token.Value}" : "/mcp";
        Endpoint = new Uri(baseUrl + path);
        _listener = new StreamableHttpListener(baseUrl, path, _json, _loggerFactory, OnSession);
        // The listener binds synchronously and hands its accept loop to the thread pool, so this
        // returns as soon as the port is ours — or throws because it is not.
        _listener.Start().GetAwaiter().GetResult();
    }

    /// <summary>The URL a client connects to — <c>http://127.0.0.1:{port}/mcp</c>, with the path
    /// token appended when one is set.</summary>
    public Uri Endpoint { get; }

    private async Task OnSession(ITransport transport)
    {
        var http = SessionTransport(transport);
        var session = new McpSession(http.SessionId, http.Lifetime);
        Track(http);

        var builder = new ServerBuilder()
            .WithName(_options.ServerName)
            .WithVersion("1.0.0")
            .WithLogger(_loggerFactory)
            .WithStreamableHttpTransport(transport)
            .WithToolsCapability(Tools(session));
        if (_options.Instructions is { } instructions) builder.WithInstructions(instructions);
        if (_options.Prompts is { } prompts) builder.WithPromptsCapability(prompts);
        await builder.Build().Start();
    }

    // The listener's contract is that every session transport is an HttpServerTransport — the type
    // that carries the session id and the teardown token — but its callback is typed as the
    // transport interface. This is the one place that narrows it, and it fails loudly rather than
    // serve a session whose end nothing could observe.
    private static HttpServerTransport SessionTransport(ITransport transport) =>
        transport as HttpServerTransport
        ?? throw new InvalidOperationException($"Expected an {nameof(HttpServerTransport)} session, got {transport.GetType().Name}.");

    private void Track(HttpServerTransport http)
    {
        lock (_sessionsGate)
        {
            if (!_disposed)
            {
                _sessions.Add(http);
                http.Lifetime.Register(() => { lock (_sessionsGate) _sessions.Remove(http); });
                return;
            }
        }
        // Raced with Dispose: the listener no longer accepts, but this session got in first.
        _ = http.Stop();
    }

    private IToolsController Tools(McpSession session)
    {
        var tools = new McpToolSet(new DefaultToolsController(_json));
        foreach (var source in _options.ToolSources)
            source.Register(session, tools);
        return tools.Controller();
    }

    /// <summary>Stops listening and ends every open session, so a tool waiting on a person sees its
    /// <see cref="McpSession.Ended"/> fire instead of waiting for a client that will never answer.</summary>
    public void Dispose()
    {
        List<HttpServerTransport> sessions;
        lock (_sessionsGate)
        {
            if (_disposed) return;
            _disposed = true;
            sessions = [.. _sessions];
            _sessions.Clear();
        }
        _listener.Stop().GetAwaiter().GetResult();
        foreach (var session in sessions)
            session.Stop().GetAwaiter().GetResult();
    }
}

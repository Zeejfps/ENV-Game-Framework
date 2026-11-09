using System.Globalization;
using System.Net;
using ZGF.Gui.Mcp;

namespace ZGF.Gui.Immediate.Mcp;

/// <summary>Serves an immediate-mode app's <see cref="ImmediateGuiTools"/> over MCP, on 127.0.0.1. Each returns a
/// listening server; dispose it once the host's <c>Run</c> returns.</summary>
public static class ImmediateMcp
{
    private const int FirstDynamicPort = 49152;
    private const int PickAttempts = 64;

    /// <summary>Serves every command on an app launched in automation mode, at <c>http://127.0.0.1:{port}/mcp/{token}</c>:
    /// the port its launch flag gave, or one it picks and binds itself, then reports in the flag's file.</summary>
    /// <exception cref="ArgumentException">The automation runs in-process, with no endpoint.</exception>
    /// <exception cref="HttpListenerException">The port cannot be bound, or no free one was found.</exception>
    public static GuiMcpServer ServeAutomation(ImmediateAutomation automation)
    {
        var endpoint = automation.Options.Endpoint
            ?? throw new ArgumentException("Automation driven in-process has no endpoint to serve.", nameof(automation));
        if (!McpPathToken.TryParse(endpoint.Token, out var token))
            throw new ArgumentException("The automation token is not a URL path token.", nameof(automation));
        var tools = new ImmediateGuiTools(AutomationRunner.Driving(automation));
        switch (endpoint.Port)
        {
            case AutomationPort.Fixed fixedPort:
                return new GuiMcpServer(Options(fixedPort.Number, token, tools, "ZGF immediate automation"));
            case AutomationPort.Chosen chosen:
                var server = BindFree(token, tools);
                try
                {
                    Report(chosen.ReportFile, server.Endpoint.Port);
                }
                catch
                {
                    server.Dispose();
                    throw;
                }
                return server;
            default:
                throw new ArgumentOutOfRangeException(nameof(automation), endpoint.Port, null);
        }
    }

    /// <summary>The debug server for an app running as usual, as a retained app's <c>UseMcpServer</c> is: snapshot
    /// and screenshot alone, at <c>http://127.0.0.1:{port}/mcp</c>, or <c>/mcp/{token}</c> with a token. Nothing it
    /// serves injects input or moves focus, so it never takes the app over from the person using it.</summary>
    /// <exception cref="HttpListenerException">The port cannot be bound.</exception>
    public static GuiMcpServer ServeInspection(ImmediateInspection inspection, int port = 5577, McpPathToken? token = null)
    {
        var tools = new ImmediateGuiTools(AutomationRunner.Inspecting(inspection));
        return new GuiMcpServer(Options(port, token, tools, "ZGF immediate inspection"));
    }

    /// <summary>Binds a port from the dynamic range that nothing else holds. HttpListener cannot bind port 0, so it tries
    /// random ones; the one it returns is held, so the port it reports cannot be taken from under it.</summary>
    private static GuiMcpServer BindFree(McpPathToken token, ImmediateGuiTools tools)
    {
        HttpListenerException? last = null;
        for (var attempt = 0; attempt < PickAttempts; attempt++)
        {
            var port = Random.Shared.Next(FirstDynamicPort, 65536);
            try
            {
                return new GuiMcpServer(Options(port, token, tools, "ZGF immediate automation"));
            }
            catch (HttpListenerException taken)
            {
                last = taken;
            }
        }
        throw last ?? new HttpListenerException(0, "No port was tried.");
    }

    /// <summary>Writes <paramref name="port"/> to <paramref name="file"/> whole: to a file beside it first, then moved
    /// over it, so a reader never sees half of it. A write that fails leaves no partial file, and the caller closes
    /// the server it bound.</summary>
    private static void Report(string file, int port)
    {
        var partial = file + ".partial";
        try
        {
            File.WriteAllText(partial, port.ToString(CultureInfo.InvariantCulture));
            File.Move(partial, file, overwrite: true);
        }
        catch
        {
            if (File.Exists(partial))
                File.Delete(partial);
            throw;
        }
    }

    private static McpServerOptions Options(int port, McpPathToken? token, ImmediateGuiTools tools, string name) => new()
    {
        Port = port,
        PathToken = token,
        ServerName = name,
        Instructions = tools.Instructions,
        ToolSources = [tools],
    };
}

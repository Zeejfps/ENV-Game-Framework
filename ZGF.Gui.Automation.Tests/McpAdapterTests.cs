using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using McpSdk.Protocol.Models;
using Xunit.Abstractions;
using ZGF.Gui.Immediate;
using ZGF.Gui.Mcp;

namespace ZGF.Gui.Automation.Tests;

/// <summary>What crosses MCP between RemoteApp and an app's gui_* tools: bad arguments come back as structured
/// rejections, a failure inside the server is told apart from one, a cancelled call injects nothing, and an event
/// stream is read as it arrives.</summary>
public sealed class McpAdapterTests(ITestOutputHelper output)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    [SampleAppFact]
    public void Arguments_the_wire_cannot_use_come_back_as_structured_rejections()
    {
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);
        using var client = Session(app.Endpoint);

        foreach (var arguments in new[] { "{\"path\":\"add\",\"PATH\":\"draft\"}", "{\"x\":\"a\",\"y\":1}", "{\"x\":1e300,\"y\":0}", "{\"colour\":\"red\"}" })
        {
            var reply = client.CallTool("gui_click", arguments, Timeout);
            Assert.True(reply.IsError);
            var rejected = Assert.IsType<AutomationResult.Rejected>(ToolReplies.Read("gui_click", reply));
            Assert.StartsWith("gui_click:", rejected.Message);
        }
        Assert.Contains("3 tasks remaining", app.Snapshot());
    }

    [SampleAppFact]
    public void A_click_whose_client_gave_up_waiting_its_turn_is_never_injected()
    {
        using var app = RemoteApp.Launch(SampleApp.Launch, output.WriteLine);
        using var waiting = Session(app.Endpoint);
        using var impatient = Session(app.Endpoint);
        var wait = Task.Run(() => waiting.CallTool("gui_wait_for", "{\"text\":\"Never shown\",\"timeoutSeconds\":2}", Timeout));
        Thread.Sleep(300);

        Assert.Throws<TimeoutException>(() => impatient.CallTool("gui_click", "{\"path\":\"add-many\"}", TimeSpan.FromMilliseconds(300)));

        Assert.IsType<AutomationResult.Timeout>(ToolReplies.Read("gui_wait_for", wait.Result));
        app.Step(5);
        Assert.Contains("3 tasks remaining", app.Snapshot());
    }

    [Fact]
    public void A_tool_that_fails_inside_the_server_is_no_rejection()
    {
        using var server = new GuiMcpServer(new McpServerOptions { Port = FreePort(), ToolSources = [new FailingTools()] });
        using var client = Session(server.Endpoint);

        var reply = client.CallTool("gui_snapshot", "{}", Timeout);

        Assert.Null(reply.Structured);
        var failed = Assert.Throws<McpToolFailedException>(() => ToolReplies.Read("gui_snapshot", reply));
        Assert.Contains("boom", failed.Message);
    }

    [Fact]
    public void A_reply_whose_isError_disagrees_with_its_status_is_a_protocol_error()
    {
        using var ok = JsonDocument.Parse(AutomationWire.Write(new AutomationResult.Ok(1, new AutomationPayload.None())));
        using var rejected = JsonDocument.Parse(AutomationWire.Write(new AutomationResult.Rejected("no")));

        Assert.Throws<McpProtocolException>(() => ToolReplies.Read("t", new ToolCallReply(ok.RootElement, null, true, "")));
        Assert.Throws<McpProtocolException>(() => ToolReplies.Read("t", new ToolCallReply(rejected.RootElement, null, false, "")));
        Assert.Throws<McpProtocolException>(() => ToolReplies.Read("t", new ToolCallReply(null, null, false, "")));
    }

    [Fact]
    public void An_event_stream_answer_is_read_without_waiting_for_the_stream_to_close()
    {
        using var server = new StreamingServer();
        using var client = new McpHttpClient(server.Endpoint);
        client.Initialize(Timeout);
        Assert.Equal("2025-06-18", client.ProtocolVersion);
        var called = Stopwatch.StartNew();

        var reply = client.CallTool("echo", "{}", TimeSpan.FromSeconds(10));

        Assert.True(called.Elapsed < TimeSpan.FromSeconds(5), $"The call took {called.Elapsed}.");
        Assert.Equal("streamed", reply.Text);
        Assert.Equal("2025-06-18", server.LastProtocolVersionHeader);
    }

    private static McpHttpClient Session(Uri endpoint)
    {
        var client = new McpHttpClient(endpoint);
        client.Initialize(Timeout);
        return client;
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            if (listener.LocalEndpoint is not IPEndPoint bound)
                throw new InvalidOperationException("A TCP listener bound no IP endpoint.");
            return bound.Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private sealed class FailingTools : IMcpToolSource
    {
        public void Register(McpSession session, McpToolSet tools) =>
            tools.Add(McpTools.DefineUnchecked<int>("gui_snapshot", "Fails.", new ObjectSchema(), true,
                new ToolOutput<int>(new ObjectSchema(), (_, _) => { }), (_, _) => throw new InvalidOperationException("boom")));
    }

    /// <summary>Answers initialize as JSON, and a tool call as one event on a stream it then holds open for ten seconds.</summary>
    private sealed class StreamingServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _serving;

        public StreamingServer()
        {
            var port = FreePort();
            Endpoint = new Uri($"http://127.0.0.1:{port}/mcp/");
            _listener.Prefixes.Add(Endpoint.ToString());
            _listener.Start();
            _serving = Task.Run(Serve);
        }

        public Uri Endpoint { get; }
        public string? LastProtocolVersionHeader { get; private set; }

        private async Task Serve()
        {
            while (!_stop.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception e) when (e is HttpListenerException or ObjectDisposedException)
                {
                    return;
                }
                _ = Task.Run(() => Answer(context));
            }
        }

        private async Task Answer(HttpListenerContext context)
        {
            using var reader = new StreamReader(context.Request.InputStream);
            using var request = JsonDocument.Parse(await reader.ReadToEndAsync());
            var root = request.RootElement;
            var response = context.Response;
            if (!root.TryGetProperty("id", out var id))
            {
                response.StatusCode = 202;
                response.Close();
                return;
            }
            var method = root.GetProperty("method").GetString();
            if (method == "initialize")
            {
                response.Headers["Mcp-Session-Id"] = "session";
                response.ContentType = "application/json";
                var body = Encoding.UTF8.GetBytes($"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"protocolVersion\":\"2025-06-18\",\"capabilities\":{{}},\"serverInfo\":{{\"name\":\"s\",\"version\":\"1\"}}}}}}");
                await response.OutputStream.WriteAsync(body);
                response.Close();
                return;
            }
            LastProtocolVersionHeader = context.Request.Headers["MCP-Protocol-Version"];
            response.ContentType = "text/event-stream";
            response.SendChunked = true;
            var events = Encoding.UTF8.GetBytes(
                "event: message\ndata: {\"jsonrpc\":\"2.0\",\"method\":\"notifications/progress\",\"params\":{}}\n\n"
                + $"event: message\ndata: {{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"streamed\"}}]}}}}\n\n");
            await response.OutputStream.WriteAsync(events);
            await response.OutputStream.FlushAsync();
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), _stop.Token);
            }
            catch (OperationCanceledException)
            {
            }
            response.Abort();
        }

        public void Dispose()
        {
            _stop.Cancel();
            _listener.Stop();
            _serving.Wait(TimeSpan.FromSeconds(5));
            _listener.Close();
        }
    }
}

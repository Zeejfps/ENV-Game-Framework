using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using McpSdk.Adapter.System.Text.Json;
using McpSdk.Protocol;
using McpSdk.Protocol.Models;
using McpSdk.Server;
using ZGF.Gui.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Desktop.Automation;
using ZGF.Gui.Testing;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>Speaks Streamable HTTP to a real <see cref="GuiMcpServer"/> on a free port: what the
/// options put on the wire (name, instructions, tool set, gated path) and how a session's end and a
/// request's cancellation reach an app tool.</summary>
public class GuiMcpServerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Initialize_CarriesServerNameAndInstructions()
    {
        using var server = Start(new McpServerOptions
        {
            Port = FreePort(),
            ServerName = "Test App",
            Instructions = "Call echo first.",
            ToolSources = [new EchoSource()],
        });
        using var client = new McpClient(server.Endpoint);

        var init = await client.Initialize();

        Assert.Equal("Test App", init.GetProperty("serverInfo").GetProperty("name").GetString());
        Assert.Equal("Call echo first.", init.GetProperty("instructions").GetString());
    }

    [Fact]
    public async Task ToolsList_HasOnlyAppTools()
    {
        using var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [new EchoSource()] });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        var names = await client.ListToolNames();

        Assert.Equal(["echo"], names);
    }

    [Fact]
    public async Task ToolsList_IsEmpty_WithNoToolSources()
    {
        using var server = Start(new McpServerOptions { Port = FreePort() });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        var names = await client.ListToolNames();

        Assert.Empty(names);
    }

    [Fact]
    public async Task RetainedGuiTools_RegistersTheGuiToolSet()
    {
        using var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [new RetainedGuiTools(EmptyDriver())] });
        Assert.Equal("/mcp", server.Endpoint.AbsolutePath);
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        var names = await client.ListToolNames();

        Assert.Equal(["gui_click", "gui_key", "gui_move", "gui_screenshot", "gui_snapshot", "gui_type"], names.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GuiSnapshot_DeclaresOutputSchema_AndReturnsStructuredWindows()
    {
        using var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [new RetainedGuiTools(EmptyDriver())] });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        var tools = (await client.Request("tools/list", new { })).GetProperty("tools").EnumerateArray();
        var snapshot = tools.Single(t => t.GetProperty("name").GetString() == "gui_snapshot");
        Assert.Equal("array", snapshot.GetProperty("outputSchema").GetProperty("properties").GetProperty("windows").GetProperty("type").GetString());

        var json = await client.Call("gui_snapshot", new { format = "json" });
        Assert.Equal(JsonValueKind.Array, json.GetProperty("structuredContent").GetProperty("windows").ValueKind);
        Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(json.GetProperty("content")[0].GetProperty("text").GetString()!).RootElement.ValueKind);
        Assert.Equal("{\"windows\":[]}", json.GetProperty("content")[1].GetProperty("text").GetString());

        var text = await client.Call("gui_snapshot", new { });
        Assert.Equal(JsonValueKind.Array, text.GetProperty("structuredContent").GetProperty("windows").ValueKind);
        Assert.Equal("", text.GetProperty("content")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task GuiSnapshot_ServesADeeplyNestedTree_InBothFormats()
    {
        using var fonts = new FreeTypeFontBackend();
        var font = fonts.LoadFontFromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Inter-Regular.ttf"), 16);
        var driver = DriverFor(fonts, font, context =>
        {
            IWidget nested = new Text { Value = "leaf" };
            for (var i = 0; i < 100; i++) nested = new Box { Children = [nested] };
            return nested.BuildView(context);
        });
        using var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [new RetainedGuiTools(driver)] });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        foreach (var format in new[] { "json", "text" })
        {
            var result = await client.Call("gui_snapshot", new { format });

            Assert.False(result.TryGetProperty("isError", out var isError) && isError.GetBoolean(), result.ToString());
            var nodes = result.GetProperty("structuredContent").GetProperty("windows")[0].GetProperty("nodes");
            var leaf = nodes.EnumerateArray().Single(n => n.GetProperty("text").GetString() == "leaf");
            var depth = 0;
            for (var parent = leaf.GetProperty("parent").GetInt32(); parent >= 0; parent = nodes[parent].GetProperty("parent").GetInt32())
                depth++;
            Assert.True(depth >= 100, $"leaf depth {depth}");
            Assert.Contains("leaf", result.GetProperty("content")[0].GetProperty("text").GetString());
        }
    }

    [Fact]
    public async Task ToolCall_RejectsArgumentsThatDifferOnlyInCase()
    {
        using var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [new RetainedGuiTools(EmptyDriver())] });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        var result = await client.Call("gui_snapshot", JsonDocument.Parse("{\"format\":\"json\",\"FORMAT\":\"text\"}").RootElement);

        Assert.True(result.GetProperty("isError").GetBoolean());
        Assert.Equal("'FORMAT' is given more than once.", result.GetProperty("content")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task GuiClick_RejectsANonFiniteCoordinate_NamingIt()
    {
        using var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [new RetainedGuiTools(EmptyDriver())] });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        var result = await client.Call("gui_click", JsonDocument.Parse("{\"x\":1e300,\"y\":5}").RootElement);

        Assert.True(result.GetProperty("isError").GetBoolean());
        Assert.Equal("'x' must be a finite number.", result.GetProperty("content")[0].GetProperty("text").GetString());
    }

    [Fact]
    public void ToolArguments_RejectsTheWrongJsonType_NamingTheArgument()
    {
        var args = ToolArguments.Parse(new SystemJson().Parse("{\"S\":5,\"n\":\"10\",\"b\":\"yes\",\"ok\":\"v\"}"));

        Assert.Equal("v", args.Str("OK"));
        Assert.Equal("'s' must be a string.", Assert.Throws<InvalidOperationException>(() => args.Str("s")).Message);
        Assert.Equal("'n' must be a number.", Assert.Throws<InvalidOperationException>(() => args.Num("n")).Message);
        Assert.Equal("'b' must be a boolean.", Assert.Throws<InvalidOperationException>(() => args.Bool("b", true)).Message);
        Assert.Null(args.Num("absent"));
        Assert.True(args.Bool("absent", true));
    }

    [Fact]
    public async Task PathToken_GatesTheEndpoint()
    {
        var token = McpPathToken.Generate();
        using var server = Start(new McpServerOptions { Port = FreePort(), PathToken = token, ToolSources = [new EchoSource()] });
        Assert.Equal($"/mcp/{token.Value}", server.Endpoint.AbsolutePath);

        using var gated = new McpClient(server.Endpoint);
        var init = await gated.Initialize();
        Assert.Equal("ZGF GUI", init.GetProperty("serverInfo").GetProperty("name").GetString());

        using var bare = new McpClient(new Uri(server.Endpoint, "/mcp"));
        var status = await bare.PostRaw(McpClient.InitializeBody(1));
        Assert.Equal(HttpStatusCode.NotFound, status);
    }

    [Fact]
    public async Task ToolCall_RoundTripsArguments_AndSessionIdReachesTheTool()
    {
        var source = new EchoSource();
        using var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [source] });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        var result = await client.Call("echo", new { text = "hi" });

        Assert.Equal("hi", result.GetProperty("content")[0].GetProperty("text").GetString());
        Assert.Equal(client.SessionId, source.LastSession?.Id);
    }

    [Fact]
    public async Task CancelledNotification_CancelsInFlightToolCall()
    {
        var source = new WaitSource();
        using var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [source] });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        // The POST for a cancelled request stays open (McpSdk suppresses its response), so it is
        // deliberately not awaited: the assertion is that the tool observed the cancellation.
        _ = client.Call("wait", new { }, requestId: 7);
        await source.Entered.Task.WaitAsync(Timeout);
        await client.Notify("notifications/cancelled", new { requestId = 7 });

        Assert.True(await source.Left.Task.WaitAsync(Timeout));
    }

    [Fact]
    public async Task SessionDelete_EndsTheSession_WhileAToolIsWaiting()
    {
        var source = new WaitSource();
        using var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [source] });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();

        _ = client.Call("wait", new { });
        await source.Entered.Task.WaitAsync(Timeout);
        Assert.False(source.Session!.Ended.IsCancellationRequested);

        await client.Delete();

        Assert.True(await source.Left.Task.WaitAsync(Timeout));
        Assert.True(source.Session.Ended.IsCancellationRequested);
    }

    [Fact]
    public async Task Dispose_EndsOpenSessions()
    {
        var source = new WaitSource();
        var server = Start(new McpServerOptions { Port = FreePort(), ToolSources = [source] });
        using var client = new McpClient(server.Endpoint);
        await client.Initialize();
        _ = client.Call("wait", new { });
        await source.Entered.Task.WaitAsync(Timeout);

        server.Dispose();

        Assert.True(await source.Left.Task.WaitAsync(Timeout));
        Assert.True(source.Session!.Ended.IsCancellationRequested);
    }

    [Fact]
    public void BusyPort_ThrowsHttpListenerException()
    {
        var port = FreePort();
        using var first = Start(new McpServerOptions { Port = port });

        Assert.Throws<HttpListenerException>(() => Start(new McpServerOptions { Port = port }));
    }

    [Fact]
    public void PathToken_ParsesOnlyUnreservedCharacters()
    {
        Assert.True(McpPathToken.TryParse(McpPathToken.Generate().Value, out var generated));
        Assert.Equal(32, generated.Value.Length);
        Assert.True(McpPathToken.TryParse("a-b.c_d~E9", out _));
        Assert.False(McpPathToken.TryParse("", out _));
        Assert.False(McpPathToken.TryParse("has/slash", out _));
        Assert.False(McpPathToken.TryParse("has space", out _));
        Assert.False(McpPathToken.TryParse(new string('a', 129), out _));
    }

    // ---- fixtures ----

    private static GuiMcpServer Start(McpServerOptions options) => new(options);

    private static GuiDriver EmptyDriver() => new(() => [], new InlineDispatcher(), (_, _, _) => { });

    private static GuiDriver DriverFor(FreeTypeFontBackend fonts, FontHandle font, Func<Context, View> buildRoot)
    {
        var window = new FakeWindow(400, 300);
        var scale = new FakeUiScale(1f);
        var input = new DesktopInputSystem(window, scale);
        var context = new Context();
        context.AddService(input.InputSystem);
        var host = new GuiWindowHost(window, new CaptureCanvas(fonts, font), input, context, scale, sizeRootToWindow: true);
        host.SetRoot(buildRoot(context));
        var surface = new GuiSurface("main", window, host.Root, input, 1f);
        return new GuiDriver(() => [surface], new InlineDispatcher(), (_, _, _) => { });
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public void Post(Action action) => action();
    }

    private sealed class EchoSource : IMcpToolSource
    {
        public McpSession? LastSession { get; private set; }

        public void Register(McpSession session, DefaultToolsController tools)
        {
            LastSession = session;
            tools.AddTool(new EchoTool());
        }

        private sealed class EchoTool : IToolHandler
        {
            public Tool Tool { get; } = new("echo", "Echoes text.", new ObjectSchema().Add("text", new StringSchema { Description = "Text to echo." }));

            public Task<CallToolResult> Call(IJsonObject arguments, McpRequestContext context)
            {
                var text = arguments.First(kv => kv.Key == "text").Value.AsString();
                return Task.FromResult(CallToolResult.Ok(new TextContent(text)));
            }
        }
    }

    /// <summary>A tool that blocks until its request is cancelled or its session ends, reporting
    /// when it entered and whether it left through cancellation.</summary>
    private sealed class WaitSource : IMcpToolSource
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Left { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public McpSession? Session { get; private set; }

        public void Register(McpSession session, DefaultToolsController tools)
        {
            Session = session;
            tools.AddTool(new WaitTool(this, session));
        }

        private sealed class WaitTool(WaitSource owner, McpSession session) : IToolHandler
        {
            public Tool Tool { get; } = new("wait", "Waits for cancellation.", new ObjectSchema());

            public async Task<CallToolResult> Call(IJsonObject arguments, McpRequestContext context)
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, session.Ended);
                owner.Entered.SetResult();
                try
                {
                    await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, linked.Token);
                }
                catch (OperationCanceledException)
                {
                    owner.Left.TrySetResult(true);
                    throw;
                }
                owner.Left.TrySetResult(false);
                return CallToolResult.Ok(new TextContent("done"));
            }
        }
    }

    /// <summary>The client half of Streamable HTTP, enough to drive a session: initialize (capturing
    /// <c>Mcp-Session-Id</c>), then requests, notifications and the terminating DELETE.</summary>
    private sealed class McpClient : IDisposable
    {
        private const string ProtocolVersion = "2025-06-18";
        private readonly HttpClient _http = new() { Timeout = Timeout };
        private readonly Uri _endpoint;
        private int _nextId = 1;

        public McpClient(Uri endpoint) => _endpoint = endpoint;

        public string? SessionId { get; private set; }

        public static string InitializeBody(int id) => RequestBody(id, "initialize", new
        {
            protocolVersion = ProtocolVersion,
            capabilities = new { },
            clientInfo = new { name = "test", version = "1" },
        });

        private static string RequestBody(int id, string method, object @params) =>
            JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params });

        private static string NotificationBody(string method, object @params) =>
            JsonSerializer.Serialize(new { jsonrpc = "2.0", method, @params });

        public async Task<JsonElement> Initialize()
        {
            var id = _nextId++;
            using var response = await Send(InitializeBody(id));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            SessionId = response.Headers.GetValues("Mcp-Session-Id").Single();
            var result = Result(await response.Content.ReadAsStringAsync());
            await Notify("notifications/initialized", new { });
            return result;
        }

        public async Task<IReadOnlyList<string>> ListToolNames()
        {
            var result = await Request("tools/list", new { });
            return result.GetProperty("tools").EnumerateArray().Select(t => t.GetProperty("name").GetString()!).ToList();
        }

        public Task<JsonElement> Call(string tool, object arguments, int? requestId = null) =>
            Request("tools/call", new { name = tool, arguments }, requestId);

        public async Task<JsonElement> Request(string method, object @params, int? requestId = null)
        {
            var id = requestId ?? _nextId++;
            using var response = await Send(RequestBody(id, method, @params));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return Result(await response.Content.ReadAsStringAsync());
        }

        public async Task Notify(string method, object @params)
        {
            using var response = await Send(NotificationBody(method, @params));
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }

        public async Task<HttpStatusCode> PostRaw(string body)
        {
            using var response = await Send(body);
            return response.StatusCode;
        }

        public async Task Delete()
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, _endpoint);
            AddSessionHeaders(request);
            using var response = await _http.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private Task<HttpResponseMessage> Send(string body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            AddSessionHeaders(request);
            return _http.SendAsync(request);
        }

        private void AddSessionHeaders(HttpRequestMessage request)
        {
            if (SessionId is null) return;
            request.Headers.Add("Mcp-Session-Id", SessionId);
            request.Headers.Add("MCP-Protocol-Version", ProtocolVersion);
        }

        // A JSON-RPC error is a test failure with the server's message, not a missing-property KeyNotFound.
        private static JsonElement Result(string json)
        {
            var doc = JsonDocument.Parse(json).RootElement;
            if (doc.TryGetProperty("error", out var error))
                Assert.Fail($"JSON-RPC error: {error}");
            return doc.GetProperty("result");
        }

        public void Dispose() => _http.Dispose();
    }
}

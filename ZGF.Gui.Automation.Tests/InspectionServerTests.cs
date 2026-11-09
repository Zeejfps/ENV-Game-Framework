using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Mcp;
using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Automation.Tests;

/// <summary>The inspection-only debug server of an app running as usual, mid-interaction: a text field focused with
/// its caret, and a popup open. A client reads it, and every tool that would drive it is unsupported, the interaction
/// left as it was.</summary>
public sealed class InspectionServerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>One of every command that drives an app, whichever verb it goes as.</summary>
    private static readonly AutomationCommand[] Driving =
    [
        new AutomationCommand.MovePointer(new WindowPoint(10f, 10f)),
        new AutomationCommand.MoveTo(Selector.AtPath("name")),
        new AutomationCommand.PointerDown(),
        new AutomationCommand.PointerUp(),
        new AutomationCommand.Click(new WindowPoint(10f, 10f)),
        new AutomationCommand.ClickOn(Selector.AtPath("name")),
        new AutomationCommand.Drag(new WindowPoint(1f, 1f), new WindowPoint(50f, 50f)),
        new AutomationCommand.Scroll(0, -3),
        new AutomationCommand.Key(KeyboardKey.Escape),
        new AutomationCommand.TypeText("typed"),
        new AutomationCommand.Compose("か"),
        new AutomationCommand.WaitFor(Selector.AtPath("name"), TimeSpan.FromSeconds(1)),
        new AutomationCommand.Step(),
        new AutomationCommand.Advance(1),
        new AutomationCommand.Settle(),
        new AutomationCommand.WaitUntilReady(TimeSpan.FromSeconds(1)),
        new AutomationCommand.QueueFilePicker(null),
        new AutomationCommand.FilePickerRequests(),
        new AutomationCommand.GetClipboard(),
        new AutomationCommand.SetClipboard("x"),
        new AutomationCommand.SystemMenuItems(),
        new AutomationCommand.PickSystemMenuItem(["File", "Open"]),
        new AutomationCommand.Effects(),
        new AutomationCommand.Close(),
    ];

    private sealed class LiveApp() : ImmediateGuiApp<DefaultUiState>(new DefaultUiState())
    {
        private bool _focused;
        public string Name = "half-typed";
        public volatile int FocusLost;
        public volatile int Frames;

        protected override void OnUpdate(Ui<DefaultUiState> ui)
        {
            Frames++;
            using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Padding = 8f, Gap = 8f }))
            {
                ui.TextField("name", ref Name, width: Sizing.Fixed(200f));
                ui.Label("Live");
            }
            var field = ui.GetId("name");
            if (!_focused)
            {
                ui.Focus(field);
                _focused = true;
            }
            else if (!ui.HasFocus(field))
            {
                FocusLost++;
            }
            using (ui.Popup(ui.GetId("menu"), new UiRect(20f, 60f, 10f, 10f)))
                ui.Label("In popup");
        }
    }

    [LiveHostFact]
    public void Inspection_reads_a_live_interaction_and_every_driving_tool_leaves_it_alone()
    {
        var names = Driving.Select(c => AutomationWire.Write(c).Name).ToHashSet();
        Assert.Equal(AutomationWire.All.Where(v => !v.Inspection).Select(v => v.Name).Order(), names.Order());
        var app = new LiveApp();
        using var live = LiveHost.Start(app);
        using var client = new McpHttpClient(live.Endpoint);
        client.Initialize(Timeout);

        var capabilities = Ok<AutomationPayload.Capabilities>(Call(client, new AutomationCommand.Capabilities()));
        Assert.Equal(AutomationWire.ProtocolVersion, capabilities.Version);
        Assert.Equal(AutomationCapabilitySet.Inspection, capabilities.Set);
        Assert.Equal(["gui_capabilities", "gui_snapshot", "gui_screenshot"], capabilities.Commands);
        var before = Ok<AutomationPayload.Snapshot>(Call(client, new AutomationCommand.Snapshot())).Text;
        Assert.Contains("value=\"half-typed\" focused", before);
        Assert.Contains("# surface popup", before);

        var capture = Call(client, new AutomationCommand.Capture(new CaptureTarget.Composite()));
        var image = Ok<AutomationPayload.Image>(capture);
        var frame = Assert.IsType<AutomationResult.Ok>(capture).Frame;
        Assert.Contains($"in frame {frame},", image.Text);
        Assert.Contains($"# frame {frame}:", image.Text);
        Assert.Equal(0x89, image.Png[0]);

        foreach (var command in Driving)
            Assert.IsType<AutomationResult.Unsupported>(Call(client, command));
        Assert.IsType<AutomationResult.Unsupported>(Call(client, new AutomationCommand.Capture(new CaptureTarget.Element(Selector.AtPath("name")))));
        Assert.IsType<AutomationResult.Unsupported>(Call(client, new AutomationCommand.Capture(new CaptureTarget.Surface(1))));

        var after = Ok<AutomationPayload.Snapshot>(Call(client, new AutomationCommand.Snapshot())).Text;
        Assert.Contains("value=\"half-typed\" focused", after);
        Assert.Contains("# surface popup", after);
        Assert.Equal("half-typed", app.Name);
        Assert.Equal(0, app.FocusLost);
        Assert.True(app.Frames > 1);
    }

    private static T Ok<T>(AutomationResult result) where T : AutomationPayload =>
        Assert.IsType<T>(Assert.IsType<AutomationResult.Ok>(result).Payload);

    private static AutomationResult Call(McpHttpClient client, AutomationCommand command)
    {
        var (name, arguments) = AutomationWire.Write(command);
        return ToolReplies.Read(name, client.CallTool(name, arguments, Timeout));
    }

    /// <summary>A host running as usual, its window shown as an app's is, on a thread of its own, serving its inspection
    /// on a free port.</summary>
    private sealed class LiveHost : IDisposable
    {
        private readonly Thread _thread;
        private readonly ImmediateGuiAppHost<DefaultUiState> _host;
        private readonly ZGF.Gui.Mcp.GuiMcpServer _server;

        private LiveHost(LiveApp app)
        {
            var started = new TaskCompletionSource<ImmediateGuiAppHost<DefaultUiState>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _thread = new Thread(() =>
            {
                try
                {
                    var host = new ImmediateGuiAppHost<DefaultUiState>(app,
                        new StartupConfig { WindowWidth = 320, WindowHeight = 200, WindowTitle = "Inspection test" });
                    started.SetResult(host);
                    host.Run();
                }
                catch (Exception e)
                {
                    started.TrySetException(e);
                }
            }) { IsBackground = true, Name = "Live UI thread" };
            if (OperatingSystem.IsWindows())
                _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            _host = started.Task.Result;
            var inspection = _host.Inspection ?? throw new InvalidOperationException("A host running as usual has no inspection.");
            _server = Serve(inspection);
        }

        public Uri Endpoint => _server.Endpoint;

        public static LiveHost Start(LiveApp app) => new(app);

        private static ZGF.Gui.Mcp.GuiMcpServer Serve(ImmediateInspection inspection)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    return ImmediateMcp.ServeInspection(inspection, Random.Shared.Next(49152, 65536));
                }
                catch (System.Net.HttpListenerException) when (attempt < 32)
                {
                }
            }
        }

        public void Dispose()
        {
            _server.Dispose();
            _host.Dispatcher.Post(_host.CloseImmediately);
            Assert.True(_thread.Join(Timeout), "The live host did not close.");
        }
    }
}

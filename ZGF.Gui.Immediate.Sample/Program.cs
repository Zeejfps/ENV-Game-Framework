using System.Globalization;
using System.Net;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Mcp;
using ZGF.Gui.Immediate.Sample;
using ZGF.Gui.Mcp;

internal static class Program
{
    private const string InspectFlag = "--zgf-inspect";

    [STAThread]
    private static int Main(string[] args)
    {
        var launch = AutomationOptions.Parse(args);
        if (launch is AutomationLaunch.Malformed malformed)
        {
            Console.Error.WriteLine(malformed.Reason);
            return 2;
        }
        var automation = launch is AutomationLaunch.Automated automated ? automated.Options : null;
        var inspectPort = InspectPort(args);
        if (inspectPort == 0)
        {
            Console.Error.WriteLine($"{InspectFlag} takes a port from 1 to 65535.");
            return 2;
        }

        var state = new TodoState { DarkMode = !args.Contains("--light") };
        state.Add("Sketch the immediate-mode API");
        state.Add("Render through the canvas");
        state.Add("Decide if this beats the retained tree");

        var app = new TodoApp(state);
        var host = new ImmediateGuiAppHost<DefaultUiState>(
            app,
            new StartupConfig
            {
                WindowWidth = 520,
                WindowHeight = 760,
                WindowTitle = "Immediate Mode Prototype",
            },
            args.Contains("--gl") ? RenderBackendKind.OpenGl : RenderBackendKind.Auto,
            automation);

        GuiMcpServer? server;
        try
        {
            server = Serve(host, inspectPort);
        }
        catch (HttpListenerException e)
        {
            Console.Error.WriteLine($"The MCP server could not start: {e.Message}");
            return 3;
        }
        using (server)
        {
            if (server != null)
                Console.WriteLine($"MCP listening on {server.Endpoint.GetLeftPart(UriPartial.Authority)}");
            host.Run();
        }
        return 0;
    }

    private static GuiMcpServer? Serve(ImmediateGuiAppHost<DefaultUiState> host, int? inspectPort)
    {
        if (host.Automation != null)
            return ImmediateMcp.ServeAutomation(host.Automation);
        if (inspectPort == null || host.Inspection == null)
            return null;
        return ImmediateMcp.ServeInspection(host.Inspection, inspectPort.Value);
    }

    // --zgf-inspect <port>: the inspection-only debug server, for an agent to look at the app as it runs; 0 when malformed.
    private static int? InspectPort(string[] args)
    {
        var at = Array.IndexOf(args, InspectFlag);
        if (at < 0)
            return null;
        if (at + 1 >= args.Length
            || !int.TryParse(args[at + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            || port is < 1 or > 65535)
            return 0;
        return port;
    }
}

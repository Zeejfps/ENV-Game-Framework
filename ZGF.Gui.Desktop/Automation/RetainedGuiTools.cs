using McpSdk.Protocol.Models;
using McpSdk.Server;
using ZGF.Gui.Desktop.Inspection;

namespace ZGF.Gui.Desktop.Automation;

/// <summary>
/// The <c>gui_*</c> debug tools over a retained app's <see cref="GuiDriver"/>: read the laid-out view
/// trees of every live window, inject mouse and keyboard input, and capture screenshots. Each tool
/// marshals onto the UI thread and blocks for the result. They let a client click and type into the
/// live window, so a server a user turns on from a preference does not include them.
/// </summary>
public sealed class RetainedGuiTools(GuiDriver driver) : IMcpToolSource
{
    private static readonly ToolOutput<MultiWindowSnapshot> SnapshotOutput = new(
        new ObjectSchema().Add("windows", new ArraySchema("object")
        {
            Description = "Every live window as { role, bounds: { x, y, w, h }, scale, focused, nodes }; nodes lists the view tree in pre-order, each node with the index of its parent (-1 for the root).",
        }),
        (writer, snapshot) =>
        {
            writer.WritePropertyName("windows");
            snapshot.WriteFlatJson(writer);
        });

    public void Register(McpSession session, DefaultToolsController tools)
    {
        tools.AddTool(McpTools.Define(
            "gui_snapshot",
            "Read the laid-out view tree of every live window — the main window plus any open context menu, tooltip, or secondary window — each under a \"=== window: ROLE [x,y wxh] ===\" header. Call this first to discover what is on screen (incl. context menus, which are separate windows) and how to target it.",
            new ObjectSchema().AddOption("format", new StringSchema
            {
                Description = "\"text\" (default, compact and human-readable) or \"json\" (machine-readable).",
                Options = ["text", "json"],
            }),
            readOnly: true,
            SnapshotOutput,
            a =>
            {
                var asJson = string.Equals(a.Str("format"), "json", StringComparison.OrdinalIgnoreCase);
                var snapshot = driver.SnapshotTool();
                return new ToolReply<MultiWindowSnapshot>(snapshot, snapshot.Render(asJson));
            }));

        tools.AddTool(McpTools.Define(
            "gui_screenshot",
            "Capture a PNG screenshot of a rendered window. Defaults to the topmost open context menu if one is open, else the main window; pass \"window\" (a role like \"context-menu\" or \"main\", or a snapshot window index) to target another. Returns image content, or an error if the active render backend cannot read back its framebuffer.",
            new ObjectSchema()
                .AddOption("window", new StringSchema { Description = "Window to capture: a role (\"main\", \"context-menu\", \"tooltip\", \"secondary\") or a snapshot window index. Defaults to the topmost context menu, else main." }),
            readOnly: true,
            a => McpTools.Image(driver.ScreenshotTool(a.Str("window")))));

        tools.AddTool(McpTools.Define(
            "gui_click",
            "Click a view in any live window (searches open context menus first, then secondary windows, then main). Target it by \"id\", \"label\", or \"text\", or click absolute GUI coordinates with \"x\" and \"y\" (in the \"window\" you name, default main).",
            new ObjectSchema()
                .AddOption("id", new StringSchema { Description = "View id to click." })
                .AddOption("label", new StringSchema { Description = "Clickable label / accessible name to match." })
                .AddOption("text", new StringSchema { Description = "Visible text to match." })
                .AddOption("exact", new BooleanSchema { Description = "Match id/label/text exactly (default true); false matches substrings." })
                .AddOption("button", new StringSchema { Description = "Mouse button (default left).", Options = ["left", "right", "middle"] })
                .AddOption("x", new NumberSchema { Description = "Absolute GUI x (use with y to click a coordinate)." })
                .AddOption("y", new NumberSchema { Description = "Absolute GUI y (use with x to click a coordinate)." })
                .AddOption("window", new StringSchema { Description = "For x/y clicks, which window's coordinate space: a role or snapshot index (default \"main\")." }),
            readOnly: false,
            a => McpTools.Text(driver.ClickTool(
                a.Str("id"), a.Str("label"), a.Str("text"),
                a.Bool("exact", true), a.Num("x"), a.Num("y"), a.Str("button"), a.Str("window")))));

        tools.AddTool(McpTools.Define(
            "gui_move",
            "Move the pointer without clicking, so hover behaviour can be driven: tooltips, hover cards, cursor changes. Target a view by \"id\", \"label\" or \"text\", or absolute GUI coordinates with \"x\" and \"y\". Anything that waits for the pointer to settle needs a moment after this before it appears.",
            new ObjectSchema()
                .AddOption("id", new StringSchema { Description = "View id to move onto." })
                .AddOption("label", new StringSchema { Description = "Clickable label / accessible name to match." })
                .AddOption("text", new StringSchema { Description = "Visible text to match." })
                .AddOption("exact", new BooleanSchema { Description = "Match id/label/text exactly (default true); false matches substrings." })
                .AddOption("x", new NumberSchema { Description = "Absolute GUI x (use with y)." })
                .AddOption("y", new NumberSchema { Description = "Absolute GUI y (use with x)." })
                .AddOption("window", new StringSchema { Description = "For x/y moves, which window's coordinate space: a role or snapshot index (default \"main\")." }),
            readOnly: false,
            a => McpTools.Text(driver.MoveTool(
                a.Num("x"), a.Num("y"), a.Str("id"), a.Str("label"), a.Str("text"),
                a.Bool("exact", true), a.Str("window")))));

        tools.AddTool(McpTools.Define(
            "gui_type",
            "Type ASCII text into the focused view, one key at a time. Use gui_key for non-printable keys (Enter, Tab, ...).",
            new ObjectSchema().Add("text", new StringSchema { Description = "ASCII text to type." }),
            readOnly: false,
            a => McpTools.Text(driver.TypeTool(a.RequiredStr("text")))));

        tools.AddTool(McpTools.Define(
            "gui_key",
            "Press a single key, optionally with modifiers — e.g. Enter, Escape, Tab, or A with Control.",
            new ObjectSchema()
                .Add("key", new StringSchema { Description = "Key name (e.g. Enter, Escape, Tab, A, Left)." })
                .AddOption("mods", new StringSchema { Description = "Comma-separated modifiers (e.g. \"Control,Shift\")." })
                .AddOption("action", new StringSchema { Description = "press (default), down, or up.", Options = ["press", "down", "up"] }),
            readOnly: false,
            a => McpTools.Text(driver.KeyTool(a.RequiredStr("key"), a.Str("mods"), a.Str("action")))));
    }
}

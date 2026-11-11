using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

public enum WireArgKind
{
    String,
    Number,
    Integer,
    Boolean,
    StringList,
}

/// <summary>An argument a command takes on the wire.</summary>
public sealed record WireArg(string Name, WireArgKind Kind, bool Required, string Description, IReadOnlyList<string>? Options = null);

/// <summary>A command as the wire names it: what it takes, whether it only reads, and whether an inspection-only server
/// takes it.</summary>
public sealed record WireVerb(string Name, string Description, bool ReadOnly, bool Inspection, IReadOnlyList<WireArg> Args);

/// <summary>What reading a command off the wire came to.</summary>
public abstract record WireCommand
{
    private WireCommand()
    {
    }

    public sealed record Parsed(AutomationCommand Command) : WireCommand;

    public sealed record Rejected(string Reason) : WireCommand;
}

/// <summary>
/// The JSON form of <see cref="AutomationCommand"/> and <see cref="AutomationResult"/>, one shape for both ends: a
/// command is a verb name and an object of arguments, a result an object with a <c>status</c>, and an image result's
/// PNG travels beside it. Reading either checks every value and rejects what it does not know.
/// </summary>
public static class AutomationWire
{
    /// <summary>Raised when the wire contract changes in a way an older peer would misread.</summary>
    public const int ProtocolVersion = 1;

    private const string PathArg = "path";
    private const string RoleArg = "role";
    private const string LabelArg = "label";
    private const string TextArg = "text";
    private const string XArg = "x";
    private const string YArg = "y";
    private const string SurfaceArg = "surface";
    private const string FramesArg = "frames";
    private const string ButtonArg = "button";
    private const string ModsArg = "mods";
    private const string TimeoutArg = "timeoutSeconds";

    private static readonly string[] Buttons = Enum.GetNames<PointerButton>().Select(n => n.ToLowerInvariant()).ToArray();
    private static readonly string[] Roles = Enum.GetValues<SemanticRole>().Select(SemanticText.RoleName).ToArray();

    private static readonly WireArg[] SelectorArgs =
    [
        new(PathArg, WireArgKind.String, false, "The node's test path, as the snapshot writes it (e.g. \"menu/file\" or \"items/#3/done\")."),
        new(RoleArg, WireArgKind.String, false, "With label: the node's role.", Roles),
        new(LabelArg, WireArgKind.String, false, "With role: the node's exact label."),
        new(TextArg, WireArgKind.String, false, "The innermost node labelled exactly this."),
    ];

    private static readonly WireArg[] PointArgs =
    [
        new(XArg, WireArgKind.Number, false, "UI points from the window's left."),
        new(YArg, WireArgKind.Number, false, "UI points from the window's top."),
        new(SurfaceArg, WireArgKind.Integer, false, "The window x and y are in: 0, the default, is the main window, then each popup and window as the snapshot lists its surfaces."),
    ];

    private static readonly WireArg Frames = new(FramesArg, WireArgKind.Integer, false, "Frames to run after queueing the input, without moving the clock (default 1; 0 only queues).");
    private static readonly WireArg Button = new(ButtonArg, WireArgKind.String, false, "Pointer button (default left).", Buttons);
    private static readonly WireArg Mods = new(ModsArg, WireArgKind.String, false, "Comma-separated modifiers held: Shift, Control, Alt, Super.");
    private static readonly WireArg Timeout = new(TimeoutArg, WireArgKind.Number, true, "Real seconds to wait at most.");

    private sealed record Verb(WireVerb Wire, Func<Args, AutomationCommand> Read);

    private static readonly Verb[] Verbs =
    [
        new(new("gui_capabilities", "The protocol version, whether this server drives the app or only inspects it, and the tools it takes.", true, true, []),
            _ => new AutomationCommand.Capabilities()),
        new(new("gui_snapshot", "The last frame as text: every window and popup, then each node's role, label, value, states and [x,y wxh] bounds in main-window UI points, with its test path. Call this first.", true, true, []),
            _ => new AutomationCommand.Snapshot()),
        new(new("gui_screenshot", "A PNG of the last frame: the main window with every popup and window over it, or one surface alone, or the pixels around one node (name it as gui_click does), at the frame's own scale. Inspection-only servers capture the main window alone.", true, true,
            [.. SelectorArgs,
             new(SurfaceArg, WireArgKind.Integer, false, "A surface to capture alone, as the snapshot lists them."),
             new("margin", WireArgKind.Number, false, "Points around the node to include (default 8)."),
             new("magnify", WireArgKind.Integer, false, "Scale the crop up, nearest-neighbour, 1 to 16 (default 1).")]),
            ReadCapture),
        new(new("gui_move", "Move the pointer, so hover shows: onto a node (path, role and label, or text), at a point a press would reach it, or to x and y. Runs a frame after.", false, false,
            [.. SelectorArgs, .. PointArgs, Frames]),
            ReadMove),
        new(new("gui_press", "Press a pointer button where the pointer is.", false, false, [Button, Mods, Frames]),
            a => new AutomationCommand.PointerDown(a.Button(), a.Mods(), a.Frames())),
        new(new("gui_release", "Release a pointer button where the pointer is.", false, false, [Button, Mods, Frames]),
            a => new AutomationCommand.PointerUp(a.Button(), a.Mods(), a.Frames())),
        new(new("gui_click", "Click a node (path, role and label, or text) where a press reaches it, checked through the real hit test: a node covered, behind a modal or cut off is reported, not clicked. Or click x and y.", false, false,
            [.. SelectorArgs, .. PointArgs, Button, Mods]),
            ReadClick),
        new(new("gui_drag", "Press at fromX,fromY, move to toX,toY in steps a frame apart, and release there; for splitters, sliders and drag and drop.", false, false,
            [new("fromX", WireArgKind.Number, true, "Where the press is."), new("fromY", WireArgKind.Number, true, "Where the press is."),
             new("toX", WireArgKind.Number, true, "Where the release is."), new("toY", WireArgKind.Number, true, "Where the release is."),
             new(SurfaceArg, WireArgKind.Integer, false, "The window both points are in (default 0, the main window)."),
             new("steps", WireArgKind.Integer, false, "Moves from one to the other, a frame apart (default 4)."), Button, Mods]),
            ReadDrag),
        new(new("gui_scroll", "Scroll the wheel where the pointer is: positive dy scrolls up.", false, false,
            [new("dx", WireArgKind.Number, false, "Horizontal wheel steps."), new("dy", WireArgKind.Number, false, "Vertical wheel steps."), Frames]),
            a => new AutomationCommand.Scroll(a.Number("dx") ?? 0, a.Number("dy") ?? 0, a.Frames())),
        new(new("gui_key", "Press a key, optionally with modifiers: Enter, Escape, Tab, A with Control.", false, false,
            [new("key", WireArgKind.String, true, "Key name, as ZGF's KeyboardKey names it (Enter, Escape, Tab, A, LeftArrow, Alpha1)."), Mods,
             new("action", WireArgKind.String, false, "press (default), down or up.", ["press", "down", "up"]), Frames]),
            a => new AutomationCommand.Key(a.Named<KeyboardKey>("key"), a.Mods(), a.OptionalNamed("action", KeyAction.Press), a.Frames())),
        new(new("gui_type", "Type text into the focused field, as committed characters.", false, false,
            [new(TextArg, WireArgKind.String, true, "The text to type."), Frames]),
            a => new AutomationCommand.TypeText(a.RequiredString(TextArg), a.Frames())),
        new(new("gui_compose", "Set the IME's composition to text, caret at caret (the end by default); empty text ends it.", false, false,
            [new(TextArg, WireArgKind.String, true, "The composition."), new("caret", WireArgKind.Integer, false, "Where its caret is."), Frames]),
            a => new AutomationCommand.Compose(a.RequiredString(TextArg), a.Integer("caret"), a.Frames())),
        new(new("gui_wait_for", "Run frames until exactly one node matches (path, role and label, or text), or the timeout passes.", false, false,
            [.. SelectorArgs, Timeout]),
            a => new AutomationCommand.WaitFor(a.Target() ?? throw a.Reject("Name the node: path, role and label, or text."), a.Timeout())),
        new(new("gui_step", "Run frames without moving the virtual clock.", false, false,
            [new("count", WireArgKind.Integer, false, "Frames to run (default 1).")]),
            a => new AutomationCommand.Step(a.Integer("count") ?? 1)),
        new(new("gui_advance", "Move the virtual clock on, a frame every 1/60 s: animations and timers move.", false, false,
            [new("seconds", WireArgKind.Number, true, "Virtual seconds, 0 to 600.")]),
            a => new AutomationCommand.Advance(a.RequiredNumber("seconds"))),
        new(new("gui_settle", "Run frames 1/60 s apart until animations and scheduled updates have run out.", false, false,
            [new("maxSeconds", WireArgKind.Number, false, "Virtual seconds to run at most (default 5).")]),
            a => new AutomationCommand.Settle(a.Number("maxSeconds") ?? 5)),
        new(new("gui_wait_until_ready", "Run frames until posted work, requested updates and the app's tracked background work are done.", false, false,
            [Timeout]),
            a => new AutomationCommand.WaitUntilReady(a.Timeout())),
        new(new("gui_file_picker", "Answer the next file picker the app opens with a path, or cancel it. Pickers with no answer queued cancel.", false, false,
            [new("answer", WireArgKind.String, false, "The path the picker returns."), new("cancel", WireArgKind.Boolean, false, "true to cancel it instead.")]),
            ReadFilePicker),
        new(new("gui_file_picker_requests", "Every file picker the app opened, in order, and how each was answered.", true, false, []),
            _ => new AutomationCommand.FilePickerRequests()),
        new(new("gui_clipboard_get", "The app's clipboard: its text, and its HTML if it was set.", true, false, []),
            _ => new AutomationCommand.GetClipboard()),
        new(new("gui_clipboard_set", "Put text on the app's clipboard.", false, false,
            [new(TextArg, WireArgKind.String, true, "The text.")]),
            a => new AutomationCommand.SetClipboard(a.RequiredString(TextArg))),
        new(new("gui_menu_items", "The items of the system menu bar, where the app has one (macOS); empty elsewhere.", true, false, []),
            _ => new AutomationCommand.SystemMenuItems()),
        new(new("gui_menu_pick", "Pick a system menu bar item as clicking it would.", false, false,
            [new(PathArg, WireArgKind.StringList, true, "The titles of the menus leading to it, then its own.")]),
            a => new AutomationCommand.PickSystemMenuItem(a.RequiredStrings(PathArg))),
        new(new("gui_effects", "Every platform effect the app asked for, recorded instead of performed: windows shown or brought to the front, files, folders and URLs opened.", true, false, []),
            _ => new AutomationCommand.Effects()),
        new(new("gui_close", "Close the app as its close button would, without asking it.", false, false, []),
            _ => new AutomationCommand.Close()),
    ];

    private static readonly Dictionary<string, Verb> ByName = Verbs.ToDictionary(v => v.Wire.Name, StringComparer.Ordinal);

    /// <summary>Every command the wire knows, for a server to describe.</summary>
    public static IReadOnlyList<WireVerb> All { get; } = Verbs.Select(v => v.Wire).ToArray();

    /// <summary>The commands a server of <paramref name="set"/> takes.</summary>
    public static IReadOnlyList<string> Names(AutomationCapabilitySet set) =>
        All.Where(v => set == AutomationCapabilitySet.Automation || v.Inspection).Select(v => v.Name).ToArray();

    /// <summary>Reads verb <paramref name="name"/>'s <paramref name="arguments"/>, a JSON object.</summary>
    public static WireCommand Parse(string name, JsonElement arguments)
    {
        if (!ByName.TryGetValue(name, out var verb))
            return new WireCommand.Rejected($"There is no command \"{name}\".");
        try
        {
            return new WireCommand.Parsed(verb.Read(Args.Of(verb.Wire, arguments)));
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            return new WireCommand.Rejected($"{name}: {e.Message}");
        }
    }

    public static WireCommand Parse(string name, string argumentsJson)
    {
        try
        {
            using var document = JsonDocument.Parse(argumentsJson);
            return Parse(name, document.RootElement);
        }
        catch (JsonException e)
        {
            return new WireCommand.Rejected($"{name}: the arguments are not JSON: {e.Message}");
        }
    }

    /// <summary>The verb <paramref name="command"/> goes as, and its arguments as a JSON object.</summary>
    public static (string Name, string Arguments) Write(AutomationCommand command)
    {
        var buffer = new ArrayBufferWriter<byte>();
        string name;
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            name = WriteArguments(w, command);
            w.WriteEndObject();
        }
        return (name, Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    private static string WriteArguments(Utf8JsonWriter w, AutomationCommand command)
    {
        switch (command)
        {
            case AutomationCommand.Capabilities:
                return "gui_capabilities";
            case AutomationCommand.Snapshot:
                return "gui_snapshot";
            case AutomationCommand.Capture capture:
                switch (capture.Target)
                {
                    case CaptureTarget.Composite:
                        break;
                    case CaptureTarget.Surface surface:
                        w.WriteNumber(SurfaceArg, surface.Index);
                        break;
                    case CaptureTarget.Element element:
                        WriteSelector(w, element.Selector);
                        w.WriteNumber("margin", element.Margin);
                        w.WriteNumber("magnify", element.Magnify);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command), capture.Target, null);
                }
                return "gui_screenshot";
            case AutomationCommand.MovePointer move:
                WritePoint(w, move.At);
                w.WriteNumber(FramesArg, move.Frames);
                return "gui_move";
            case AutomationCommand.MoveTo moveTo:
                WriteSelector(w, moveTo.Selector);
                return "gui_move";
            case AutomationCommand.PointerDown down:
                WriteButton(w, down.Button, down.Modifiers);
                w.WriteNumber(FramesArg, down.Frames);
                return "gui_press";
            case AutomationCommand.PointerUp up:
                WriteButton(w, up.Button, up.Modifiers);
                w.WriteNumber(FramesArg, up.Frames);
                return "gui_release";
            case AutomationCommand.Click click:
                WritePoint(w, click.At);
                WriteButton(w, click.Button, click.Modifiers);
                return "gui_click";
            case AutomationCommand.ClickOn clickOn:
                WriteSelector(w, clickOn.Selector);
                WriteButton(w, clickOn.Button, clickOn.Modifiers);
                return "gui_click";
            case AutomationCommand.Drag drag:
                if (drag.From.Surface != drag.To.Surface)
                    throw new ArgumentException("A drag's two points are in one window.", nameof(command));
                w.WriteNumber("fromX", drag.From.X);
                w.WriteNumber("fromY", drag.From.Y);
                w.WriteNumber("toX", drag.To.X);
                w.WriteNumber("toY", drag.To.Y);
                w.WriteNumber(SurfaceArg, drag.From.Surface);
                w.WriteNumber("steps", drag.Steps);
                WriteButton(w, drag.Button, drag.Modifiers);
                return "gui_drag";
            case AutomationCommand.Scroll scroll:
                w.WriteNumber("dx", scroll.X);
                w.WriteNumber("dy", scroll.Y);
                w.WriteNumber(FramesArg, scroll.Frames);
                return "gui_scroll";
            case AutomationCommand.Key key:
                w.WriteString("key", key.Pressed.ToString());
                w.WriteString(ModsArg, FlagsText(key.Modifiers));
                w.WriteString("action", key.Action.ToString().ToLowerInvariant());
                w.WriteNumber(FramesArg, key.Frames);
                return "gui_key";
            case AutomationCommand.TypeText type:
                w.WriteString(TextArg, type.Text);
                w.WriteNumber(FramesArg, type.Frames);
                return "gui_type";
            case AutomationCommand.Compose compose:
                w.WriteString(TextArg, compose.Text);
                if (compose.Caret != null)
                    w.WriteNumber("caret", compose.Caret.Value);
                w.WriteNumber(FramesArg, compose.Frames);
                return "gui_compose";
            case AutomationCommand.WaitFor waitFor:
                WriteSelector(w, waitFor.Selector);
                w.WriteNumber(TimeoutArg, waitFor.Timeout.TotalSeconds);
                return "gui_wait_for";
            case AutomationCommand.Step step:
                w.WriteNumber("count", step.Count);
                return "gui_step";
            case AutomationCommand.Advance advance:
                w.WriteNumber("seconds", advance.Seconds);
                return "gui_advance";
            case AutomationCommand.Settle settle:
                w.WriteNumber("maxSeconds", settle.MaxSeconds);
                return "gui_settle";
            case AutomationCommand.WaitUntilReady ready:
                w.WriteNumber(TimeoutArg, ready.Timeout.TotalSeconds);
                return "gui_wait_until_ready";
            case AutomationCommand.QueueFilePicker picker:
                if (picker.Path == null)
                    w.WriteBoolean("cancel", true);
                else
                    w.WriteString("answer", picker.Path);
                return "gui_file_picker";
            case AutomationCommand.FilePickerRequests:
                return "gui_file_picker_requests";
            case AutomationCommand.GetClipboard:
                return "gui_clipboard_get";
            case AutomationCommand.SetClipboard set:
                w.WriteString(TextArg, set.Text);
                return "gui_clipboard_set";
            case AutomationCommand.SystemMenuItems:
                return "gui_menu_items";
            case AutomationCommand.PickSystemMenuItem pick:
                w.WriteStartArray(PathArg);
                foreach (var title in pick.Path)
                    w.WriteStringValue(title);
                w.WriteEndArray();
                return "gui_menu_pick";
            case AutomationCommand.Effects:
                return "gui_effects";
            case AutomationCommand.Close:
                return "gui_close";
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }
    }

    private static void WritePoint(Utf8JsonWriter w, WindowPoint at)
    {
        w.WriteNumber(XArg, at.X);
        w.WriteNumber(YArg, at.Y);
        w.WriteNumber(SurfaceArg, at.Surface);
    }

    private static void WriteButton(Utf8JsonWriter w, PointerButton button, KeyModifiers modifiers)
    {
        w.WriteString(ButtonArg, button.ToString().ToLowerInvariant());
        w.WriteString(ModsArg, FlagsText(modifiers));
    }

    private static void WriteSelector(Utf8JsonWriter w, Selector selector)
    {
        switch (selector)
        {
            case Selector.ByPath byPath:
                w.WriteString(PathArg, byPath.Path.ToString());
                break;
            case Selector.ByRole byRole:
                w.WriteString(RoleArg, SemanticText.RoleName(byRole.Role));
                w.WriteString(LabelArg, byRole.Label);
                break;
            case Selector.ByText byText:
                w.WriteString(TextArg, byText.Text);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(selector), selector, null);
        }
    }

    private static AutomationCommand ReadCapture(Args a)
    {
        var selector = a.Target();
        var surface = a.Integer(SurfaceArg);
        if (selector != null)
        {
            if (surface != null)
                throw a.Reject("Give a node or a surface, not both.");
            return new AutomationCommand.Capture(new CaptureTarget.Element(selector, (float)(a.Number("margin") ?? 8.0), a.Integer("magnify") ?? 1));
        }
        if (a.Has("margin") || a.Has("magnify"))
            throw a.Reject("margin and magnify crop to a node: name one.");
        if (surface != null)
        {
            if (surface < 0)
                throw a.Reject("A surface is 0 or more.");
            return new AutomationCommand.Capture(new CaptureTarget.Surface(surface.Value));
        }
        return new AutomationCommand.Capture(new CaptureTarget.Composite());
    }

    private static AutomationCommand ReadMove(Args a)
    {
        var selector = a.Target();
        if (selector == null)
            return new AutomationCommand.MovePointer(a.Point(), a.Frames());
        a.RequireNoPoint();
        if (a.Has(FramesArg))
            throw a.Reject("Moving onto a node runs one frame; frames is for x and y.");
        return new AutomationCommand.MoveTo(selector);
    }

    private static AutomationCommand ReadClick(Args a)
    {
        var selector = a.Target();
        if (selector == null)
            return new AutomationCommand.Click(a.Point(), a.Button(), a.Mods());
        a.RequireNoPoint();
        return new AutomationCommand.ClickOn(selector, a.Button(), a.Mods());
    }

    private static AutomationCommand ReadDrag(Args a)
    {
        var surface = a.Integer(SurfaceArg) ?? 0;
        var from = new WindowPoint(a.RequiredCoordinate("fromX"), a.RequiredCoordinate("fromY"), surface);
        var to = new WindowPoint(a.RequiredCoordinate("toX"), a.RequiredCoordinate("toY"), surface);
        return new AutomationCommand.Drag(from, to, a.Integer("steps") ?? 4, a.Button(), a.Mods());
    }

    private static AutomationCommand ReadFilePicker(Args a)
    {
        var answer = a.String("answer");
        var cancel = a.Boolean("cancel") ?? false;
        if (answer != null == cancel)
            throw a.Reject("Give an answer, or cancel: true.");
        return new AutomationCommand.QueueFilePicker(answer);
    }

    /// <summary>A command's arguments, matched to its verb by name, case-insensitively; an argument the verb does not
    /// take, one given twice, a null and a value of the wrong JSON type are rejected, naming it.</summary>
    private sealed class Args
    {
        private readonly Dictionary<string, (WireArg Spec, JsonElement Value)> _values;

        private Args(Dictionary<string, (WireArg, JsonElement)> values)
        {
            _values = values;
        }

        public static Args Of(WireVerb verb, JsonElement arguments)
        {
            if (arguments.ValueKind == JsonValueKind.Undefined || arguments.ValueKind == JsonValueKind.Null)
                return Check(verb, new Dictionary<string, (WireArg, JsonElement)>(StringComparer.OrdinalIgnoreCase));
            if (arguments.ValueKind != JsonValueKind.Object)
                throw new FormatException("The arguments are not an object.");
            var specs = verb.Args.ToDictionary(arg => arg.Name, StringComparer.OrdinalIgnoreCase);
            var values = new Dictionary<string, (WireArg, JsonElement)>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in arguments.EnumerateObject())
            {
                if (!specs.TryGetValue(property.Name, out var spec))
                    throw new FormatException($"'{property.Name}' is no argument of {verb.Name}; it takes {Known(verb)}.");
                if (property.Value.ValueKind == JsonValueKind.Null)
                    throw new FormatException($"'{property.Name}' is null; leave it out instead.");
                if (!values.TryAdd(property.Name, (spec, property.Value)))
                    throw new FormatException($"'{property.Name}' is given more than once.");
                CheckKind(spec, property.Value);
            }
            return Check(verb, values);
        }

        private static Args Check(WireVerb verb, Dictionary<string, (WireArg, JsonElement)> values)
        {
            foreach (var arg in verb.Args)
            {
                if (arg.Required && !values.ContainsKey(arg.Name))
                    throw new FormatException($"'{arg.Name}' is required.");
            }
            return new Args(values);
        }

        private static string Known(WireVerb verb) =>
            verb.Args.Count == 0 ? "none" : string.Join(", ", verb.Args.Select(a => a.Name));

        private static void CheckKind(WireArg spec, JsonElement value)
        {
            var ok = spec.Kind switch
            {
                WireArgKind.String => value.ValueKind == JsonValueKind.String,
                WireArgKind.Number => value.ValueKind == JsonValueKind.Number && double.IsFinite(value.GetDouble()),
                WireArgKind.Integer => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _),
                WireArgKind.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                WireArgKind.StringList => value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(e => e.ValueKind == JsonValueKind.String),
                _ => throw new ArgumentOutOfRangeException(nameof(spec), spec.Kind, null),
            };
            if (!ok)
                throw new FormatException($"'{spec.Name}' must be {KindName(spec.Kind)}.");
            if (spec.Options != null && spec.Kind == WireArgKind.String
                && !spec.Options.Contains(value.GetString(), StringComparer.OrdinalIgnoreCase))
                throw new FormatException($"'{spec.Name}' is one of {string.Join(", ", spec.Options)}, not \"{value.GetString()}\".");
        }

        private static string KindName(WireArgKind kind) => kind switch
        {
            WireArgKind.String => "a string",
            WireArgKind.Number => "a finite number",
            WireArgKind.Integer => "a whole number",
            WireArgKind.Boolean => "true or false",
            WireArgKind.StringList => "an array of strings",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        public FormatException Reject(string reason) => new(reason);

        public bool Has(string name) => _values.ContainsKey(name);

        public string? String(string name) => _values.TryGetValue(name, out var v) ? v.Value.GetString() : null;

        public string RequiredString(string name) => String(name) ?? throw Reject($"'{name}' is required.");

        public double? Number(string name) => _values.TryGetValue(name, out var v) ? v.Value.GetDouble() : null;

        public double RequiredNumber(string name) => Number(name) ?? throw Reject($"'{name}' is required.");

        /// <summary>A coordinate, narrowed to the <see cref="float"/> points are kept in; durations stay doubles.</summary>
        public float RequiredCoordinate(string name)
        {
            var single = (float)RequiredNumber(name);
            if (!float.IsFinite(single))
                throw Reject($"'{name}' is out of range.");
            return single;
        }

        public int? Integer(string name) => _values.TryGetValue(name, out var v) ? v.Value.GetInt32() : null;

        public bool? Boolean(string name) => _values.TryGetValue(name, out var v) ? v.Value.GetBoolean() : null;

        public IReadOnlyList<string> RequiredStrings(string name)
        {
            if (!_values.TryGetValue(name, out var v))
                throw Reject($"'{name}' is required.");
            return v.Value.EnumerateArray().Select(e => e.GetString()!).ToArray();
        }

        public int Frames() => Integer(FramesArg) ?? 1;

        public TimeSpan Timeout()
        {
            var seconds = Number(TimeoutArg) ?? throw Reject($"'{TimeoutArg}' is required.");
            if (seconds < 0 || seconds > AutomationCommand.MaxTimeout.TotalSeconds)
                throw Reject($"'{TimeoutArg}' is from 0 to {AutomationCommand.MaxTimeout.TotalSeconds}.");
            return TimeSpan.FromSeconds(seconds);
        }

        public PointerButton Button() => OptionalNamed(ButtonArg, PointerButton.Left);

        public KeyModifiers Mods()
        {
            var text = String(ModsArg);
            return text == null ? KeyModifiers.None : ParseFlags<KeyModifiers>(text, ModsArg);
        }

        public T Named<T>(string name) where T : struct, System.Enum => ParseName<T>(RequiredString(name), name);

        public T OptionalNamed<T>(string name, T fallback) where T : struct, System.Enum
        {
            var text = String(name);
            return text == null ? fallback : ParseName<T>(text, name);
        }

        public WindowPoint Point()
        {
            var x = Number(XArg);
            var y = Number(YArg);
            if (x == null || y == null)
                throw Reject("Name a node (path, role and label, or text), or give x and y.");
            return new WindowPoint(RequiredCoordinate(XArg), RequiredCoordinate(YArg), Integer(SurfaceArg) ?? 0);
        }

        public void RequireNoPoint()
        {
            if (Has(XArg) || Has(YArg) || Has(SurfaceArg))
                throw Reject("Give a node or x and y, not both.");
        }

        /// <summary>The node named by path, by role and label, or by text, one of the three; null when none is given.</summary>
        public Selector? Target()
        {
            var path = String(PathArg);
            var role = String(RoleArg);
            var label = String(LabelArg);
            var text = String(TextArg);
            var given = (path != null ? 1 : 0) + (role != null || label != null ? 1 : 0) + (text != null ? 1 : 0);
            if (given == 0)
                return null;
            if (given > 1)
                throw Reject("Name the node one way: path, role and label, or text.");
            if (path != null)
                return Selector.AtPath(path);
            if (text != null)
                return Selector.WithText(text);
            if (role == null || label == null)
                throw Reject("role and label go together.");
            return Selector.OfRole(ParseName<SemanticRole>(role, RoleArg), label);
        }
    }

    /// <summary>The text a person or a model reads for <paramref name="result"/>.</summary>
    public static string Text(AutomationResult result)
    {
        switch (result)
        {
            case AutomationResult.Ok ok:
                return ok.Payload switch
                {
                    AutomationPayload.None => $"Done; frame {ok.Frame}.",
                    AutomationPayload.Capabilities c => $"Protocol {c.Version}, {c.Set.ToString().ToLowerInvariant()}: {string.Join(", ", c.Commands)}.",
                    AutomationPayload.Snapshot s => s.Text,
                    AutomationPayload.Node n => n.Found.Line,
                    AutomationPayload.Image i => i.Text,
                    AutomationPayload.Settled s => s.Result.Settled
                        ? $"Settled after {s.Result.Seconds.ToString("0.###", CultureInfo.InvariantCulture)} s."
                        : $"Still pending after {s.Result.Seconds.ToString("0.###", CultureInfo.InvariantCulture)} s: {s.Result.Pending}.",
                    AutomationPayload.Clipboard c => c.Text ?? "(the clipboard is empty)",
                    AutomationPayload.MenuItems m => m.Items.Count == 0 ? "(no system menu bar)" : string.Join("\n", m.Items.Select(i => string.Join(" > ", i.Path))),
                    AutomationPayload.FilePickers f => f.Requests.Count == 0 ? "(no file picker opened)" : string.Join("\n", f.Requests.Select(r => $"{r.Kind} \"{r.Title}\" -> {r.Answer ?? "cancelled"}")),
                    AutomationPayload.Effects e => e.Recorded.Count == 0 ? "(no effects)" : string.Join("\n", e.Recorded),
                    _ => throw new ArgumentOutOfRangeException(nameof(result), ok.Payload, null),
                };
            default:
                var snapshot = result.FailureSnapshot;
                var message = result.FailureMessage ?? "";
                return snapshot == null ? message : message + "\n\n" + snapshot;
        }
    }

    public static string StatusName(AutomationResult result) => result switch
    {
        AutomationResult.Ok => "ok",
        AutomationResult.NotFound => "not_found",
        AutomationResult.Ambiguous => "ambiguous",
        AutomationResult.Occluded => "occluded",
        AutomationResult.Blocked => "blocked",
        AutomationResult.Clipped => "clipped",
        AutomationResult.Unreachable => "unreachable",
        AutomationResult.Timeout => "timeout",
        AutomationResult.Unsupported => "unsupported",
        AutomationResult.Rejected => "rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
    };

    /// <summary>Every <c>status</c> a result may have.</summary>
    public static IReadOnlyList<string> Statuses { get; } =
        ["ok", "not_found", "ambiguous", "occluded", "blocked", "clipped", "unreachable", "timeout", "unsupported", "rejected"];

    /// <summary>The PNG an image result carries beside its JSON; null for any other.</summary>
    public static byte[]? Png(AutomationResult result)
    {
        if (result is not AutomationResult.Ok ok)
            return null;
        if (ok.Payload is not AutomationPayload.Image image)
            return null;
        return image.Png;
    }

    /// <summary>Writes <paramref name="result"/> as the members of an object, its PNG left out.</summary>
    public static void WriteMembers(Utf8JsonWriter w, AutomationResult result)
    {
        w.WriteString("status", StatusName(result));
        if (result is AutomationResult.Ok ok)
        {
            w.WriteNumber("frame", ok.Frame);
            w.WriteStartObject("payload");
            WritePayload(w, ok.Payload);
            w.WriteEndObject();
            return;
        }
        w.WriteString("message", result.FailureMessage);
        var snapshot = result.FailureSnapshot;
        if (snapshot != null)
            w.WriteString("snapshot", snapshot);
    }

    public static string Write(AutomationResult result)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            WriteMembers(w, result);
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WritePayload(Utf8JsonWriter w, AutomationPayload payload)
    {
        switch (payload)
        {
            case AutomationPayload.None:
                w.WriteString("kind", "none");
                break;
            case AutomationPayload.Capabilities c:
                w.WriteString("kind", "capabilities");
                w.WriteNumber("version", c.Version);
                w.WriteString("set", c.Set.ToString().ToLowerInvariant());
                WriteStrings(w, "commands", c.Commands);
                break;
            case AutomationPayload.Snapshot s:
                w.WriteString("kind", "snapshot");
                w.WriteString("text", s.Text);
                break;
            case AutomationPayload.Node n:
                w.WriteString("kind", "node");
                WriteNode(w, n.Found);
                break;
            case AutomationPayload.Image i:
                w.WriteString("kind", "image");
                w.WriteNumber("width", i.Width);
                w.WriteNumber("height", i.Height);
                w.WriteString("text", i.Text);
                break;
            case AutomationPayload.Settled s:
                w.WriteString("kind", "settled");
                w.WriteBoolean("settled", s.Result.Settled);
                w.WriteNumber("seconds", s.Result.Seconds);
                w.WriteString("pending", FlagsText(s.Result.Pending));
                break;
            case AutomationPayload.Clipboard c:
                w.WriteString("kind", "clipboard");
                WriteOptional(w, "text", c.Text);
                WriteOptional(w, "html", c.Html);
                break;
            case AutomationPayload.MenuItems m:
                w.WriteString("kind", "menuItems");
                w.WriteStartArray("items");
                foreach (var item in m.Items)
                {
                    w.WriteStartObject();
                    WriteStrings(w, "path", item.Path);
                    w.WriteBoolean("enabled", item.Enabled);
                    w.WriteBoolean("checked", item.Checked);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                break;
            case AutomationPayload.FilePickers f:
                w.WriteString("kind", "filePickers");
                w.WriteStartArray("requests");
                foreach (var request in f.Requests)
                    WritePickerRequest(w, request);
                w.WriteEndArray();
                break;
            case AutomationPayload.Effects e:
                w.WriteString("kind", "effects");
                w.WriteStartArray("effects");
                foreach (var effect in e.Recorded)
                    WriteEffect(w, effect);
                w.WriteEndArray();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(payload), payload, null);
        }
    }

    private static void WriteNode(Utf8JsonWriter w, NodeSummary node)
    {
        WriteOptional(w, "path", node.Path);
        w.WriteString("role", SemanticText.RoleName(node.Role));
        WriteOptional(w, "label", node.Label);
        WriteOptional(w, "value", node.Value);
        w.WriteString("states", FlagsText(node.States));
        w.WriteNumber("surface", node.Surface);
        w.WriteStartObject("bounds");
        w.WriteNumber("x", node.Bounds.X);
        w.WriteNumber("y", node.Bounds.Y);
        w.WriteNumber("w", node.Bounds.Width);
        w.WriteNumber("h", node.Bounds.Height);
        w.WriteEndObject();
        w.WriteString("visibility", node.Visibility.ToString());
        w.WriteString("line", node.Line);
    }

    private static void WritePickerRequest(Utf8JsonWriter w, FilePickerRequest request)
    {
        w.WriteStartObject();
        w.WriteString("kind", request.Kind.ToString());
        w.WriteString("title", request.Title);
        WriteOptional(w, "initialDirectory", request.InitialDirectory);
        WriteOptional(w, "suggestedFileName", request.SuggestedFileName);
        if (request.Filters != null)
        {
            w.WriteStartArray("filters");
            foreach (var filter in request.Filters)
            {
                w.WriteStartObject();
                w.WriteString("name", filter.Name);
                WriteStrings(w, "patterns", filter.Patterns);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        WriteOptional(w, "answer", request.Answer);
        w.WriteEndObject();
    }

    private static void WriteEffect(Utf8JsonWriter w, AutomationEffect effect)
    {
        w.WriteStartObject();
        switch (effect)
        {
            case AutomationEffect.WindowShown:
                w.WriteString("kind", "windowShown");
                break;
            case AutomationEffect.WindowHidden:
                w.WriteString("kind", "windowHidden");
                break;
            case AutomationEffect.BroughtToFront:
                w.WriteString("kind", "broughtToFront");
                break;
            case AutomationEffect.WindowStateRequested state:
                w.WriteString("kind", "windowStateRequested");
                w.WriteString("state", state.State.ToString());
                break;
            case AutomationEffect.PointerLockChanged locked:
                w.WriteString("kind", "pointerLockChanged");
                w.WriteBoolean("locked", locked.Locked);
                break;
            case AutomationEffect.FileOpened opened:
                w.WriteString("kind", "fileOpened");
                w.WriteString("path", opened.Path);
                break;
            case AutomationEffect.FileRevealed revealed:
                w.WriteString("kind", "fileRevealed");
                w.WriteString("path", revealed.Path);
                break;
            case AutomationEffect.FolderOpened folder:
                w.WriteString("kind", "folderOpened");
                w.WriteString("path", folder.Path);
                break;
            case AutomationEffect.UrlOpened url:
                w.WriteString("kind", "urlOpened");
                w.WriteString("url", url.Url);
                break;
            case AutomationEffect.FileShared shared:
                w.WriteString("kind", "fileShared");
                w.WriteString("path", shared.Path);
                w.WriteString("title", shared.Title);
                WriteOptional(w, "text", shared.Text);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(effect), effect, null);
        }
        w.WriteEndObject();
    }

    private static void WriteStrings(Utf8JsonWriter w, string name, IEnumerable<string> values)
    {
        w.WriteStartArray(name);
        foreach (var value in values)
            w.WriteStringValue(value);
        w.WriteEndArray();
    }

    private static void WriteOptional(Utf8JsonWriter w, string name, string? value)
    {
        if (value != null)
            w.WriteString(name, value);
    }

    /// <summary>Reads a result back: <paramref name="result"/> as <see cref="WriteMembers"/> wrote it, and the PNG an
    /// image result carries beside it.</summary>
    /// <exception cref="FormatException">It is not a result this version writes.</exception>
    public static AutomationResult ReadResult(JsonElement result, byte[]? png)
    {
        try
        {
            return ReadResultUnchecked(result, png);
        }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            throw new FormatException($"Not an automation result: {e.Message}\n{result}", e);
        }
    }

    private static readonly string[] SnapshotStatuses = ["not_found", "ambiguous", "occluded", "blocked", "clipped", "unreachable"];

    private static AutomationResult ReadResultUnchecked(JsonElement element, byte[]? png)
    {
        var status = Fields.Of(element, "result", ["status"], ["frame", "payload", "message", "snapshot"]).Str("status");
        if (status == "ok")
        {
            var ok = Fields.Of(element, "ok result", ["status", "frame", "payload"], []);
            var frame = ok.Long("frame");
            if (frame < 0)
                throw new FormatException("'frame' is negative.");
            var payload = ReadPayload(ok.Element("payload"), png);
            if (png != null && payload is not AutomationPayload.Image)
                throw new FormatException("A PNG came with a result that carries no image.");
            return new AutomationResult.Ok(frame, payload);
        }
        if (png != null)
            throw new FormatException($"A PNG came with a {status} result.");
        if (SnapshotStatuses.Contains(status))
        {
            var failed = Fields.Of(element, $"{status} result", ["status", "message", "snapshot"], []);
            var message = failed.Str("message");
            var snapshot = failed.Str("snapshot");
            return status switch
            {
                "not_found" => new AutomationResult.NotFound(message, snapshot),
                "ambiguous" => new AutomationResult.Ambiguous(message, snapshot),
                "occluded" => new AutomationResult.Occluded(message, snapshot),
                "blocked" => new AutomationResult.Blocked(message, snapshot),
                "clipped" => new AutomationResult.Clipped(message, snapshot),
                "unreachable" => new AutomationResult.Unreachable(message, snapshot),
                _ => throw new FormatException($"Unknown status \"{status}\"."),
            };
        }
        switch (status)
        {
            case "timeout":
                var timeout = Fields.Of(element, "timeout result", ["status", "message"], ["snapshot"]);
                return new AutomationResult.Timeout(timeout.Str("message"), timeout.OptionalStr("snapshot"));
            case "unsupported":
                return new AutomationResult.Unsupported(Fields.Of(element, "unsupported result", ["status", "message"], []).Str("message"));
            case "rejected":
                return new AutomationResult.Rejected(Fields.Of(element, "rejected result", ["status", "message"], []).Str("message"));
            default:
                throw new FormatException($"Unknown status \"{status}\".");
        }
    }

    private static AutomationPayload ReadPayload(JsonElement element, byte[]? png)
    {
        var kind = Fields.Of(element, "payload", ["kind"], ["version", "set", "commands", "text", "path", "role", "label", "value",
            "states", "surface", "bounds", "visibility", "line", "width", "height", "settled", "seconds", "pending", "html", "items",
            "requests", "effects"]).Str("kind");
        switch (kind)
        {
            case "none":
                Fields.Of(element, "none payload", ["kind"], []);
                return new AutomationPayload.None();
            case "capabilities":
                var capabilities = Fields.Of(element, "capabilities", ["kind", "version", "set", "commands"], []);
                var version = capabilities.Int("version");
                if (version < 1)
                    throw new FormatException("'version' is below 1.");
                return new AutomationPayload.Capabilities(version, ParseName<AutomationCapabilitySet>(capabilities.Str("set"), "set"),
                    NonEmpty(capabilities.Strings("commands"), "commands"));
            case "snapshot":
                return new AutomationPayload.Snapshot(Fields.Of(element, "snapshot", ["kind", "text"], []).Str("text"));
            case "node":
                return new AutomationPayload.Node(ReadNode(element));
            case "image":
                var image = Fields.Of(element, "image", ["kind", "width", "height", "text"], []);
                var width = image.Int("width");
                var height = image.Int("height");
                if (width < 1 || height < 1)
                    throw new FormatException($"An image is at least 1x1, not {width}x{height}.");
                if (png == null)
                    throw new FormatException("An image result came without its PNG.");
                var (pngWidth, pngHeight) = PngHeader(png);
                if (pngWidth != width || pngHeight != height)
                    throw new FormatException($"The PNG is {pngWidth}x{pngHeight}, and its result says {width}x{height}.");
                return new AutomationPayload.Image(png, width, height, image.Str("text"));
            case "settled":
                var settled = Fields.Of(element, "settled", ["kind", "settled", "seconds", "pending"], []);
                var seconds = settled.Double("seconds");
                if (seconds < 0)
                    throw new FormatException("'seconds' is negative.");
                var pending = ParseFlags<FramePending>(settled.Str("pending"), "pending");
                if (settled.Bool("settled") != (pending == FramePending.None))
                    throw new FormatException("settled disagrees with pending.");
                return new AutomationPayload.Settled(new SettleResult(pending, seconds));
            case "clipboard":
                var clipboard = Fields.Of(element, "clipboard", ["kind"], ["text", "html"]);
                return new AutomationPayload.Clipboard(clipboard.OptionalStr("text"), clipboard.OptionalStr("html"));
            case "menuItems":
                return new AutomationPayload.MenuItems(Fields.Of(element, "menu items", ["kind", "items"], []).Array("items").Select(ReadMenuItem).ToArray());
            case "filePickers":
                return new AutomationPayload.FilePickers(Fields.Of(element, "file pickers", ["kind", "requests"], []).Array("requests")
                    .Select(ReadPickerRequest).ToArray());
            case "effects":
                return new AutomationPayload.Effects(Fields.Of(element, "effects", ["kind", "effects"], []).Array("effects")
                    .Select(ReadEffect).ToArray());
            default:
                throw new FormatException($"Unknown payload \"{kind}\".");
        }
    }

    private static SystemMenuItem ReadMenuItem(JsonElement element)
    {
        var item = Fields.Of(element, "menu item", ["path", "enabled", "checked"], []);
        var path = item.Strings("path");
        if (path.Length < 2)
            throw new FormatException("A menu item's path holds its menus' titles, then its own.");
        return new SystemMenuItem(path, item.Bool("enabled"), item.Bool("checked"));
    }

    private static NodeSummary ReadNode(JsonElement element)
    {
        var node = Fields.Of(element, "node", ["kind", "role", "states", "surface", "bounds", "visibility", "line"], ["path", "label", "value"]);
        var bounds = Fields.Of(node.Element("bounds"), "bounds", ["x", "y", "w", "h"], []);
        var rect = new UiRect(bounds.Single("x"), bounds.Single("y"), bounds.Single("w"), bounds.Single("h"));
        if (rect.Width < 0 || rect.Height < 0)
            throw new FormatException("A node's bounds have a negative size.");
        var surface = node.Int("surface");
        if (surface < 0)
            throw new FormatException("'surface' is negative.");
        var path = node.OptionalStr("path");
        if (path != null)
            TestPath.Parse(path);
        return new NodeSummary(path, ParseName<SemanticRole>(node.Str("role"), "role"), node.OptionalStr("label"), node.OptionalStr("value"),
            ParseFlags<SemanticStates>(node.Str("states"), "states"), surface, rect, ParseName<Visibility>(node.Str("visibility"), "visibility"),
            node.Str("line"));
    }

    private static FilePickerRequest ReadPickerRequest(JsonElement element)
    {
        var request = Fields.Of(element, "file picker request", ["kind", "title"], ["initialDirectory", "suggestedFileName", "filters", "answer"]);
        IReadOnlyList<FileFilter>? filters = null;
        if (request.Has("filters"))
            filters = request.Array("filters").Select(ReadFileFilter).ToArray();
        return new FilePickerRequest(ParseName<FilePickerKind>(request.Str("kind"), "kind"), request.Str("title"),
            request.OptionalStr("initialDirectory"), request.OptionalStr("suggestedFileName"), filters, request.OptionalStr("answer"));
    }

    private static FileFilter ReadFileFilter(JsonElement element)
    {
        var filter = Fields.Of(element, "file filter", ["name", "patterns"], []);
        return new FileFilter(filter.Str("name"), filter.Strings("patterns"));
    }

    private static AutomationEffect ReadEffect(JsonElement element)
    {
        var kind = Fields.Of(element, "effect", ["kind"], ["state", "locked", "path", "url", "title", "text"]).Str("kind");
        var members = kind switch
        {
            "windowStateRequested" => new[] { "kind", "state" },
            "pointerLockChanged" => ["kind", "locked"],
            "fileOpened" or "fileRevealed" or "folderOpened" => ["kind", "path"],
            "urlOpened" => ["kind", "url"],
            "fileShared" => ["kind", "path", "title"],
            _ => ["kind"],
        };
        var effect = Fields.Of(element, $"{kind} effect", members, kind == "fileShared" ? ["text"] : []);
        return kind switch
        {
            "windowShown" => new AutomationEffect.WindowShown(),
            "windowHidden" => new AutomationEffect.WindowHidden(),
            "broughtToFront" => new AutomationEffect.BroughtToFront(),
            "windowStateRequested" => new AutomationEffect.WindowStateRequested(ParseName<WindowState>(effect.Str("state"), "state")),
            "pointerLockChanged" => new AutomationEffect.PointerLockChanged(effect.Bool("locked")),
            "fileOpened" => new AutomationEffect.FileOpened(effect.Str("path")),
            "fileRevealed" => new AutomationEffect.FileRevealed(effect.Str("path")),
            "folderOpened" => new AutomationEffect.FolderOpened(effect.Str("path")),
            "fileShared" => new AutomationEffect.FileShared(effect.Str("path"), effect.Str("title"), effect.OptionalStr("text")),
            "urlOpened" => new AutomationEffect.UrlOpened(effect.Str("url")),
            _ => throw new FormatException($"Unknown effect \"{kind}\"."),
        };
    }

    private static string[] NonEmpty(string[] values, string what)
    {
        if (values.Any(string.IsNullOrEmpty))
            throw new FormatException($"'{what}' holds an empty string.");
        return values;
    }

    /// <summary>The size of <paramref name="png"/>, once it is checked whole: every chunk's CRC, its IDAT inflating to
    /// what its header promises, IEND last.</summary>
    private static (int Width, int Height) PngHeader(byte[] png) => ZGF.Gui.Hosting.PngStructure.Validate(png);

    /// <summary>An object read off the wire: every member it must have, none it does not know, and none twice.</summary>
    private sealed class Fields
    {
        private readonly Dictionary<string, JsonElement> _members;

        private Fields(Dictionary<string, JsonElement> members)
        {
            _members = members;
        }

        public static Fields Of(JsonElement element, string what, string[] required, string[] optional)
        {
            if (element.ValueKind != JsonValueKind.Object)
                throw new FormatException($"The {what} is not an object.");
            var members = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!required.Contains(property.Name) && !optional.Contains(property.Name))
                    throw new FormatException($"The {what} has a member '{property.Name}' it does not take.");
                if (!members.TryAdd(property.Name, property.Value))
                    throw new FormatException($"The {what} has '{property.Name}' twice.");
            }
            foreach (var name in required)
            {
                if (!members.ContainsKey(name))
                    throw new FormatException($"The {what} lacks '{name}'.");
            }
            return new Fields(members);
        }

        public bool Has(string name) => _members.ContainsKey(name);

        public JsonElement Element(string name) =>
            _members.TryGetValue(name, out var value) ? value : throw new FormatException($"'{name}' is missing.");

        public string Str(string name) => OptionalStr(name) ?? throw new FormatException($"'{name}' is missing.");

        public string? OptionalStr(string name)
        {
            if (!_members.TryGetValue(name, out var value))
                return null;
            if (value.ValueKind != JsonValueKind.String)
                throw new FormatException($"'{name}' is not a string.");
            return value.GetString();
        }

        public long Long(string name)
        {
            var value = Element(name);
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var number))
                throw new FormatException($"'{name}' is not a whole number.");
            return number;
        }

        public int Int(string name)
        {
            var value = Element(name);
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
                throw new FormatException($"'{name}' is not a whole number.");
            return number;
        }

        public double Double(string name)
        {
            var value = Element(name);
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number) || !double.IsFinite(number))
                throw new FormatException($"'{name}' is not a finite number.");
            return number;
        }

        public float Single(string name)
        {
            var number = (float)Double(name);
            if (!float.IsFinite(number))
                throw new FormatException($"'{name}' is out of range.");
            return number;
        }

        public bool Bool(string name) => Element(name).ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new FormatException($"'{name}' is not true or false."),
        };

        public JsonElement[] Array(string name)
        {
            var value = Element(name);
            if (value.ValueKind != JsonValueKind.Array)
                throw new FormatException($"'{name}' is not an array.");
            return value.EnumerateArray().ToArray();
        }

        public string[] Strings(string name) => Array(name).Select(StringOf).ToArray();

        private static string StringOf(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.String)
                throw new FormatException("An array of strings holds something else.");
            return element.GetString() ?? "";
        }
    }

    private static string FlagsText<T>(T flags) where T : struct, Enum => flags.ToString();

    /// <summary>One of <typeparamref name="T"/>'s names, in any case; a number is no name.</summary>
    private static T ParseName<T>(string text, string what) where T : struct, Enum
    {
        foreach (var name in System.Enum.GetNames<T>())
        {
            if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
                return System.Enum.Parse<T>(name);
        }
        throw new FormatException($"'{what}' \"{text}\" is not one of {string.Join(", ", System.Enum.GetNames<T>())}.");
    }

    /// <summary>Flag names joined by commas, as <see cref="Enum.ToString()"/> writes them; "None", or nothing, for none.</summary>
    private static T ParseFlags<T>(string text, string what) where T : struct, Enum
    {
        ulong bits = 0;
        foreach (var part in text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            bits |= Convert.ToUInt64(ParseName<T>(part, what), CultureInfo.InvariantCulture);
        return (T)System.Enum.ToObject(typeof(T), bits);
    }
}

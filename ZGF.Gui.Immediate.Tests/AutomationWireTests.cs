using System.Text.Json;
using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class AutomationWireTests
{
    public static TheoryData<AutomationCommand> Commands => new()
    {
        new AutomationCommand.Capabilities(),
        new AutomationCommand.Snapshot(),
        new AutomationCommand.Capture(new CaptureTarget.Composite()),
        new AutomationCommand.Capture(new CaptureTarget.Surface(2)),
        new AutomationCommand.Capture(new CaptureTarget.Element(Selector.OfRole(SemanticRole.Button, "Add"), 4f, 3)),
        new AutomationCommand.MovePointer(new WindowPoint(1.5f, 2.5f, 1), 0),
        new AutomationCommand.PointerDown(PointerButton.Right, KeyModifiers.Shift | KeyModifiers.Control, 2),
        new AutomationCommand.PointerUp(),
        new AutomationCommand.Click(new WindowPoint(10f, 20f), PointerButton.Middle, KeyModifiers.Alt),
        new AutomationCommand.Drag(new WindowPoint(1f, 2f), new WindowPoint(3f, 4f), 7),
        new AutomationCommand.Scroll(0.5, -3),
        new AutomationCommand.Key(KeyboardKey.Enter, KeyModifiers.Super, KeyAction.Down, 3),
        new AutomationCommand.TypeText("héllo \"there\""),
        new AutomationCommand.Compose("かな", 1),
        new AutomationCommand.Compose(""),
        new AutomationCommand.MoveTo(Selector.AtPath("items/#3/done")),
        new AutomationCommand.ClickOn(Selector.WithText("Add 1,000"), PointerButton.Left, KeyModifiers.Control),
        new AutomationCommand.WaitFor(Selector.AtPath("draft"), TimeSpan.FromSeconds(2.5)),
        new AutomationCommand.Step(4),
        new AutomationCommand.Advance(1.25),
        new AutomationCommand.Settle(3),
        new AutomationCommand.WaitUntilReady(TimeSpan.FromSeconds(9)),
        new AutomationCommand.QueueFilePicker("C:/notes.txt"),
        new AutomationCommand.QueueFilePicker(null),
        new AutomationCommand.FilePickerRequests(),
        new AutomationCommand.GetClipboard(),
        new AutomationCommand.SetClipboard("copied"),
        new AutomationCommand.SystemMenuItems(),
        new AutomationCommand.PickSystemMenuItem(["File", "Open"]),
        new AutomationCommand.Effects(),
        new AutomationCommand.Close(),
    };

    [Theory]
    [MemberData(nameof(Commands))]
    public void Every_command_reads_back_as_written(AutomationCommand command)
    {
        var (name, arguments) = AutomationWire.Write(command);

        var parsed = Assert.IsType<WireCommand.Parsed>(AutomationWire.Parse(name, arguments));

        Assert.Equal(Describe(command), Describe(parsed.Command));
    }

    [Fact]
    public void Every_verb_is_written_by_some_command()
    {
        var written = ((IEnumerable<object[]>)Commands).Select(row => AutomationWire.Write((AutomationCommand)row[0]).Name).ToHashSet();

        Assert.Equal(AutomationWire.All.Select(v => v.Name).Order(), written.Order());
    }

    [Theory]
    [InlineData("gui_nope", "{}", "no command")]
    [InlineData("gui_click", "{\"x\":1}", "x and y")]
    [InlineData("gui_click", "{\"x\":\"1\",\"y\":2}", "'x' must be a finite number")]
    [InlineData("gui_click", "{\"path\":\"a\",\"x\":1,\"y\":2}", "not both")]
    [InlineData("gui_click", "{\"path\":\"a\",\"text\":\"b\"}", "one way")]
    [InlineData("gui_click", "{\"role\":\"button\"}", "go together")]
    [InlineData("gui_click", "{\"role\":\"robot\",\"label\":\"x\"}", "'role' is one of")]
    [InlineData("gui_click", "{\"path\":\"a\",\"PATH\":\"b\"}", "more than once")]
    [InlineData("gui_click", "{\"path\":\"a\",\"colour\":\"red\"}", "no argument")]
    [InlineData("gui_click", "{\"path\":null}", "null")]
    [InlineData("gui_click", "{\"path\":\"a\\\\\"}", "escape")]
    [InlineData("gui_key", "{\"key\":\"Unknown\"}", "Name a key")]
    [InlineData("gui_key", "{\"key\":\"A\",\"mods\":\"Control,Hyper\"}", "'mods'")]
    [InlineData("gui_key", "{\"key\":\"1\"}", "'key'")]
    [InlineData("gui_step", "{\"count\":1.5}", "whole number")]
    [InlineData("gui_step", "{\"count\":-1}", "Frames are from 0")]
    [InlineData("gui_advance", "{\"seconds\":601}", "from 0 to 600")]
    [InlineData("gui_wait_for", "{\"timeoutSeconds\":1}", "Name the node")]
    [InlineData("gui_wait_until_ready", "{\"timeoutSeconds\":-1}", "from 0")]
    [InlineData("gui_wait_until_ready", "{}", "required")]
    [InlineData("gui_file_picker", "{}", "answer")]
    [InlineData("gui_file_picker", "{\"answer\":\"a\",\"cancel\":true}", "answer")]
    [InlineData("gui_menu_pick", "{\"path\":[\"File\"]}", "path")]
    [InlineData("gui_screenshot", "{\"surface\":1,\"path\":\"a\"}", "not both")]
    [InlineData("gui_screenshot", "{\"magnify\":2}", "crop to a node")]
    [InlineData("gui_compose", "{\"text\":\"ab\",\"caret\":3}", "caret")]
    [InlineData("gui_snapshot", "[1]", "not an object")]
    [InlineData("gui_snapshot", "{", "not JSON")]
    public void A_command_the_wire_cannot_use_is_rejected_saying_why(string name, string arguments, string reason)
    {
        var rejected = Assert.IsType<WireCommand.Rejected>(AutomationWire.Parse(name, arguments));

        Assert.Contains(reason, rejected.Reason);
    }

    [Fact]
    public void A_huge_number_is_rejected_where_a_float_is_meant()
    {
        Assert.IsType<WireCommand.Rejected>(AutomationWire.Parse("gui_click", "{\"x\":1e300,\"y\":0}"));
    }

    public static TheoryData<AutomationResult> Results => new()
    {
        new AutomationResult.Ok(3, new AutomationPayload.None()),
        new AutomationResult.Ok(4, new AutomationPayload.Capabilities(1, AutomationCapabilitySet.Inspection, ["gui_snapshot"])),
        new AutomationResult.Ok(5, new AutomationPayload.Snapshot("# frame 5")),
        new AutomationResult.Ok(6, new AutomationPayload.Node(new NodeSummary("a/#1", SemanticRole.Checkbox, "Done", null,
            SemanticStates.Checked | SemanticStates.Focused, 1, new UiRect(1, 2, 3, 4), Visibility.Clipped, "checkbox"))),
        new AutomationResult.Ok(7, new AutomationPayload.Image(Png(10, 20), 10, 20, "# png")),
        new AutomationResult.Ok(8, new AutomationPayload.Settled(new SettleResult(FramePending.Animation | FramePending.ScheduledUpdate, 5))),
        new AutomationResult.Ok(9, new AutomationPayload.Clipboard("text", null)),
        new AutomationResult.Ok(10, new AutomationPayload.MenuItems([new SystemMenuItem(["File", "Open"], true, false)])),
        new AutomationResult.Ok(11, new AutomationPayload.FilePickers([new FilePickerRequest(FilePickerKind.SaveFile, "Save", "/tmp", "a.txt",
            [new FileFilter("Text", ["*.txt"])], null)])),
        new AutomationResult.Ok(12, new AutomationPayload.Effects([new AutomationEffect.WindowShown(), new AutomationEffect.UrlOpened("https://x"),
            new AutomationEffect.WindowStateRequested(WindowState.Maximized), new AutomationEffect.PointerLockChanged(true),
            new AutomationEffect.FileShared("/tmp/a.png", "A", "words"), new AutomationEffect.FileShared("/tmp/b.png", "B", null)])),
        new AutomationResult.NotFound("Nothing matches", "# snapshot"),
        new AutomationResult.Ambiguous("2 nodes", "# snapshot"),
        new AutomationResult.Occluded("covered", "# snapshot"),
        new AutomationResult.Blocked("blocked", "# snapshot"),
        new AutomationResult.Clipped("clipped", "# snapshot"),
        new AutomationResult.Unreachable("unreachable", "# snapshot"),
        new AutomationResult.Timeout("timed out", null),
        new AutomationResult.Timeout("timed out", "# snapshot"),
        new AutomationResult.Unsupported("inspection only"),
        new AutomationResult.Rejected("bad"),
    };

    [Theory]
    [MemberData(nameof(Results))]
    public void Every_result_reads_back_as_written(AutomationResult result)
    {
        using var document = JsonDocument.Parse(AutomationWire.Write(result));

        var read = AutomationWire.ReadResult(document.RootElement, AutomationWire.Png(result));

        Assert.Equal(AutomationWire.Write(result), AutomationWire.Write(read));
        Assert.Equal(AutomationWire.Text(result), AutomationWire.Text(read));
    }

    [Theory]
    [InlineData("{\"status\":\"fine\"}")]
    [InlineData("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"image\",\"width\":1,\"height\":1,\"text\":\"\"}}")]
    [InlineData("{\"status\":\"not_found\",\"message\":\"m\"}")]
    [InlineData("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"settled\",\"settled\":true,\"seconds\":1,\"pending\":\"Animation\"}}")]
    [InlineData("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"effects\",\"effects\":[{\"kind\":\"exploded\"}]}}")]
    [InlineData("{\"status\":\"ok\",\"frame\":\"1\",\"payload\":{\"kind\":\"none\"}}")]
    [InlineData("{\"status\":\"ok\",\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"none\"}}")]
    [InlineData("{\"status\":\"ok\",\"frame\":1,\"extra\":1,\"payload\":{\"kind\":\"none\"}}")]
    [InlineData("{\"status\":\"ok\",\"frame\":-1,\"payload\":{\"kind\":\"none\"}}")]
    [InlineData("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"none\",\"text\":\"x\"}}")]
    [InlineData("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"settled\",\"settled\":true,\"seconds\":1e400,\"pending\":\"None\"}}")]
    [InlineData("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"settled\",\"settled\":true,\"seconds\":-1,\"pending\":\"None\"}}")]
    [InlineData("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"node\",\"role\":\"text\",\"states\":\"None\",\"surface\":-1,\"bounds\":{\"x\":0,\"y\":0,\"w\":1,\"h\":1},\"visibility\":\"Visible\",\"line\":\"l\"}}")]
    [InlineData("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"node\",\"role\":\"text\",\"states\":\"None\",\"surface\":0,\"bounds\":{\"x\":0,\"y\":0,\"w\":-1,\"h\":1},\"visibility\":\"Visible\",\"line\":\"l\"}}")]
    [InlineData("{\"status\":\"rejected\",\"message\":\"m\",\"snapshot\":\"s\"}")]
    [InlineData("{\"status\":\"timeout\",\"message\":3}")]
    public void A_result_this_version_does_not_write_is_refused(string json)
    {
        using var document = JsonDocument.Parse(json);

        Assert.Throws<FormatException>(() => AutomationWire.ReadResult(document.RootElement, null));
    }


    [Theory]
    [InlineData(10, 20, 10, 20, false)]
    [InlineData(10, 20, 10, 21, true)]
    [InlineData(10, 20, 0, 20, true)]
    [InlineData(10, 20, -3, 20, true)]
    public void An_image_result_must_carry_a_png_of_the_size_it_says(int pngWidth, int pngHeight, int width, int height, bool refused)
    {
        var json = $"{{\"status\":\"ok\",\"frame\":1,\"payload\":{{\"kind\":\"image\",\"width\":{width},\"height\":{height},\"text\":\"\"}}}}";
        using var document = JsonDocument.Parse(json);
        var png = Png(pngWidth, pngHeight);

        if (refused)
            Assert.Throws<FormatException>(() => AutomationWire.ReadResult(document.RootElement, png));
        else
            Assert.IsType<AutomationResult.Ok>(AutomationWire.ReadResult(document.RootElement, png));
    }

    [Fact]
    public void Bytes_that_are_no_png_are_refused_and_a_png_beside_a_result_without_an_image_too()
    {
        using var image = JsonDocument.Parse("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"image\",\"width\":1,\"height\":1,\"text\":\"\"}}");
        using var none = JsonDocument.Parse("{\"status\":\"ok\",\"frame\":1,\"payload\":{\"kind\":\"none\"}}");

        Assert.Throws<FormatException>(() => AutomationWire.ReadResult(image.RootElement, [0x89, 1, 2, 3]));
        Assert.Throws<FormatException>(() => AutomationWire.ReadResult(none.RootElement, Png(1, 1)));
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0 / 60.0)]
    [InlineData(599.999999)]
    public void Durations_keep_every_digit_on_the_wire(double seconds)
    {
        AutomationCommand[] commands =
        [
            new AutomationCommand.Advance(seconds),
            new AutomationCommand.Settle(seconds),
            new AutomationCommand.WaitUntilReady(TimeSpan.FromSeconds(seconds)),
        ];
        foreach (var command in commands)
        {
            var (name, arguments) = AutomationWire.Write(command);
            var read = Assert.IsType<WireCommand.Parsed>(AutomationWire.Parse(name, arguments)).Command;
            switch (read)
            {
                case AutomationCommand.Advance advance:
                    Assert.Equal(seconds, advance.Seconds);
                    break;
                case AutomationCommand.Settle settle:
                    Assert.Equal(seconds, settle.MaxSeconds);
                    break;
                case AutomationCommand.WaitUntilReady ready:
                    Assert.Equal(TimeSpan.FromSeconds(seconds), ready.Timeout);
                    break;
                default:
                    throw new InvalidOperationException(read.ToString());
            }
        }
        Assert.Equal(6, (long)Math.Ceiling(0.1 / ImmediateAutomation.FrameTime));
    }

    private static byte[] Png(int width, int height) =>
        PngSharp.Api.Png.EncodeToByteArray(PngSharp.Api.Png.CreateRgba(width, height, new byte[width * height * 4]));
    [Fact]
    public void Inspection_takes_only_capabilities_snapshot_and_screenshot()
    {
        Assert.Equal(["gui_capabilities", "gui_snapshot", "gui_screenshot"], AutomationWire.Names(AutomationCapabilitySet.Inspection));
        Assert.Equal(AutomationWire.All.Count, AutomationWire.Names(AutomationCapabilitySet.Automation).Count);
    }

    [Fact]
    public void Commands_check_their_values_as_they_are_made()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WindowPoint(float.NaN, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WindowPoint(0f, 0f, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AutomationCommand.Advance(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AutomationCommand.WaitUntilReady(TimeSpan.FromHours(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AutomationCommand.Scroll(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CaptureTarget.Element(Selector.WithText("a"), magnify: 0));
        Assert.Throws<ArgumentException>(() => new AutomationCommand.PickSystemMenuItem(["File"]));
    }

    /// <summary>A command's values, as one string to compare: records holding a <see cref="Selector"/> or a list
    /// compare those by reference.</summary>
    private static string Describe(AutomationCommand command) => AutomationWire.Write(command).Name + " " + AutomationWire.Write(command).Arguments;
}

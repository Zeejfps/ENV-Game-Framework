using System.Globalization;
using System.Text;
using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

/// <summary>
/// Carries out <see cref="AutomationCommand"/>s on an app, whatever they arrived by: every command on an app in
/// automation mode, through its <see cref="ImmediateAutomation"/>, or only inspection on an app running as usual,
/// through its <see cref="ImmediateInspection"/>, where every command that would drive it is
/// <see cref="AutomationResult.Unsupported"/>. Safe from any thread but the app's UI thread, where it rejects every
/// command at once. Commands run one at a time, a second waiting its turn, so two clients cannot interleave the steps of
/// a click; a command cancelled while it waits is dropped, and one cancelled while it runs stops before its next input
/// or frame.
/// </summary>
public sealed class AutomationRunner
{
    /// <summary>How long an inspection waits for the frame it asks for.</summary>
    public static readonly TimeSpan InspectionTimeout = TimeSpan.FromSeconds(10);

    private readonly Func<AutomationCommand, CancellationToken, AutomationResult> _run;
    private readonly Func<bool> _onUiThread;
    private readonly SemaphoreSlim _turn = new(1, 1);

    private AutomationRunner(AutomationCapabilitySet capabilities, Func<bool> onUiThread,
        Func<AutomationCommand, CancellationToken, AutomationResult> run)
    {
        Capabilities = capabilities;
        _onUiThread = onUiThread;
        _run = run;
    }

    public AutomationCapabilitySet Capabilities { get; }

    public static AutomationRunner Driving(ImmediateAutomation automation) =>
        new(AutomationCapabilitySet.Automation, () => automation.OnUiThread,
            (command, cancel) => new Driven(automation, cancel).Run(command));

    public static AutomationRunner Inspecting(ImmediateInspection inspection) =>
        new(AutomationCapabilitySet.Inspection, () => inspection.OnUiThread,
            (command, cancel) => new Inspected(inspection, cancel).Run(command));

    /// <summary>Runs <paramref name="command"/>. A value it cannot use, an app that has closed and a call from the UI
    /// thread are <see cref="AutomationResult.Rejected"/>; a UI thread that did not take it in time, and a command
    /// <paramref name="cancel"/> stopped, are a <see cref="AutomationResult.Timeout"/>.</summary>
    public AutomationResult Run(AutomationCommand command, CancellationToken cancel = default)
    {
        if (_onUiThread())
            return new AutomationResult.Rejected("A command was sent from the app's UI thread, which would wait for itself; send it from another thread.");
        using var turn = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        turn.CancelAfter(TurnLimit(command));
        try
        {
            _turn.Wait(turn.Token);
        }
        catch (OperationCanceledException)
        {
            return Stopped(cancel, command, "waiting for the command before it to finish");
        }
        try
        {
            return _run(command, turn.Token);
        }
        catch (OperationCanceledException) when (turn.IsCancellationRequested)
        {
            return Stopped(cancel, command, "running; it injected nothing after, and let go of what it held");
        }
        catch (ObjectDisposedException closed)
        {
            return new AutomationResult.Rejected(closed.Message);
        }
        catch (TimeoutException timedOut)
        {
            return new AutomationResult.Timeout(timedOut.Message, null);
        }
        catch (ArgumentException invalid)
        {
            return new AutomationResult.Rejected(invalid.Message);
        }
        finally
        {
            _turn.Release();
        }
    }

    /// <summary>How long one command may hold, or wait for, the turn: its own wait and a margin, or a bound past which
    /// a UI thread that stopped answering no longer keeps every later command out.</summary>
    public static TimeSpan TurnLimit(AutomationCommand command) => command switch
    {
        AutomationCommand.WaitFor waitFor => waitFor.Timeout + TurnMargin,
        AutomationCommand.WaitUntilReady ready => ready.Timeout + TurnMargin,
        _ => DefaultTurnLimit,
    };

    private static readonly TimeSpan TurnMargin = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultTurnLimit = TimeSpan.FromSeconds(120);

    private static AutomationResult Stopped(CancellationToken cancel, AutomationCommand command, string when) =>
        new AutomationResult.Timeout(cancel.IsCancellationRequested
            ? $"The client cancelled the command while it was {when}."
            : $"The command ran past its turn's limit of {TurnLimit(command).TotalSeconds:0}s while it was {when}.", null);

    private static AutomationResult.Ok Ok(long frame, AutomationPayload payload) => new(frame, payload);

    private static AutomationPayload.Capabilities CapabilitiesOf(AutomationCapabilitySet set) =>
        new(AutomationWire.ProtocolVersion, set, AutomationWire.Names(set));

    /// <summary>The failure <paramref name="target"/>, no success, is, with the snapshot it was found in.</summary>
    private static AutomationResult Failure(TargetResult target, SemanticSnapshot snapshot)
    {
        var message = TargetText.Problem(target);
        var text = snapshot.ToText();
        return target switch
        {
            TargetResult.NotFound => new AutomationResult.NotFound(message, text),
            TargetResult.Ambiguous => new AutomationResult.Ambiguous(message, text),
            TargetResult.Occluded => new AutomationResult.Occluded(message, text),
            TargetResult.Blocked => new AutomationResult.Blocked(message, text),
            TargetResult.Clipped => new AutomationResult.Clipped(message, text),
            TargetResult.Unreachable => new AutomationResult.Unreachable(message, text),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "No failure."),
        };
    }

    private static readonly TimeSpan SnapshotAfterTimeoutLimit = TimeSpan.FromSeconds(2);

    private static string Scale(float scale) => scale.ToString("0.##", CultureInfo.InvariantCulture);

    private sealed class Driven(ImmediateAutomation app, CancellationToken cancel)
    {
        private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(5);

        private readonly List<PointerButton> _heldButtons = new();
        private readonly List<(KeyboardKey Key, KeyModifiers Modifiers)> _heldKeys = new();
        private bool _queuedInput;

        /// <summary>Runs <paramref name="command"/>; cancelled partway, it drops what it queued that no frame saw and lets
        /// go of what it holds, so it leaves neither a click nor a held button or key to the next command.</summary>
        public AutomationResult Run(AutomationCommand command)
        {
            try
            {
                return Unguarded(command);
            }
            catch (OperationCanceledException) when (cancel.IsCancellationRequested)
            {
                if (_queuedInput)
                    app.AbandonInput(_heldButtons, _heldKeys, CleanupTimeout);
                throw;
            }
        }

        private void Press(PointerButton button, KeyModifiers modifiers)
        {
            if (!_heldButtons.Contains(button))
                _heldButtons.Add(button);
            _queuedInput = true;
            app.Press(button, modifiers, cancel);
        }

        private void Release(PointerButton button, KeyModifiers modifiers)
        {
            _queuedInput = true;
            app.Release(button, modifiers, cancel);
        }

        private AutomationResult Unguarded(AutomationCommand command)
        {
            switch (command)
            {
                case AutomationCommand.Capabilities:
                    return Ok(app.LastFrame(cancel), CapabilitiesOf(AutomationCapabilitySet.Automation));
                case AutomationCommand.Snapshot:
                    var snapshot = app.Snapshot(cancel);
                    return Ok(snapshot.Frame, new AutomationPayload.Snapshot(snapshot.ToText()));
                case AutomationCommand.Capture capture:
                    return Capture(capture.Target);
                case AutomationCommand.MovePointer move:
                    _queuedInput = true;
                    app.MovePointer(InMainWindow(move.At), cancel);
                    return Frames(move.Frames);
                case AutomationCommand.PointerDown down:
                    Press(down.Button, down.Modifiers);
                    return Frames(down.Frames);
                case AutomationCommand.PointerUp up:
                    Release(up.Button, up.Modifiers);
                    return Frames(up.Frames);
                case AutomationCommand.Click click:
                    _queuedInput = true;
                    app.MovePointer(InMainWindow(click.At), cancel);
                    app.Step(1, cancel);
                    PressAndRelease(click.Button, click.Modifiers);
                    return Done();
                case AutomationCommand.Drag drag:
                    return Drag(drag);
                case AutomationCommand.Scroll scroll:
                    _queuedInput = true;
                    app.Scroll(scroll.X, scroll.Y, cancel);
                    return Frames(scroll.Frames);
                case AutomationCommand.Key key:
                    switch (key.Action)
                    {
                        case KeyAction.Press:
                            _queuedInput = true;
                            app.PressKey(key.Pressed, key.Modifiers, cancel);
                            break;
                        case KeyAction.Down:
                            _queuedInput = true;
                            app.KeyDown(key.Pressed, key.Modifiers, cancel);
                            _heldKeys.Add((key.Pressed, key.Modifiers));
                            break;
                        case KeyAction.Up:
                            _queuedInput = true;
                            app.KeyUp(key.Pressed, key.Modifiers, cancel);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(command), key.Action, null);
                    }
                    return Frames(key.Frames);
                case AutomationCommand.TypeText type:
                    _queuedInput = true;
                    app.Type(type.Text, cancel);
                    return Frames(type.Frames);
                case AutomationCommand.Compose compose:
                    _queuedInput = true;
                    app.Compose(compose.Text, compose.Caret, cancel);
                    return Frames(compose.Frames);
                case AutomationCommand.MoveTo moveTo:
                    return MoveTo(moveTo.Selector) ?? Done();
                case AutomationCommand.ClickOn clickOn:
                    var unreached = MoveTo(clickOn.Selector);
                    if (unreached != null)
                        return unreached;
                    PressAndRelease(clickOn.Button, clickOn.Modifiers);
                    return Done();
                case AutomationCommand.WaitFor waitFor:
                    return WaitFor(waitFor);
                case AutomationCommand.Step step:
                    app.Step(step.Count, cancel);
                    return Done();
                case AutomationCommand.Advance advance:
                    app.Advance(advance.Seconds, cancel);
                    return Done();
                case AutomationCommand.Settle settle:
                    var settled = app.Settle(settle.MaxSeconds, cancel);
                    return Ok(app.LastFrame(cancel), new AutomationPayload.Settled(settled));
                case AutomationCommand.WaitUntilReady ready:
                    return WaitUntilReady(ready.Timeout);
                case AutomationCommand.QueueFilePicker picker:
                    if (picker.Path == null)
                        app.QueueFilePickerCancel(cancel);
                    else
                        app.QueueFilePickerAnswer(picker.Path, cancel);
                    return Done();
                case AutomationCommand.FilePickerRequests:
                    return Ok(app.LastFrame(cancel), new AutomationPayload.FilePickers(app.FilePickerRequests(cancel)));
                case AutomationCommand.GetClipboard:
                    return Ok(app.LastFrame(cancel), new AutomationPayload.Clipboard(app.GetClipboardText(cancel), app.GetClipboardHtml(cancel)));
                case AutomationCommand.SetClipboard set:
                    app.SetClipboardText(set.Text, cancel);
                    return Done();
                case AutomationCommand.SystemMenuItems:
                    return Ok(app.LastFrame(cancel), new AutomationPayload.MenuItems(app.SystemMenuItems(cancel)));
                case AutomationCommand.PickSystemMenuItem pick:
                    app.PickSystemMenuItem(pick.Path, cancel);
                    return Done();
                case AutomationCommand.Effects:
                    return Ok(app.LastFrame(cancel), new AutomationPayload.Effects(app.Effects(cancel)));
                case AutomationCommand.Close:
                    var last = app.LastFrame(cancel);
                    app.Close(cancel);
                    return Ok(last, new AutomationPayload.None());
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, "A command this server does not know.");
            }
        }

        private AutomationResult Done() => Ok(app.LastFrame(cancel), new AutomationPayload.None());

        private AutomationResult Frames(int count)
        {
            if (count > 0)
                app.Step(count, cancel);
            return Done();
        }

        private void PressAndRelease(PointerButton button, KeyModifiers modifiers)
        {
            Press(button, modifiers);
            app.Step(1, cancel);
            Release(button, modifiers);
            app.Step(1, cancel);
        }

        private AutomationResult Drag(AutomationCommand.Drag drag)
        {
            var from = InMainWindow(drag.From);
            var to = InMainWindow(drag.To);
            _queuedInput = true;
            app.MovePointer(from, cancel);
            app.Step(1, cancel);
            Press(drag.Button, drag.Modifiers);
            app.Step(1, cancel);
            for (var i = 1; i <= drag.Steps; i++)
            {
                var t = i / (float)drag.Steps;
                _queuedInput = true;
                app.MovePointer(new PointF(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t), cancel);
                app.Step(1, cancel);
            }
            Release(drag.Button, drag.Modifiers);
            app.Step(1, cancel);
            return Done();
        }

        /// <summary>Moves onto the node and runs a frame, as an in-process harness does; null once there, else why not.</summary>
        private AutomationResult? MoveTo(Selector selector)
        {
            var snapshot = app.Snapshot(cancel);
            var target = snapshot.Target(selector);
            if (target is not TargetResult.Hittable hittable)
                return Failure(target, snapshot);
            _queuedInput = true;
            app.MovePointer(hittable.At, cancel);
            app.Step(1, cancel);
            snapshot = app.Snapshot(cancel);
            var resolved = snapshot.Resolve(selector);
            if (resolved is not TargetResult.Found found)
                return Failure(resolved, snapshot);
            var reach = snapshot.ReachAt(found.Node, hittable.At);
            if (reach is not TargetResult.Hittable)
                return Failure(reach, snapshot);
            return null;
        }

        private AutomationResult WaitFor(AutomationCommand.WaitFor waitFor)
        {
            try
            {
                var node = app.WaitFor(waitFor.Selector, waitFor.Timeout, cancel);
                return Ok(app.LastFrame(cancel), new AutomationPayload.Node(NodeSummary.Of(node)));
            }
            catch (TimeoutException timedOut)
            {
                var snapshot = SnapshotAfterTimeout();
                if (snapshot == null)
                    return new AutomationResult.Timeout(timedOut.Message, null);
                var resolved = snapshot.Resolve(waitFor.Selector);
                var detail = resolved is TargetResult.Found found
                    ? $"{found.Node} is there, but the frames never became ready."
                    : TargetText.Problem(resolved);
                return new AutomationResult.Timeout(timedOut.Message + "\n" + detail, snapshot.ToText());
            }
        }

        private AutomationResult WaitUntilReady(TimeSpan timeout)
        {
            try
            {
                app.WaitUntilReady(timeout, cancel);
                return Done();
            }
            catch (TimeoutException timedOut)
            {
                return new AutomationResult.Timeout(timedOut.Message, SnapshotAfterTimeout()?.ToText());
            }
        }

        /// <summary>The last frame's snapshot, unless the UI thread, which a wait may have timed out on, does not take
        /// the call within <see cref="SnapshotAfterTimeoutLimit"/>.</summary>
        private SemanticSnapshot? SnapshotAfterTimeout()
        {
            try
            {
                return app.Snapshot(SnapshotAfterTimeoutLimit);
            }
            catch (TimeoutException)
            {
                return null;
            }
        }

        private PointF InMainWindow(WindowPoint at)
        {
            if (at.Surface == 0)
                return new PointF(at.X, at.Y);
            var surfaces = app.Snapshot(cancel).Surfaces;
            if (at.Surface >= surfaces.Count)
                throw new ArgumentOutOfRangeException(nameof(at), at.Surface, $"The last frame drew {surfaces.Count} surfaces.");
            var rect = surfaces[at.Surface].Rect;
            return new PointF(rect.X + at.X, rect.Y + at.Y);
        }

        private AutomationResult Capture(CaptureTarget target)
        {
            var snapshot = app.Snapshot(cancel);
            switch (target)
            {
                case CaptureTarget.Composite:
                    var main = snapshot.Surfaces[0];
                    var image = CapturedImage.Of(app.Capture(main, cancel));
                    for (var i = 1; i < snapshot.Surfaces.Count; i++)
                    {
                        var surface = snapshot.Surfaces[i];
                        image = image.Overlaid(CapturedImage.Of(app.Capture(surface, cancel)),
                            (int)MathF.Round(surface.Rect.X * main.DpiScale), (int)MathF.Round(surface.Rect.Y * main.DpiScale));
                    }
                    var header = $"# png: the main window with every window and popup over it, {image.Width}x{image.Height} px at @{Scale(main.DpiScale)}x: pixel = UI point x {Scale(main.DpiScale)}\n";
                    return Image(snapshot, image, header + Animating() + snapshot.ToText());
                case CaptureTarget.Surface one:
                    if (one.Index >= snapshot.Surfaces.Count)
                        return new AutomationResult.Rejected($"The last frame drew {snapshot.Surfaces.Count} surfaces; there is no surface {one.Index}.");
                    var shown = snapshot.Surfaces[one.Index];
                    var alone = CapturedImage.Of(app.Capture(shown, cancel));
                    return Image(snapshot, alone,
                        $"# png: surface {one.Index} alone, {alone.Width}x{alone.Height} px at @{Scale(shown.DpiScale)}x\n" + Animating() + snapshot.ToText());
                case CaptureTarget.Element element:
                    var resolved = snapshot.Resolve(element.Selector);
                    if (resolved is not TargetResult.Found found)
                        return Failure(resolved, snapshot);
                    var node = found.Node;
                    var crop = Crop(CapturedImage.Of(app.Capture(node.Surface, cancel)), node.Surface, node.Bounds, element.Margin).Magnify(element.Magnify);
                    var text = new StringBuilder();
                    text.Append("# ").Append(element.Selector).Append(" in frame ").Append(snapshot.Frame).Append(", ")
                        .Append(element.Margin.ToString(CultureInfo.InvariantCulture)).Append("pt around it, ").Append(element.Magnify).Append("x\n");
                    AppendSubtree(text, node, 0);
                    return Image(snapshot, crop, text.ToString());
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        private string Animating()
        {
            var pending = app.PendingNow(cancel);
            var reasons = new List<string>();
            if ((pending & FramePending.Animation) != 0)
                reasons.Add("an animation asks for the next frame");
            if ((pending & FramePending.ScheduledUpdate) != 0)
                reasons.Add("an update is scheduled");
            return reasons.Count == 0 ? "" : $"# still animating: {string.Join(", ", reasons)}; settle runs it out\n";
        }

        private static AutomationResult Image(SemanticSnapshot snapshot, CapturedImage image, string text) =>
            Ok(snapshot.Frame, new AutomationPayload.Image(image.ToPng(), image.Width, image.Height, text));

        /// <summary>The pixels of <paramref name="image"/>, <paramref name="surface"/>'s, around <paramref name="bounds"/>,
        /// which are in main-window UI points.</summary>
        private static CapturedImage Crop(CapturedImage image, SemanticSurface surface, UiRect bounds, float margin)
        {
            var scale = surface.DpiScale;
            var x = bounds.X - surface.Rect.X;
            var y = bounds.Y - surface.Rect.Y;
            var left = Math.Clamp((int)MathF.Floor((x - margin) * scale), 0, image.Width - 1);
            var top = Math.Clamp((int)MathF.Floor((y - margin) * scale), 0, image.Height - 1);
            var right = Math.Clamp((int)MathF.Ceiling((x + bounds.Width + margin) * scale), left + 1, image.Width);
            var bottom = Math.Clamp((int)MathF.Ceiling((y + bounds.Height + margin) * scale), top + 1, image.Height);
            return image.Crop(left, top, right - left, bottom - top);
        }

        private static void AppendSubtree(StringBuilder text, SemanticNode node, int depth)
        {
            text.Append(' ', depth * 2).Append(node).Append('\n');
            foreach (var child in node.Children)
                AppendSubtree(text, child, depth + 1);
        }
    }

    private sealed class Inspected(ImmediateInspection app, CancellationToken cancel)
    {
        public AutomationResult Run(AutomationCommand command)
        {
            switch (command)
            {
                case AutomationCommand.Capabilities:
                    return Ok(app.FrameNumber, CapabilitiesOf(AutomationCapabilitySet.Inspection));
                case AutomationCommand.Snapshot:
                    var snapshot = app.Snapshot(InspectionTimeout, cancel);
                    return Ok(snapshot.Frame, new AutomationPayload.Snapshot(snapshot.ToText()));
                case AutomationCommand.Capture capture:
                    return Capture(capture.Target);
                case AutomationCommand.MovePointer or AutomationCommand.PointerDown or AutomationCommand.PointerUp or AutomationCommand.Click
                    or AutomationCommand.Drag or AutomationCommand.Scroll or AutomationCommand.Key or AutomationCommand.TypeText
                    or AutomationCommand.Compose or AutomationCommand.MoveTo or AutomationCommand.ClickOn or AutomationCommand.WaitFor
                    or AutomationCommand.Step or AutomationCommand.Advance or AutomationCommand.Settle or AutomationCommand.WaitUntilReady
                    or AutomationCommand.QueueFilePicker or AutomationCommand.FilePickerRequests or AutomationCommand.GetClipboard
                    or AutomationCommand.SetClipboard or AutomationCommand.SystemMenuItems or AutomationCommand.PickSystemMenuItem
                    or AutomationCommand.Effects or AutomationCommand.Close:
                    return Unsupported(command);
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, "A command this server does not know.");
            }
        }

        private AutomationResult Capture(CaptureTarget target)
        {
            switch (target)
            {
                case CaptureTarget.Composite:
                case CaptureTarget.Surface { Index: 0 }:
                    var frame = app.Capture(InspectionTimeout, cancel);
                    var snapshot = frame.Snapshot;
                    var png = frame.Png;
                    var (width, height) = CapturedImage.PngSize(png);
                    var scale = Scale(snapshot.Surfaces[0].DpiScale);
                    var header = $"# png: the main window alone, as the screen showed it in frame {snapshot.Frame}, {width}x{height} px at @{scale}x: pixel = UI point x {scale}; an inspection-only server captures no popup or window of its own, which the snapshot still lists\n";
                    return Ok(snapshot.Frame, new AutomationPayload.Image(png, width, height, header + snapshot.ToText()));
                case CaptureTarget.Surface or CaptureTarget.Element:
                    return new AutomationResult.Unsupported("An inspection-only server captures the main window alone, as the screen shows it.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        private static AutomationResult Unsupported(AutomationCommand command) => new AutomationResult.Unsupported(
            $"{AutomationWire.Write(command).Name} drives the app, and this server only inspects an app running as usual: "
            + "gui_snapshot and gui_screenshot. Drive an app through RemoteApp or GpuFrames, in automation mode.");
    }
}

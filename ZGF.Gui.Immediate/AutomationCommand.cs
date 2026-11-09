using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

/// <summary>A point in one window of the app, in UI points from that window's top-left: <see cref="Surface"/> 0 is
/// the main window, then each popup and window of its own, as <see cref="SemanticSnapshot.Surfaces"/> lists them.</summary>
public readonly record struct WindowPoint
{
    public WindowPoint(float x, float y, int surface = 0)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y))
            throw new ArgumentOutOfRangeException(nameof(x), $"A point is two finite numbers, not ({x}, {y}).");
        ArgumentOutOfRangeException.ThrowIfNegative(surface);
        X = x;
        Y = y;
        Surface = surface;
    }

    public float X { get; }
    public float Y { get; }
    public int Surface { get; }
}

public enum KeyAction
{
    /// <summary>Down, then up.</summary>
    Press,
    Down,
    Up,
}

/// <summary>What a capture shows.</summary>
public abstract record CaptureTarget
{
    private CaptureTarget()
    {
    }

    /// <summary>The main window with every popup and window drawn over it where it is.</summary>
    public sealed record Composite : CaptureTarget;

    /// <summary>One surface alone, at its own scale.</summary>
    public sealed record Surface(int Index) : CaptureTarget;

    /// <summary>The node <see cref="Selector"/> names: its own surface around it, <see cref="Margin"/> points beyond
    /// its bounds, each pixel <see cref="Magnify"/> times as wide and high.</summary>
    public sealed record Element : CaptureTarget
    {
        public Element(Selector selector, float margin = 8f, int magnify = 1)
        {
            LookArguments.RequireCrop(margin, magnify);
            Selector = selector;
            Margin = margin;
            Magnify = magnify;
        }

        public Selector Selector { get; }
        public float Margin { get; }
        public int Magnify { get; }
    }
}

/// <summary>
/// One request to an app in automation mode, independent of how it travels. Input commands queue their input and
/// then run <c>Frames</c> frames without moving the clock, so what they caused is there to read; 0 only queues.
/// Every constructor checks its values: finite coordinates and durations, counts in range.
/// </summary>
public abstract record AutomationCommand
{
    /// <summary>The most frames one input command or <see cref="Step"/> may run.</summary>
    public const int MaxFrames = 10_000;

    /// <summary>The longest real time a wait may be given.</summary>
    public static readonly TimeSpan MaxTimeout = TimeSpan.FromMinutes(10);

    private AutomationCommand()
    {
    }

    /// <summary>The protocol version, which capabilities the server offers, and the commands it takes.</summary>
    public sealed record Capabilities : AutomationCommand;

    public sealed record Snapshot : AutomationCommand;

    public sealed record Capture(CaptureTarget Target) : AutomationCommand;

    public sealed record MovePointer : AutomationCommand
    {
        public MovePointer(WindowPoint at, int frames = 1)
        {
            At = at;
            Frames = RequireFrames(frames);
        }

        public WindowPoint At { get; }
        public int Frames { get; }
    }

    public sealed record PointerDown : AutomationCommand
    {
        public PointerDown(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None, int frames = 1)
        {
            Button = button;
            Modifiers = modifiers;
            Frames = RequireFrames(frames);
        }

        public PointerButton Button { get; }
        public KeyModifiers Modifiers { get; }
        public int Frames { get; }
    }

    public sealed record PointerUp : AutomationCommand
    {
        public PointerUp(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None, int frames = 1)
        {
            Button = button;
            Modifiers = modifiers;
            Frames = RequireFrames(frames);
        }

        public PointerButton Button { get; }
        public KeyModifiers Modifiers { get; }
        public int Frames { get; }
    }

    /// <summary>Moves to <see cref="At"/>, lets a frame see the pointer there, then presses and releases over two more.</summary>
    public sealed record Click(WindowPoint At, PointerButton Button = PointerButton.Left, KeyModifiers Modifiers = KeyModifiers.None)
        : AutomationCommand;

    /// <summary>Presses at <see cref="From"/>, moves to <see cref="To"/> in <see cref="Steps"/> steps a frame apart,
    /// and releases there.</summary>
    public sealed record Drag : AutomationCommand
    {
        public Drag(WindowPoint from, WindowPoint to, int steps = 4, PointerButton button = PointerButton.Left,
            KeyModifiers modifiers = KeyModifiers.None)
        {
            if (steps is < 1 or > 1000)
                throw new ArgumentOutOfRangeException(nameof(steps), steps, "A drag takes from 1 to 1000 steps.");
            From = from;
            To = to;
            Steps = steps;
            Button = button;
            Modifiers = modifiers;
        }

        public WindowPoint From { get; }
        public WindowPoint To { get; }
        public int Steps { get; }
        public PointerButton Button { get; }
        public KeyModifiers Modifiers { get; }
    }

    public sealed record Scroll : AutomationCommand
    {
        public Scroll(double x, double y, int frames = 1)
        {
            if (!double.IsFinite(x) || !double.IsFinite(y))
                throw new ArgumentOutOfRangeException(nameof(x), $"A scroll is two finite numbers, not ({x}, {y}).");
            X = x;
            Y = y;
            Frames = RequireFrames(frames);
        }

        public double X { get; }
        public double Y { get; }
        public int Frames { get; }
    }

    public sealed record Key : AutomationCommand
    {
        public Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None, KeyAction action = KeyAction.Press, int frames = 1)
        {
            if (key == KeyboardKey.Unknown)
                throw new ArgumentOutOfRangeException(nameof(key), key, "Name a key.");
            Pressed = key;
            Modifiers = modifiers;
            Action = action;
            Frames = RequireFrames(frames);
        }

        public KeyboardKey Pressed { get; }
        public KeyModifiers Modifiers { get; }
        public KeyAction Action { get; }
        public int Frames { get; }
    }

    public sealed record TypeText : AutomationCommand
    {
        public TypeText(string text, int frames = 1)
        {
            Text = text;
            Frames = RequireFrames(frames);
        }

        public string Text { get; }
        public int Frames { get; }
    }

    /// <summary>The IME replaces its composition with <see cref="Text"/>, caret at <see cref="Caret"/>, the end when
    /// null; an empty text ends it.</summary>
    public sealed record Compose : AutomationCommand
    {
        public Compose(string text, int? caret = null, int frames = 1)
        {
            if (caret != null && (caret < 0 || caret > text.Length))
                throw new ArgumentOutOfRangeException(nameof(caret), caret, $"The caret is from 0 to the text's length, {text.Length}.");
            Text = text;
            Caret = caret;
            Frames = RequireFrames(frames);
        }

        public string Text { get; }
        public int? Caret { get; }
        public int Frames { get; }
    }

    /// <summary>Moves the pointer onto the node <see cref="Selector"/> names, at a point a press reaches it, and runs a
    /// frame; moves nothing when no such point exists, and fails when after that frame a press would not reach it.</summary>
    public sealed record MoveTo(Selector Selector) : AutomationCommand;

    /// <summary>As <see cref="MoveTo"/>, then a press and a release over two more frames.</summary>
    public sealed record ClickOn(Selector Selector, PointerButton Button = PointerButton.Left, KeyModifiers Modifiers = KeyModifiers.None)
        : AutomationCommand;

    public sealed record WaitFor : AutomationCommand
    {
        public WaitFor(Selector selector, TimeSpan timeout)
        {
            Selector = selector;
            Timeout = RequireTimeout(timeout);
        }

        public Selector Selector { get; }
        public TimeSpan Timeout { get; }
    }

    public sealed record Step : AutomationCommand
    {
        public Step(int count = 1)
        {
            Count = RequireFrames(count);
        }

        public int Count { get; }
    }

    public sealed record Advance : AutomationCommand
    {
        public Advance(double seconds)
        {
            Seconds = RequireSeconds(seconds);
        }

        public double Seconds { get; }
    }

    public sealed record Settle : AutomationCommand
    {
        public Settle(double maxSeconds = 5)
        {
            MaxSeconds = RequireSeconds(maxSeconds);
        }

        public double MaxSeconds { get; }
    }

    public sealed record WaitUntilReady : AutomationCommand
    {
        public WaitUntilReady(TimeSpan timeout)
        {
            Timeout = RequireTimeout(timeout);
        }

        public TimeSpan Timeout { get; }
    }

    /// <summary>The next file picker the app opens is answered with <see cref="Path"/>, or cancelled when null.</summary>
    public sealed record QueueFilePicker(string? Path) : AutomationCommand;

    public sealed record FilePickerRequests : AutomationCommand;

    public sealed record GetClipboard : AutomationCommand;

    public sealed record SetClipboard(string Text) : AutomationCommand;

    public sealed record SystemMenuItems : AutomationCommand;

    public sealed record PickSystemMenuItem : AutomationCommand
    {
        public PickSystemMenuItem(IReadOnlyList<string> path)
        {
            if (path.Count < 2)
                throw new ArgumentException("A menu item's path is the titles of the menus leading to it, then its own.", nameof(path));
            Path = path.ToArray();
        }

        public IReadOnlyList<string> Path { get; }
    }

    public sealed record Effects : AutomationCommand;

    public sealed record Close : AutomationCommand;

    private static int RequireFrames(int frames)
    {
        if (frames is < 0 or > MaxFrames)
            throw new ArgumentOutOfRangeException(nameof(frames), frames, $"Frames are from 0 to {MaxFrames}.");
        return frames;
    }

    private static double RequireSeconds(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > ImmediateAutomation.MaxSeconds)
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, $"A duration is a finite number of seconds from 0 to {ImmediateAutomation.MaxSeconds}.");
        return seconds;
    }

    private static TimeSpan RequireTimeout(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero || timeout > MaxTimeout)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, $"A timeout is from 0 to {MaxTimeout.TotalSeconds} seconds.");
        return timeout;
    }
}

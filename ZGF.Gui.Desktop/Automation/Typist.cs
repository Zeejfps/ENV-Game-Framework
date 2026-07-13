using System.Text;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Automation;

/// <summary>
/// Replays a keystroke script at human speed into an <see cref="ITypeSink"/> — a live window or a
/// headless tree. Characters go in one at a time with a pause between them, so the app sees the same
/// event cadence a person produces rather than one atomic paste.
///
/// Waiting is injected rather than hard-coded to <c>Thread.Sleep</c>: a live driver sleeps the script
/// thread while the UI keeps pumping, while the test harness advances its own frame clock instead —
/// the same script, no wall-clock cost in tests.
/// </summary>
public sealed class Typist
{
    private readonly record struct Step(float Delay, Action Act, string? Note);

    private readonly List<Step> _steps = [];
    private readonly ITypeSink _sink;
    private readonly Action<float> _wait;

    public Typist(ITypeSink sink, Action<float>? wait = null)
    {
        _sink = sink;
        _wait = wait ?? (seconds => Thread.Sleep(TimeSpan.FromSeconds(seconds)));
    }

    /// <summary>Characters per second — roughly a brisk typist by default.</summary>
    public float Speed { get; init; } = 16f;

    /// <summary>Called as each annotated step runs, so a caller can narrate the script.</summary>
    public Action<string>? OnNote { get; set; }

    /// <summary>Queues <paramref name="text"/> one character at a time, dwelling a little longer on
    /// whitespace so it reads like someone thinking between words.</summary>
    public Typist Type(string text, string? note = null)
    {
        var first = true;
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value == '\r') continue;
            var r = rune;
            var dwell = 1f / Speed * (Rune.IsWhiteSpace(r) ? 2.5f : 1f);
            _steps.Add(new Step(dwell, () => _sink.TypeRune(r), first ? note : null));
            first = false;
        }
        return this;
    }

    public Typist Press(KeyboardKey key, InputModifiers modifiers = InputModifiers.None, string? note = null)
    {
        _steps.Add(new Step(1f / Speed, () => _sink.PressKey(key, modifiers), note));
        return this;
    }

    public Typist Repeat(KeyboardKey key, int times, string? note = null)
    {
        for (var i = 0; i < times; i++)
            Press(key, note: i == 0 ? note : null);
        return this;
    }

    public Typist Pause(float seconds, string? note = null)
    {
        _steps.Add(new Step(seconds, () => { }, note));
        return this;
    }

    /// <summary>Runs the script to completion, blocking the calling thread. Call from a script thread,
    /// never the UI thread — the UI has to keep drawing for any of this to be visible.</summary>
    public void Run()
    {
        foreach (var step in _steps)
        {
            if (step.Note != null) OnNote?.Invoke(step.Note);
            step.Act();
            _wait(step.Delay);
        }
    }
}

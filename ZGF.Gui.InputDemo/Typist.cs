using System.Text;
using ZGF.KeyboardModule;

namespace ZGF.Gui.InputDemo;

/// <summary>
/// Replays a scripted sequence of keystrokes into a live window at human speed, one step per elapsed
/// interval, driven off the frame ticker. Deliberately not a loop with sleeps: it has to advance on
/// the UI thread's own clock, or the window would stop pumping and the typing wouldn't be visible.
/// </summary>
internal sealed class Typist
{
    private readonly record struct Step(float Delay, Action Act, string? Note);

    private readonly List<Step> _steps = [];
    private readonly ITypeSink _sink;
    private int _next;
    private float _waited;

    public Typist(ITypeSink sink) => _sink = sink;

    public bool Done => _next >= _steps.Count;

    /// <summary>Fires when a step runs, so the caller can request a redraw or narrate.</summary>
    public Action<string>? OnNote { get; set; }

    public Typist Pause(float seconds, string? note = null)
    {
        _steps.Add(new Step(seconds, () => { }, note));
        return this;
    }

    /// <summary>Queues <paramref name="text"/> a character at a time, with a pause after each — longer
    /// after whitespace, so it reads like someone thinking between words rather than a paste.</summary>
    public Typist Type(string text, string? note = null)
    {
        var first = true;
        foreach (var rune in text.EnumerateRunes())
        {
            var r = rune;
            var dwell = Rune.IsWhiteSpace(r) ? 0.16f : 0.055f;
            _steps.Add(new Step(dwell, () => _sink.Type(r), first ? note : null));
            first = false;
        }
        return this;
    }

    public Typist Press(KeyboardKey key, bool control = false, string? note = null)
    {
        _steps.Add(new Step(0.12f, () => _sink.Press(key, control), note));
        return this;
    }

    public Typist Repeat(KeyboardKey key, int times, string? note = null)
    {
        for (var i = 0; i < times; i++)
            Press(key, note: i == 0 ? note : null);
        return this;
    }

    /// <summary>Advances the script by <paramref name="dt"/> seconds. Returns true if a step ran, so
    /// the caller knows the frame is worth redrawing.</summary>
    public bool Update(float dt)
    {
        if (Done) return false;

        _waited += dt;
        var step = _steps[_next];
        if (_waited < step.Delay) return false;

        _waited = 0f;
        _next++;
        step.Act();
        if (step.Note != null) OnNote?.Invoke(step.Note);
        return true;
    }
}

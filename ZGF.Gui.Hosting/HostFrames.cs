namespace ZGF.Gui.Hosting;

/// <summary>The canvases of one backend, so that a host frame which throws abandons every canvas frame it began,
/// main, popup and window alike, instead of leaving them to draw a partial frame again.</summary>
internal sealed class HostFrames
{
    private readonly List<RenderedCanvasBase> _canvases = new();
    private readonly List<Dictionary<RenderedCanvasBase, long>> _begunBefore = new();
    private int _depth;

    public void Add(RenderedCanvasBase canvas) => _canvases.Add(canvas);

    public void Remove(RenderedCanvasBase canvas) => _canvases.Remove(canvas);

    /// <summary>Runs <paramref name="frame"/>; if it throws, aborts the frame of every canvas it began one on, a frame
    /// run inside it included, and leaves the canvases an enclosing frame began before it alone.</summary>
    public void Run(Action frame)
    {
        if (_begunBefore.Count == _depth)
            _begunBefore.Add(new Dictionary<RenderedCanvasBase, long>(ReferenceEqualityComparer.Instance));
        var before = _begunBefore[_depth];
        before.Clear();
        foreach (var canvas in _canvases)
            before[canvas] = canvas.BegunFrames;
        _depth++;
        try
        {
            frame();
        }
        catch
        {
            foreach (var canvas in _canvases)
            {
                if (!before.TryGetValue(canvas, out var begun) || canvas.BegunFrames != begun)
                    canvas.AbortFrame();
            }
            throw;
        }
        finally
        {
            before.Clear();
            _depth--;
        }
    }
}

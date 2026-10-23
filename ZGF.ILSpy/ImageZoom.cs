namespace ZGF.ILSpy;

/// <summary>How far a picture is zoomed: one of a fixed set of steps, from a quarter of its own
/// size to 32 times it, starting at its own size.</summary>
public sealed class ImageZoom
{
    private static readonly float[] Steps = [0.25f, 0.5f, 1f, 2f, 4f, 8f, 16f, 32f];
    private const int OwnSizeStep = 2;

    private int _step = OwnSizeStep;

    public float Scale => Steps[_step];

    public int Percent => (int)(Scale * 100f);

    public bool CanZoomIn => _step < Steps.Length - 1;

    public bool CanZoomOut => _step > 0;

    public bool IsOwnSize => _step == OwnSizeStep;

    public void ZoomIn()
    {
        if (!CanZoomIn)
            return;
        _step++;
    }

    public void ZoomOut()
    {
        if (!CanZoomOut)
            return;
        _step--;
    }

    public void Reset() => _step = OwnSizeStep;
}

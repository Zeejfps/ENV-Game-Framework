using PngSharp.Api;

namespace ZGF.Gui.Desktop.Backends;

// One outstanding screenshot request, fulfilled by a render backend once the frame it should
// show has been drawn. Cleared as soon as it is taken, so at most one file is written per request.
internal sealed class PendingScreenshot
{
    public delegate bool Capture(out int width, out int height, out byte[] rgba);

    private string? _path;
    private Action? _done;

    public void Request(string path, Action? onComplete)
    {
        _path = path;
        _done = onComplete;
    }

    public void Fulfil(Capture capture)
    {
        if (_path is not { } path) return;
        _path = null;
        var done = _done;
        _done = null;
        try
        {
            if (capture(out var w, out var h, out var rgba))
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                Png.EncodeToFile(Png.CreateRgba(w, h, rgba), path);
            }
            else
            {
                Console.WriteLine("[Screenshot] no captured frame was produced.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Screenshot] failed: {ex.Message}");
        }
        finally
        {
            done?.Invoke();
        }
    }
}

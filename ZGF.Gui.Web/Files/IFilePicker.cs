namespace ZGF.Gui.Web.Files;

/// <summary>
/// Neutral file-acquisition service. Lives in the web host for now; it belongs in
/// the platform-neutral package next to <c>IClipboard</c> once that exists (see
/// docs/interaction-layer-architecture.md), so components/controllers can depend
/// on it and each platform supplies an implementation.
///
/// Deliberately path-free: the browser sandbox hands back file <em>content</em>,
/// not paths, so the abstraction exposes a stream. Desktop wraps an OS path as a
/// stream behind the same surface.
/// </summary>
public interface IFilePicker
{
    /// <summary>
    /// Opens the platform file picker and resolves with the chosen files (empty if
    /// cancelled). On the web this MUST be invoked synchronously from within a user
    /// gesture (a click/tap handler) or the browser blocks the dialog — see
    /// <see cref="WebFilePicker"/>.
    /// </summary>
    Task<IReadOnlyList<PickedFile>> PickFilesAsync(FilePickOptions options);
}

public sealed class FilePickOptions
{
    public bool Multiple { get; init; }

    /// <summary>HTML-style accept list, e.g. <c>"image/*,.pdf"</c>. Empty = any.</summary>
    public string Accept { get; init; } = "";

    public static FilePickOptions Default { get; } = new();
}

/// <summary>
/// A file chosen via the picker or a drag-and-drop. Content is read lazily and is
/// the only way to get the bytes — there is no path on the web.
/// </summary>
public sealed class PickedFile
{
    public string Name { get; }
    public long Size { get; }
    public string ContentType { get; }
    internal int Handle { get; }

    internal PickedFile(int handle, string name, long size, string contentType)
    {
        Handle = handle;
        Name = name;
        Size = size;
        ContentType = contentType;
    }

    /// <summary>
    /// Reads the whole file into memory and returns a stream over it. (A first cut:
    /// chunked streaming for very large files can replace this behind the same API.)
    /// </summary>
    public async Task<Stream> OpenReadAsync()
    {
        var len = await WebFiles.LoadFile(Handle);
        var bytes = new byte[len];
        if (len > 0)
            WebFiles.ReadInto(Handle, bytes); // synchronous MemoryView copy (no await held)
        WebFiles.FreeFile(Handle);
        return new MemoryStream(bytes, writable: false);
    }
}

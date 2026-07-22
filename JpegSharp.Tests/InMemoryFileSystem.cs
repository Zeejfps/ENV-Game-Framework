using JpegSharp.Api;

namespace JpegSharp.Tests;

/// <summary>
/// An <see cref="IFileSystem"/> that stores files in memory instead of on disk, for exercising
/// the path-based <see cref="Jpeg"/> APIs without touching the real file system.
/// </summary>
internal sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> _files = new();

    public int CreateCount { get; private set; }
    public int OpenReadCount { get; private set; }

    public bool Exists(string path) => _files.ContainsKey(path);

    public Stream Create(string path)
    {
        CreateCount++;
        return new CaptureStream(path, this);
    }

    public Stream OpenRead(string path)
    {
        OpenReadCount++;
        if (!_files.TryGetValue(path, out var bytes))
            throw new FileNotFoundException(path);
        return new MemoryStream(bytes, writable: false);
    }

    // Captures whatever was written into the backing store when the writer disposes the stream.
    private sealed class CaptureStream(string path, InMemoryFileSystem owner) : MemoryStream
    {
        private bool _stored;

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_stored)
            {
                owner._files[path] = ToArray();
                _stored = true;
            }
            base.Dispose(disposing);
        }
    }
}

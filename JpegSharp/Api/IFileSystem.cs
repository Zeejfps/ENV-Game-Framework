namespace JpegSharp.Api;

/// <summary>
/// Abstracts the file operations used by <see cref="Jpeg"/>'s path-based APIs so callers can
/// route reads and writes through their own storage instead of the physical file system.
/// Pass a custom implementation to the path-based <see cref="Jpeg"/> methods to intercept these
/// calls; when omitted they use <see cref="PhysicalFileSystem"/>.
/// </summary>
public interface IFileSystem
{
    /// <summary>Creates or overwrites a file at the given path and returns a writable stream.</summary>
    /// <param name="path">The destination path.</param>
    /// <returns>A writable stream for the newly created file. The caller owns and disposes it.</returns>
    Stream Create(string path);

    /// <summary>Opens an existing file at the given path for reading.</summary>
    /// <param name="path">The source path.</param>
    /// <returns>A readable stream for the file. The caller owns and disposes it.</returns>
    Stream OpenRead(string path);
}

/// <summary>
/// The default <see cref="IFileSystem"/> implementation, backed by the physical file system
/// via <see cref="File"/>.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    /// <inheritdoc />
    public Stream Create(string path)
    {
        return File.Create(path);
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }
}

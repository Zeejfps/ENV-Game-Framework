namespace ZGF.ILSpy.Tests;

/// <summary>A directory of its own under the temp folder, made when a file is first put in it and
/// deleted with everything in it on dispose.</summary>
internal sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"zgf-ilspy-{Guid.NewGuid():N}");

    public string PathOf(string name) => System.IO.Path.Combine(Path, name);

    public string Write(string name, string text)
    {
        var path = Made(name);
        File.WriteAllText(path, text);
        return path;
    }

    public string Write(string name, byte[] bytes)
    {
        var path = Made(name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    public string Copy(string source, string name)
    {
        var path = Made(name);
        File.Copy(source, path);
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    private string Made(string name)
    {
        var path = PathOf(name);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        return path;
    }
}

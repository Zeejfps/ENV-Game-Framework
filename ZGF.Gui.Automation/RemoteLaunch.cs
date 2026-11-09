namespace ZGF.Gui.Automation;

/// <summary>The app a <see cref="RemoteApp"/> starts.</summary>
public abstract record RemoteProgram
{
    private RemoteProgram()
    {
    }

    /// <summary>A framework-dependent app's dll, run with the <c>dotnet</c> host that runs this process when it can tell,
    /// else the one on the PATH.</summary>
    public sealed record Dll(string Path) : RemoteProgram;

    /// <summary>An executable, run as it is.</summary>
    public sealed record Executable(string Path) : RemoteProgram;
}

/// <summary>How a <see cref="RemoteApp"/> starts its app: what runs, with which arguments ahead of the automation
/// flag it adds, and how long it waits for the app's server, then for the app to be ready.</summary>
public sealed record RemoteLaunch(RemoteProgram Program)
{
    public IReadOnlyList<string> Arguments { get; init; } = [];
    public string? WorkingDirectory { get; init; }
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan ReadyTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public static RemoteLaunch OfDll(string path) => new(new RemoteProgram.Dll(path));

    public static RemoteLaunch OfExecutable(string path) => new(new RemoteProgram.Executable(path));
}

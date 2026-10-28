namespace ZGF.ILSpy;

/// <summary>What writing the settings file did. A <c>Reason</c> names the file and says why, as the
/// system said it.</summary>
public abstract record SettingsWrite
{
    private SettingsWrite()
    {
    }

    /// <summary>The file was written, or the settings are kept in memory only and there is none.</summary>
    public sealed record Written : SettingsWrite;

    public sealed record Failed(string Reason) : SettingsWrite;
}

/// <summary>What saving the settings when they changed did.</summary>
public abstract record Saving
{
    private Saving()
    {
    }

    /// <summary>Nothing changed since the last write, or the assemblies are reloading.</summary>
    public sealed record Unchanged : Saving;

    public sealed record Saved : Saving;

    /// <summary>A failed write is tried again only in <see cref="RetryIn"/> seconds.</summary>
    public sealed record Waiting(double RetryIn) : Saving;

    /// <summary>The write failed, and is tried again in <see cref="RetryIn"/> seconds.</summary>
    public sealed record Failed(string Reason, double RetryIn) : Saving;
}

namespace ZGF.ILSpy.Core;

/// <summary>Target frameworks as assemblies name them, e.g. <c>.NETCoreApp,Version=v10.0</c>, and as
/// monikers, e.g. <c>net10.0</c>.</summary>
public static class FrameworkIds
{
    private const string VersionMark = ",Version=v";
    private const string CoreApp = ".NETCoreApp";
    private const string Standard = ".NETStandard";
    private const string Framework = ".NETFramework";
    private const int FirstNetMajor = 5;

    /// <summary>The frameworks offered to resolve an assembly's references against, newest first in
    /// each family.</summary>
    public static IReadOnlyList<string> Common { get; } =
    [
        CoreApp + VersionMark + "11.0",
        CoreApp + VersionMark + "10.0",
        CoreApp + VersionMark + "9.0",
        CoreApp + VersionMark + "8.0",
        CoreApp + VersionMark + "7.0",
        CoreApp + VersionMark + "6.0",
        CoreApp + VersionMark + "5.0",
        CoreApp + VersionMark + "3.1",
        CoreApp + VersionMark + "3.0",
        CoreApp + VersionMark + "2.1",
        Standard + VersionMark + "2.1",
        Standard + VersionMark + "2.0",
        Framework + VersionMark + "4.8.1",
        Framework + VersionMark + "4.8",
        Framework + VersionMark + "4.7.2",
        Framework + VersionMark + "4.7.1",
        Framework + VersionMark + "4.7",
        Framework + VersionMark + "4.6.2",
        Framework + VersionMark + "4.6.1",
        Framework + VersionMark + "4.6",
    ];

    /// <summary><paramref name="id"/> as a moniker, e.g. <c>net10.0</c>, <c>netstandard2.0</c> or
    /// <c>net48</c>; an id of another framework as its name and version.</summary>
    public static string MonikerOf(string id)
    {
        var at = id.IndexOf(VersionMark, StringComparison.Ordinal);
        if (at < 0)
            return id;
        var name = id[..at];
        var version = id[(at + VersionMark.Length)..];
        return name switch
        {
            CoreApp => CoreAppMoniker(version),
            Standard => $"netstandard{version}",
            Framework => $"net{version.Replace(".", "")}",
            _ => $"{name} {version}",
        };
    }

    /// <summary>A .NET Core version's moniker: <c>net</c> from .NET 5 on, <c>netcoreapp</c> before.</summary>
    private static string CoreAppMoniker(string version)
    {
        if (Version.TryParse(version, out var parsed) && parsed.Major >= FirstNetMajor)
            return $"net{version}";
        return $"netcoreapp{version}";
    }
}

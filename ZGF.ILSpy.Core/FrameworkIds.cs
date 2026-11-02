namespace ZGF.ILSpy.Core;

/// <summary>A target framework as assemblies name it, e.g. <c>.NETCoreApp,Version=v10.0</c>: a name, then
/// <c>,Version=v</c> and a version that starts with a digit, with anything after it, such as a profile, kept.
/// Made only by <see cref="Parse"/>, from what an assembly or the settings say.</summary>
public sealed record TargetFrameworkId
{
    internal const string CoreApp = ".NETCoreApp";
    internal const string Standard = ".NETStandard";
    internal const string Framework = ".NETFramework";

    private const string VersionMark = ",Version=v";
    private const string VersionKey = ",Version=";
    private const string ReadableVersionKey = ", ";
    private const int FirstNetMajor = 5;

    private TargetFrameworkId(string text, string name, string version)
    {
        Text = text;
        Name = name;
        Version = version;
    }

    /// <summary>The id as assemblies write it.</summary>
    public string Text { get; }

    /// <summary>The framework's name, e.g. <c>.NETCoreApp</c>.</summary>
    public string Name { get; }

    /// <summary>What follows <c>,Version=v</c>, e.g. <c>10.0</c>.</summary>
    public string Version { get; }

    /// <summary>The id <paramref name="text"/> writes, or null when it is not one.</summary>
    public static TargetFrameworkId? Parse(string? text)
    {
        if (text == null)
            return null;
        var at = text.IndexOf(VersionMark, StringComparison.Ordinal);
        if (at <= 0)
            return null;
        var version = text[(at + VersionMark.Length)..];
        if (version.Length == 0 || !char.IsAsciiDigit(version[0]))
            return null;
        return new TargetFrameworkId(text, text[..at], version);
    }

    internal static TargetFrameworkId Of(string name, string version) => new(name + VersionMark + version, name, version);

    /// <summary>The id as ILSpy shows it, e.g. <c>.NETCoreApp, v10.0</c>.</summary>
    public string Readable => Text.Replace(VersionKey, ReadableVersionKey);

    /// <summary>The id as a moniker, e.g. <c>net10.0</c>, <c>netstandard2.0</c> or <c>net48</c>; an id of
    /// another framework as its name and version.</summary>
    public string Moniker => Name switch
    {
        CoreApp => CoreAppMoniker(Version),
        Standard => $"netstandard{Version}",
        Framework => $"net{Version.Replace(".", "")}",
        _ => $"{Name} {Version}",
    };

    /// <summary>Whether an assembly built for this framework runs on .NET Core: one built for .NET Core
    /// or .NET Standard.</summary>
    internal bool IsCoreOrStandard => Name is CoreApp or Standard;

    public override string ToString() => Text;

    /// <summary>A .NET Core version's moniker: <c>net</c> from .NET 5 on, <c>netcoreapp</c> before.</summary>
    private static string CoreAppMoniker(string version)
    {
        if (System.Version.TryParse(version, out var parsed) && parsed.Major >= FirstNetMajor)
            return $"net{version}";
        return $"netcoreapp{version}";
    }
}

/// <summary>The target frameworks ILSpy offers to resolve an assembly's references against.</summary>
public static class FrameworkIds
{
    /// <summary>The frameworks offered to resolve an assembly's references against, newest first in
    /// each family.</summary>
    public static IReadOnlyList<TargetFrameworkId> Common { get; } =
    [
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "11.0"),
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "10.0"),
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "9.0"),
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "8.0"),
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "7.0"),
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "6.0"),
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "5.0"),
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "3.1"),
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "3.0"),
        TargetFrameworkId.Of(TargetFrameworkId.CoreApp, "2.1"),
        TargetFrameworkId.Of(TargetFrameworkId.Standard, "2.1"),
        TargetFrameworkId.Of(TargetFrameworkId.Standard, "2.0"),
        TargetFrameworkId.Of(TargetFrameworkId.Framework, "4.8.1"),
        TargetFrameworkId.Of(TargetFrameworkId.Framework, "4.8"),
        TargetFrameworkId.Of(TargetFrameworkId.Framework, "4.7.2"),
        TargetFrameworkId.Of(TargetFrameworkId.Framework, "4.7.1"),
        TargetFrameworkId.Of(TargetFrameworkId.Framework, "4.7"),
        TargetFrameworkId.Of(TargetFrameworkId.Framework, "4.6.2"),
        TargetFrameworkId.Of(TargetFrameworkId.Framework, "4.6.1"),
        TargetFrameworkId.Of(TargetFrameworkId.Framework, "4.6"),
    ];

    /// <summary>Whether an assembly built for <paramref name="id"/> runs on .NET Core, as the running
    /// runtime is: one built for .NET Core or .NET Standard, or for a framework not known.</summary>
    internal static bool RunsOnCore(TargetFrameworkId? id) => id == null || id.IsCoreOrStandard;
}

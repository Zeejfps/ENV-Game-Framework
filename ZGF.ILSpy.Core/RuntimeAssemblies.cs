using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace ZGF.ILSpy.Core;

/// <summary>An assembly of the running .NET runtime, offered to open from a picker.</summary>
public sealed record RuntimeAssembly(string Name, string Framework, string FilePath);

/// <summary>The running .NET runtime's shared framework directories and the assemblies in them; for a
/// native build, which runs on no shared framework, those of the newest installed runtime. Each is
/// found once and kept; one that cannot be found for now is looked for again the next time.</summary>
public static class RuntimeAssemblies
{
    private static readonly Lazy<ImmutableArray<(string Framework, string Path)>> LazyDirectories = new(FindDirectories, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<ImmutableArray<string>> LazyDirectoryPaths =
        new(() => LazyDirectories.Value.Select(d => d.Path).ToImmutableArray(), LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<IReadOnlyDictionary<string, string>> LazyFilesByName = new(IndexFiles, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<ImmutableArray<RuntimeAssembly>> LazyAll = new(ScanAssemblies, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>The runtime's own directory first, then the sibling shared frameworks of the same
    /// version (ASP.NET Core, Windows Desktop) that are installed.</summary>
    public static IReadOnlyList<string> Directories => LazyDirectoryPaths.Value;

    /// <summary>Every managed assembly in <see cref="Directories"/>, sorted by name. Scanned once, on
    /// first use.</summary>
    public static IReadOnlyList<RuntimeAssembly> All => LazyAll.Value;

    /// <summary>The runtime's file of the assembly named <paramref name="simpleName"/>, or null.</summary>
    /// <exception cref="IOException">The runtime's directories cannot be listed for now.</exception>
    internal static string? Find(string simpleName) => LazyFilesByName.Value.GetValueOrDefault(simpleName);

    /// <summary>The newest <c>Microsoft.NETCore.App</c> directory under the first of
    /// <paramref name="dotnetRoots"/> that has one, preferring version <paramref name="major"/>.</summary>
    internal static string? FindInstalledRuntime(IEnumerable<string> dotnetRoots, int major)
    {
        foreach (var root in dotnetRoots)
        {
            var frameworks = Path.Combine(root, "shared", "Microsoft.NETCore.App");
            if (!Directory.Exists(frameworks)) continue;
            var versions = Directory.EnumerateDirectories(frameworks)
                .Select(dir => (Dir: dir, Version: ParseVersion(Path.GetFileName(dir))))
                .Where(d => d.Version is not null && File.Exists(Path.Combine(d.Dir, "System.Private.CoreLib.dll")))
                .ToList();
            var best = versions.OrderByDescending(d => d.Version!.Major == major).ThenByDescending(d => d.Version).Select(d => d.Dir).FirstOrDefault();
            if (best is not null) return best;
        }
        return null;
    }

    private static string RuntimeDirectory()
    {
        var running = Path.TrimEndingDirectorySeparator(RuntimeEnvironment.GetRuntimeDirectory());
        if (File.Exists(Path.Combine(running, "System.Private.CoreLib.dll"))) return running;
        return FindInstalledRuntime(DotNetRoots(), Environment.Version.Major) ?? running;
    }

    internal static IEnumerable<string> DotNetRoots()
    {
        if (Environment.GetEnvironmentVariable("DOTNET_ROOT") is { Length: > 0 } root)
            yield return root;
        var exe = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = new FileInfo(Path.Combine(dir, exe));
            if (!candidate.Exists) continue;
            var target = candidate.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? candidate.FullName;
            if (Path.GetDirectoryName(target) is { } resolved)
                yield return resolved;
        }
        if (OperatingSystem.IsWindows())
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
        else if (OperatingSystem.IsMacOS())
            yield return "/usr/local/share/dotnet";
        else
        {
            yield return "/usr/share/dotnet";
            yield return "/usr/lib/dotnet";
        }
    }

    private static ImmutableArray<(string Framework, string Path)> FindDirectories()
    {
        var runtime = RuntimeDirectory();
        var frameworkDir = Path.GetDirectoryName(runtime);
        var sharedDir = frameworkDir is null ? null : Path.GetDirectoryName(frameworkDir);
        var result = ImmutableArray.CreateBuilder<(string, string)>();
        result.Add((frameworkDir is null ? "Microsoft.NETCore.App" : Path.GetFileName(frameworkDir), runtime));
        if (sharedDir is null || frameworkDir is null || !Directory.Exists(sharedDir)) return result.ToImmutable();

        var version = ParseVersion(Path.GetFileName(runtime));
        foreach (var sibling in Directory.EnumerateDirectories(sharedDir).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(sibling, frameworkDir, StringComparison.OrdinalIgnoreCase)) continue;
            var best = Directory.EnumerateDirectories(sibling)
                .Select(dir => (Dir: dir, Version: ParseVersion(Path.GetFileName(dir))))
                .Where(d => d.Version is not null && version is not null && d.Version.Major == version.Major && d.Version.Minor == version.Minor)
                .MaxBy(d => d.Version);
            if (best.Dir is not null)
                result.Add((Path.GetFileName(sibling), best.Dir));
        }
        return result.ToImmutable();
    }

    private static Version? ParseVersion(string name)
    {
        var dash = name.IndexOf('-');
        return Version.TryParse(dash < 0 ? name : name[..dash], out var version) ? version : null;
    }

    private static IReadOnlyDictionary<string, string> IndexFiles()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, dir) in LazyDirectories.Value)
        foreach (var file in Directory.EnumerateFiles(dir, "*.dll"))
            result.TryAdd(Path.GetFileNameWithoutExtension(file), file);
        return result;
    }

    private static ImmutableArray<RuntimeAssembly> ScanAssemblies()
    {
        var result = new List<RuntimeAssembly>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (framework, dir) in LazyDirectories.Value)
        foreach (var file in Directory.EnumerateFiles(dir, "*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (seen.Contains(name) || !IsManagedAssembly(file)) continue;
            seen.Add(name);
            result.Add(new RuntimeAssembly(name, framework, file));
        }
        result.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
        return result.ToImmutableArray();
    }

    private static bool IsManagedAssembly(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new PEReader(stream);
            return reader.HasMetadata && reader.GetMetadataReader().IsAssembly;
        }
        catch (Exception e) when (e is IOException or BadImageFormatException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}

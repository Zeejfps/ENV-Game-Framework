// Regenerates the GUI canvas shaders: every *.slang here, compiled by the slangc pinned in SLANG_VERSION, to GLSL
// for ZGF.Gui.OpenGL and Metal for ZGF.Gui.Metal. Those two Assets/Shaders folders hold only what this writes.
//
//   dotnet run ZGF.Gui.Shaders/regenerate.cs                 writes into the two backends
//   dotnet run ZGF.Gui.Shaders/regenerate.cs -- --out <dir>  writes the same relative paths under <dir>
//
// The compiler is downloaded once per version and platform into a per-user cache, its archive checked against
// the SHA256 in SLANG_VERSION.

using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

var shaderDir = ShaderDirectory();
var repoRoot = Paths.Parent(shaderDir);

string outRoot;
switch (args)
{
    case []:
        outRoot = repoRoot;
        break;
    case ["--out", var dir]:
        outRoot = Path.GetFullPath(dir);
        break;
    default:
        Console.Error.WriteLine("usage: dotnet run ZGF.Gui.Shaders/regenerate.cs [-- --out <dir>]");
        return 2;
}

try
{
    var pin = SlangPin.Parse(File.ReadAllLines(Path.Combine(shaderDir, "SLANG_VERSION")));
    var slangc = await SlangCompiler.Install(pin);
    Generator.Run(slangc, shaderDir, outRoot);
    return 0;
}
catch (ShaderBuildException e)
{
    Console.Error.WriteLine($"regenerate: {e.Message}");
    return 1;
}

static string ShaderDirectory([CallerFilePath] string source = "") => Paths.Parent(source);

static class Paths
{
    public static string Parent(string path) =>
        Path.GetDirectoryName(path) ?? throw new ShaderBuildException($"{path} has no parent directory.");
}

sealed class ShaderBuildException(string message) : Exception(message);

sealed record SlangArchive(string Platform, string FileName, string Sha256);

sealed record SlangPin(string Version, IReadOnlyList<SlangArchive> Archives)
{
    public static SlangPin Parse(IEnumerable<string> lines)
    {
        string? version = null;
        var archives = new List<SlangArchive>();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            switch (fields)
            {
                case ["version", var v] when version == null:
                    version = v;
                    break;
                case [var platform, var file, var sha] when version != null:
                    if (!file.Contains($"-{version}-", StringComparison.Ordinal))
                        throw new ShaderBuildException($"SLANG_VERSION: {file} is not an archive of version {version}.");
                    if (sha.Length != 64 || !sha.All(Uri.IsHexDigit))
                        throw new ShaderBuildException($"SLANG_VERSION: {sha} is not a SHA256.");
                    archives.Add(new SlangArchive(platform, file, sha.ToLowerInvariant()));
                    break;
                default:
                    throw new ShaderBuildException($"SLANG_VERSION: cannot read \"{line}\"; expected \"version <v>\" first, then \"<platform> <archive> <sha256>\" lines.");
            }
        }
        if (version == null) throw new ShaderBuildException("SLANG_VERSION names no version.");
        return new SlangPin(version, archives);
    }
}

static class SlangCompiler
{
    public static async Task<string> Install(SlangPin pin)
    {
        var platform = HostPlatform();
        var archive = pin.Archives.FirstOrDefault(a => a.Platform == platform)
                      ?? throw new ShaderBuildException($"SLANG_VERSION has no archive for {platform}.");

        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZGF", "slang", pin.Version);
        var installed = Path.Combine(root, platform);
        var slangc = Path.Combine(installed, "bin", OperatingSystem.IsWindows() ? "slangc.exe" : "slangc");
        if (File.Exists(slangc)) return slangc;

        Directory.CreateDirectory(root);
        var staging = Path.Combine(root, $".staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        try
        {
            var download = Path.Combine(staging, archive.FileName);
            var url = $"https://github.com/shader-slang/slang/releases/download/v{pin.Version}/{archive.FileName}";
            Console.Error.WriteLine($"regenerate: downloading {url}");
            using (var http = new HttpClient())
            await using (var source = await http.GetStreamAsync(url))
            await using (var file = File.Create(download))
                await source.CopyToAsync(file);

            string actual;
            await using (var file = File.OpenRead(download))
                actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(file));
            if (actual != archive.Sha256)
                throw new ShaderBuildException($"{archive.FileName} has SHA256 {actual}; SLANG_VERSION pins {archive.Sha256}.");

            var extracted = Path.Combine(staging, "extracted");
            Directory.CreateDirectory(extracted);
            if (archive.FileName.EndsWith(".zip", StringComparison.Ordinal))
                ZipFile.ExtractToDirectory(download, extracted);
            else
            {
                await using var file = File.OpenRead(download);
                await using var gzip = new GZipStream(file, CompressionMode.Decompress);
                await TarFile.ExtractToDirectoryAsync(gzip, extracted, overwriteFiles: false);
            }

            var distribution = Directory.EnumerateFiles(extracted, Path.GetFileName(slangc), SearchOption.AllDirectories)
                                   .Select(p => Paths.Parent(Paths.Parent(p)))
                                   .FirstOrDefault()
                               ?? throw new ShaderBuildException($"{archive.FileName} holds no bin/{Path.GetFileName(slangc)}.");
            try
            {
                Directory.Move(distribution, installed);
            }
            catch (IOException) when (File.Exists(slangc))
            {
                // Another run installed the same version first.
            }
        }
        finally
        {
            Directory.Delete(staging, recursive: true);
        }
        return slangc;
    }

    private static string HostPlatform()
    {
        var os = OperatingSystem.IsWindows() ? "windows"
            : OperatingSystem.IsLinux() ? "linux"
            : OperatingSystem.IsMacOS() ? "macos"
            : throw new ShaderBuildException($"No slangc for {RuntimeInformation.OSDescription}.");
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x86_64",
            Architecture.Arm64 => "arm64",
            var other => throw new ShaderBuildException($"No slangc for {other}."),
        };
        return $"{os}-{arch}";
    }
}

static class Generator
{
    private static readonly string[] Common = ["-matrix-layout-column-major", "-warnings-as-errors", "all"];
    private static readonly string GlDir = Path.Combine("ZGF.Gui.OpenGL", "Assets", "Shaders");
    private static readonly string MetalDir = Path.Combine("ZGF.Gui.Metal", "Assets", "Shaders");

    public static void Run(string slangc, string shaderDir, string outRoot)
    {
        var modules = Directory.EnumerateFiles(shaderDir, "*.slang")
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToList();
        if (modules.Count == 0) throw new ShaderBuildException($"No .slang files in {shaderDir}.");

        var written = new HashSet<string>(StringComparer.Ordinal);
        var scratch = Directory.CreateTempSubdirectory("zgf-slangc-");
        try
        {
            foreach (var module in modules)
            {
                var name = Path.GetFileNameWithoutExtension(module);
                foreach (var stage in new[] { ("vertex", "vert"), ("fragment", "frag") })
                {
                    var path = Path.Combine(GlDir, $"{name}.{stage.Item2}.glsl");
                    Emit(slangc, shaderDir, scratch.FullName, module, outRoot, path,
                        ["-target", "glsl", "-profile", "glsl_450", "-entry", $"{stage.Item1}Main", "-stage", stage.Item1]);
                    written.Add(path);
                }
                var metal = Path.Combine(MetalDir, $"{name}.gen.metal");
                Emit(slangc, shaderDir, scratch.FullName, module, outRoot, metal,
                    ["-target", "metal", "-entry", "vertexMain", "-stage", "vertex", "-entry", "fragmentMain", "-stage", "fragment"]);
                written.Add(metal);
            }
        }
        finally
        {
            scratch.Delete(recursive: true);
        }

        foreach (var dir in new[] { GlDir, MetalDir })
        foreach (var file in Directory.EnumerateFiles(Path.Combine(outRoot, dir)))
        {
            var relative = Path.Combine(dir, Path.GetFileName(file));
            if (written.Contains(relative)) continue;
            File.Delete(file);
            Console.Error.WriteLine($"regenerate: deleted {relative}, which no .slang generates");
        }
    }

    private static void Emit(string slangc, string shaderDir, string scratch, string module, string outRoot, string relative, string[] target)
    {
        var temp = Path.Combine(scratch, Path.GetFileName(relative));
        var start = new ProcessStartInfo(slangc)
        {
            WorkingDirectory = shaderDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(module);
        foreach (var arg in target.Concat(Common)) start.ArgumentList.Add(arg);
        start.ArgumentList.Add("-o");
        start.ArgumentList.Add(temp);

        using var process = Process.Start(start) ?? throw new ShaderBuildException($"Could not start {slangc}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var diagnostics = (stdout.Result + stderr.Result).Trim();
        if (process.ExitCode != 0 || !File.Exists(temp))
            throw new ShaderBuildException($"slangc failed on {module} for {relative}:\n{diagnostics}");
        if (diagnostics.Length > 0)
            throw new ShaderBuildException($"slangc reported on {module} for {relative}:\n{diagnostics}");

        var body = File.ReadAllText(temp).ReplaceLineEndings("\n");
        var header = $"// Generated from ZGF.Gui.Shaders/{module} by ZGF.Gui.Shaders/regenerate.cs. Edit the .slang and regenerate.\n";
        var destination = Path.Combine(outRoot, relative);
        Directory.CreateDirectory(Paths.Parent(destination));
        File.WriteAllText(destination, header + body);
    }
}

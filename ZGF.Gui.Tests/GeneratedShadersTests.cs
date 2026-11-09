using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ZGF.Gui.Tests;

/// <summary>
/// The GLSL and Metal the backends load are exactly what <c>ZGF.Gui.Shaders/regenerate.cs</c> makes from the
/// <c>.slang</c> sources with the pinned compiler, so an edit to a generated file, or a source never regenerated,
/// fails here.
/// </summary>
public sealed class GeneratedShadersTests
{
    private static readonly string[] GeneratedDirectories =
    [
        Path.Combine("ZGF.Gui.OpenGL", "Assets", "Shaders"),
        Path.Combine("ZGF.Gui.Metal", "Assets", "Shaders"),
    ];

    [Fact]
    public void The_checked_in_shaders_are_what_the_slang_sources_generate()
    {
        var repo = RepoRoot();
        var regenerated = Directory.CreateTempSubdirectory("zgf-shaders-");
        try
        {
            Regenerate(repo, regenerated.FullName);

            var problems = new List<string>();
            foreach (var dir in GeneratedDirectories)
            {
                var expected = Files(Path.Combine(regenerated.FullName, dir));
                var actual = Files(Path.Combine(repo, dir));
                problems.AddRange(expected.Keys.Except(actual.Keys).Select(f => $"{Path.Combine(dir, f)} is missing"));
                problems.AddRange(actual.Keys.Except(expected.Keys).Select(f => $"{Path.Combine(dir, f)} is not generated from any .slang"));
                problems.AddRange(expected.Keys.Intersect(actual.Keys)
                    .Where(f => expected[f] != actual[f])
                    .Select(f => $"{Path.Combine(dir, f)} differs from what its .slang generates"));
            }

            Assert.True(problems.Count == 0,
                string.Join("\n", problems) + "\nEdit only the .slang sources, then run: dotnet run ZGF.Gui.Shaders/regenerate.cs");
        }
        finally
        {
            regenerated.Delete(recursive: true);
        }
    }

    private static Dictionary<string, string> Files(string dir) =>
        Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir).ToDictionary(f => Path.GetFileName(f), f => File.ReadAllText(f).ReplaceLineEndings("\n"))
            : [];

    private static void Regenerate(string repo, string outDir)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in new[] { "run", Path.Combine("ZGF.Gui.Shaders", "regenerate.cs"), "--", "--out", outDir })
            start.ArgumentList.Add(arg);

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {start.FileName}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        Assert.True(process.WaitForExit(TimeSpan.FromMinutes(10)), "regenerate.cs did not finish within 10 minutes.");
        Assert.True(process.ExitCode == 0, $"regenerate.cs exited with {process.ExitCode}:\n{stdout.Result}\n{stderr.Result}");
    }

    private static string RepoRoot([CallerFilePath] string source = "") =>
        Path.GetDirectoryName(Path.GetDirectoryName(source)) ?? throw new InvalidOperationException($"{source} is not inside the repository.");
}

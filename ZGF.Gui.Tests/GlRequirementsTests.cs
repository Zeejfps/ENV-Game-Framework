using System.Reflection;
using System.Text.RegularExpressions;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Tests;

public sealed partial class GlRequirementsTests
{
    [Fact]
    public void Every_GL_call_the_canvas_makes_is_required_or_explicitly_optional()
    {
        var root = RepositoryRoot();
        var sources = Directory.GetFiles(Path.Combine(root, "ZGF.Gui.OpenGL"), "*.cs", SearchOption.TopDirectoryOnly)
            .Append(Path.Combine(root, "OpenGL.NET", "OpenGlUtils.cs"));
        var called = sources
            .SelectMany(path => GlCall().Matches(File.ReadAllText(path)).Select(m => m.Groups[1].Value))
            .Where(IsGlEntryPoint)
            .ToHashSet();

        Assert.Contains("glDrawArraysInstanced", called);
        var unlisted = called.Where(name => !GlRequirements.Required.Contains(name) && !GlRequirements.Optional.Contains(name));
        Assert.Empty(unlisted.Order());
    }

    [Fact]
    public void The_lists_name_real_entry_points_and_do_not_overlap()
    {
        Assert.All(GlRequirements.Required.Concat(GlRequirements.Optional), name => Assert.True(IsGlEntryPoint(name), name));
        Assert.Empty(GlRequirements.Required.Intersect(GlRequirements.Optional));
    }

    // GL46 keeps each imported entry point in a private s_<name> field; a source token without one is a
    // helper's name, not a GL function.
    private static bool IsGlEntryPoint(string name) =>
        typeof(GL46).GetField("s_" + name, BindingFlags.NonPublic | BindingFlags.Static) != null;

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ENV Game Framework.sln")))
            directory = directory.Parent;
        if (directory == null)
            throw new InvalidOperationException($"No repository root above {AppContext.BaseDirectory}.");
        return directory.FullName;
    }

    [GeneratedRegex(@"\b(gl[A-Z][A-Za-z0-9_]*)\s*\(")]
    private static partial Regex GlCall();
}

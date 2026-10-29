using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

internal static class TestPaths
{
    /// <summary>The path <paramref name="path"/>, which the test names and must be absolute.</summary>
    public static AssemblyPath PathOf(string path) => AssemblyPath.Of(path);

    /// <summary>A path in the temp folder for a file the test never reads, such as one in a saved list.</summary>
    public static AssemblyPath FakePath(string name) => AssemblyPath.Of(Path.Combine(Path.GetTempPath(), name));

    /// <summary>Opens the file at <paramref name="path"/>, which the test names and must be absolute.</summary>
    public static LoadedAssembly Open(this AssemblyList list, string path) => list.Open(AssemblyPath.Of(path));
}

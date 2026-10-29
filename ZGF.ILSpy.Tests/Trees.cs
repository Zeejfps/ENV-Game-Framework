using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

internal static class Trees
{
    public const string FixturesNamespace = "ZGF.ILSpy.Tests.Fixtures";

    private static readonly Lazy<LoadedModule> s_testAssembly = new(() => new AssemblyList().OpenModule(TestAssemblyPath));
    private static readonly Lazy<DecompiledDocument> s_calculatorCSharp = new(() => TestAssembly.Calculator().Decompile(DecompilationLanguage.CSharp));
    private static readonly Lazy<DecompiledDocument> s_calculatorIL = new(() => TestAssembly.Calculator().Decompile(DecompilationLanguage.IL));

    public static string TestAssemblyPath => typeof(Trees).Assembly.Location;

    public static string CoreLibPath => typeof(object).Assembly.Location;

    /// <summary>A small WPF app, built from <c>Fixtures/SampleApp/Source</c>: a window and an
    /// application compiled to BAML, a property and event with users, and a security attribute.</summary>
    public static string SampleAppPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleApp", "SampleApp.dll");

    /// <summary>A small Avalonia 12 app, built from <c>Fixtures/AvaloniaSample/Source</c>, whose
    /// assets sit in its <c>!AvaloniaResources</c>.</summary>
    public static string AvaloniaSamplePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "AvaloniaSample", "AvaloniaSample.dll");

    /// <summary>A one-type library, built from <c>Fixtures/EmbeddedPdb/Source</c>, whose portable PDB
    /// is embedded in it.</summary>
    public static string EmbeddedPdbPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "EmbeddedPdb", "EmbeddedPdb.dll");

    /// <summary>Version 1.0 of a small library, built from <c>Fixtures/ApiDiff/Older/Source</c>, to compare
    /// with <see cref="ApiDiffNewerPath"/>.</summary>
    public static string ApiDiffOlderPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "ApiDiff", "Older", "ApiDiffSample.dll");

    /// <summary>Version 2.0 of the library at <see cref="ApiDiffOlderPath"/>, built from
    /// <c>Fixtures/ApiDiff/Newer/Source</c>: members added, removed and declared differently, a namespace
    /// added and one removed.</summary>
    public static string ApiDiffNewerPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "ApiDiff", "Newer", "ApiDiffSample.dll");

    /// <summary>A library built from <c>Fixtures/Paths/Before/Source</c> whose nodes share labels: overloads
    /// told apart only by the namespace of a parameter's type, types of one name used from two namespaces,
    /// imports of one name from two types, and an event beside its backing field.</summary>
    public static string PathsBeforePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Paths", "Before", "PathsSample.dll");

    /// <summary>The library at <see cref="PathsBeforePath"/> rebuilt from <c>Fixtures/Paths/After/Source</c>:
    /// a generic parameter renamed, a class turned record, a field turned constant, and types added.</summary>
    public static string PathsAfterPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Paths", "After", "PathsSample.dll");

    /// <summary><see cref="SampleAppPath"/> converted to WebCIL by the .NET WebAssembly SDK, wrapped in
    /// WebAssembly as Blazor ships it.</summary>
    public static string WebCilSamplePath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "WebCil", "SampleApp.wasm");

    /// <summary>The test assembly, opened once for the tests that only read it. Its list is out of
    /// reach; a test that needs the list, or nodes no other test has built, opens its own with
    /// <see cref="OpenTestAssembly"/>.</summary>
    public static LoadedModule TestAssembly => s_testAssembly.Value;

    public static DecompiledDocument CalculatorDocument(DecompilationLanguage language) => language switch
    {
        DecompilationLanguage.CSharp => s_calculatorCSharp.Value,
        DecompilationLanguage.IL => s_calculatorIL.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(language)),
    };

    public static BrowseNode Fixtures(this LoadedAssembly assembly) => assembly.Root.Child(FixturesNamespace);

    public static BrowseNode Calculator(this LoadedAssembly assembly) => assembly.Fixtures().Child("Calculator<T>");

    public static (AssemblyList List, LoadedModule Assembly) OpenTestAssembly()
    {
        var list = new AssemblyList();
        return (list, list.OpenModule(TestAssemblyPath));
    }

    /// <summary>The assembly or module at <paramref name="path"/>, opened in <paramref name="list"/>; fails
    /// the test when it opens as anything else.</summary>
    public static LoadedModule OpenModule(this AssemblyList list, string path) => Assert.IsType<LoadedModule>(list.Open(path));

    /// <summary>Opens the file at <paramref name="path"/>, which the test names and must be absolute.</summary>
    public static LoadedAssembly Open(this AssemblyList list, string path, CancellationToken cancellationToken = default) =>
        list.Open(AssemblyPath.Of(path), cancellationToken);

    public static LoadedAssembly? TryLoad(this AssemblyList list, string path) => list.TryLoad(AssemblyPath.Of(path));

    public static LoadedAssembly? TryLoad(this AssemblyList list, string path, int generation) => list.TryLoad(AssemblyPath.Of(path), generation);
}

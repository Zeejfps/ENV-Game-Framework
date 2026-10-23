using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

internal static class Trees
{
    public const string FixturesNamespace = "ZGF.ILSpy.Tests.Fixtures";

    private static readonly Lazy<LoadedAssembly> s_testAssembly = new(() => new AssemblyList().Open(TestAssemblyPath));
    private static readonly Lazy<DecompiledDocument> s_calculatorCSharp = new(() => TestAssembly.Calculator().Decompile(DecompilationLanguage.CSharp));
    private static readonly Lazy<DecompiledDocument> s_calculatorIL = new(() => TestAssembly.Calculator().Decompile(DecompilationLanguage.IL));

    public static string TestAssemblyPath => typeof(Trees).Assembly.Location;

    public static string CoreLibPath => typeof(object).Assembly.Location;

    /// <summary>A small WPF app, built from <c>Fixtures/SampleApp/Source</c>: a window and an
    /// application compiled to BAML, a property and event with users, and a security attribute.</summary>
    public static string SampleAppPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleApp", "SampleApp.dll");

    /// <summary>The test assembly, opened once for the tests that only read it. Its list is out of
    /// reach; a test that needs the list, or nodes no other test has built, opens its own with
    /// <see cref="OpenTestAssembly"/>.</summary>
    public static LoadedAssembly TestAssembly => s_testAssembly.Value;

    public static DecompiledDocument CalculatorDocument(DecompilationLanguage language) => language switch
    {
        DecompilationLanguage.CSharp => s_calculatorCSharp.Value,
        DecompilationLanguage.IL => s_calculatorIL.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(language)),
    };

    public static BrowseNode Fixtures(this LoadedAssembly assembly) => assembly.Root.Child(FixturesNamespace);

    public static BrowseNode Calculator(this LoadedAssembly assembly) => assembly.Fixtures().Child("Calculator<T>");

    public static (AssemblyList List, LoadedAssembly Assembly) OpenTestAssembly()
    {
        var list = new AssemblyList();
        return (list, list.Open(TestAssemblyPath));
    }
}

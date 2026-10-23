using ICSharpCode.BamlDecompiler;

namespace ZGF.ILSpy.Core.Resources;

/// <summary>WPF's compiled XAML, which ILSpy's BAML decompiler turns back into XAML.</summary>
internal static class Baml
{
    private const string Extension = ".baml";
    public const string XamlExtension = ".xaml";

    public static bool IsBamlName(string name) => name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary><paramref name="name"/> with the extension of the XAML it decompiles to.</summary>
    public static string XamlName(string name) => Path.ChangeExtension(name, XamlExtension);

    /// <summary>The BAML in <paramref name="data"/> as XAML, its types resolved against
    /// <paramref name="typeSystem"/>.</summary>
    public static BamlDecompilationResult Decompile(BamlDecompilerTypeSystem typeSystem, Stream data, CancellationToken cancellationToken)
    {
        var settings = new BamlDecompilerSettings { ThrowOnAssemblyResolveErrors = false };
        var decompiler = new XamlDecompiler(typeSystem, settings) { CancellationToken = cancellationToken };
        return decompiler.Decompile(data);
    }

    public static string DecompileToText(LoadedAssembly assembly, byte[] data, CancellationToken cancellationToken)
    {
        var typeSystem = new BamlDecompilerTypeSystem(assembly.File, assembly.Resolver);
        using var stream = new MemoryStream(data);
        var result = Decompile(typeSystem, stream, cancellationToken);
        return result.Xaml.ToString();
    }
}

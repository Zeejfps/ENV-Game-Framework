// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Net;
using System.Text.Json;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Diagrams;

/// <summary>ILSpy's HTML class diagrammer for an assembly: a page that draws mermaid class diagrams
/// (https://mermaid.js.org/syntax/classDiagram.html) of the types picked in it, from the assembly's
/// types, their members, relations, inheritance and XML documentation baked into it as JSON. The
/// page loads mermaid from a CDN, so it draws only online.</summary>
public static class ClassDiagrams
{
    /// <summary>The file of the page, which opens the diagrammer.</summary>
    public const string PageFileName = "index.html";

    private const string TemplateName = "template.html";
    private const string ResourcePrefix = "diagram/";
    private const string RepoUrl = "https://github.com/icsharpcode/ILSpy";
    private static readonly string[] Assets = ["styles.css", "script.js", "ILSpy.ico"];

    /// <summary>Writes the diagrammer of <paramref name="assembly"/> into <paramref name="folder"/>,
    /// creating it, and returns the path of its page.</summary>
    public static string Write(LoadedAssembly assembly, string folder, IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var model = BuildModel(assembly, progress, cancellationToken);
        Directory.CreateDirectory(folder);
        var page = Path.Combine(folder, PageFileName);
        File.WriteAllText(page, WritePage(assembly, model));
        foreach (var asset in Assets)
            CopyAsset(asset, folder);
        return page;
    }

    /// <summary>The model of every type of <paramref name="assembly"/> but compiler-generated ones and
    /// those nested in them, by namespace and then by full name.</summary>
    internal static DiagramModel BuildModel(LoadedAssembly assembly, IProgress<ExportProgress>? progress, CancellationToken cancellationToken)
    {
        var picked = assembly.Module.TypeDefinitions
            .Where(static t => !t.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
            .ToArray();
        var names = new DiagramNames(picked);
        var types = new DiagramTypes(names, DiagramDocs.Of(assembly.File));
        var namespaces = picked
            .GroupBy(static t => t.Namespace)
            .OrderBy(static g => g.Key, StringComparer.InvariantCulture);
        var built = new BuiltTypes(assembly.Name, picked.Length, progress);
        var byNamespace = new Dictionary<string, Dictionary<string, DiagramType>>();
        foreach (var group in namespaces)
            byNamespace[group.Key] = BuildNamespace(group, names, types, built, cancellationToken);
        return new DiagramModel { OutsideReferences = names.OutsideReferences, TypesByNamespace = byNamespace };
    }

    private static Dictionary<string, DiagramType> BuildNamespace(IEnumerable<ITypeDefinition> group, DiagramNames names, DiagramTypes types,
        BuiltTypes built, CancellationToken cancellationToken)
    {
        var ordered = group.OrderBy(static t => t.FullName, StringComparer.InvariantCulture);
        var byId = new Dictionary<string, DiagramType>();
        foreach (var type in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byId[names.IdOf(type)] = types.Build(type);
            built.Count();
        }
        return byId;
    }

    private static string WritePage(LoadedAssembly assembly, DiagramModel model)
    {
        var json = JsonSerializer.Serialize(model, DiagramJson.Default.DiagramModel);
        var module = assembly.Module;
        return ReadAsset(TemplateName)
            .Replace("{{SourceAssemblyName}}", WebUtility.HtmlEncode(module.AssemblyName))
            .Replace("{{SourceAssemblyVersion}}", WebUtility.HtmlEncode(module.AssemblyVersion.ToString()))
            .Replace("{{BuilderVersion}}", DecompilerVersionInfo.FullVersionWithCommitHash)
            .Replace("{{RepoUrl}}", RepoUrl)
            .Replace("{{Model}}", json);
    }

    private static string ReadAsset(string name)
    {
        using var reader = new StreamReader(OpenAsset(name));
        return reader.ReadToEnd();
    }

    private static void CopyAsset(string name, string folder)
    {
        using var asset = OpenAsset(name);
        using var file = File.Create(Path.Combine(folder, name));
        asset.CopyTo(file);
    }

    private static Stream OpenAsset(string name)
    {
        var resource = ResourcePrefix + name;
        var stream = typeof(ClassDiagrams).Assembly.GetManifestResourceStream(resource);
        if (stream == null)
            throw new FileNotFoundException("The diagrammer's resource is missing.", resource);
        return stream;
    }

    /// <summary>How many of an assembly's types are built, reported as the share of them done.</summary>
    private sealed class BuiltTypes(string assemblyName, int total, IProgress<ExportProgress>? progress)
    {
        private int _done;

        public void Count()
        {
            _done++;
            progress?.Report(new ExportProgress(assemblyName, 0, 1, (float)_done / total));
        }
    }
}

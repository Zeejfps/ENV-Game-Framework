using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

public sealed partial class BrowseNode
{
    /// <summary>The root of <paramref name="module"/>'s tree: at the top, or for one read out of a package
    /// under the folder holding its file.</summary>
    internal static BrowseNode ModuleRoot(LoadedModule module, BrowseNode? parent) => new(parent, new NodeSubject.Module(module));

    /// <summary>The root of a listed file that could not be read, without children, showing why.</summary>
    internal static BrowseNode FailedRoot(FailedAssembly failed) => new(null, new NodeSubject.FailedAssembly(failed));

    /// <summary>The metadata, the debug metadata when there is some, the references and the resources,
    /// then every namespace a type is in or a name nests in, flat and sorted by name, each knowing the
    /// one it nests in.</summary>
    private BrowseNode[] ListModule(LoadedModule module)
    {
        var file = module.File;
        var result = new List<BrowseNode> { MetadataFolder(module, this, file, "Metadata") };
        var symbols = module.DebugSymbols;
        if (symbols != null)
            result.Add(MetadataFolder(module, this, symbols.File, symbols.Title));
        result.Add(new BrowseNode(this, new NodeSubject.References(module)));
        if (!file.Resources.IsEmpty)
            result.Add(new BrowseNode(this, new NodeSubject.Resources(module)));

        var byNamespace = new Dictionary<string, List<ITypeDefinition>>(StringComparer.Ordinal);
        foreach (var type in module.MainModule.TopLevelTypeDefinitions)
        {
            if (Members.IsMissing(type)) continue;
            if (!byNamespace.TryGetValue(type.Namespace, out var types))
                byNamespace.Add(type.Namespace, types = new List<ITypeDefinition>());
            types.Add(type);
        }
        var forwardedByNamespace = ForwardedTypes.ListByNamespace(file.Metadata);

        var namespaces = new Dictionary<string, BrowseNode>(StringComparer.Ordinal);
        var names = byNamespace.Keys
            .Union(forwardedByNamespace.Keys)
            .SelectMany(NamespaceNames.AndEnclosing)
            .Distinct()
            .Order(NaturalStringComparer.Instance);
        foreach (var name in names)
        {
            var types = byNamespace.GetValueOrDefault(name, []);
            var forwarded = forwardedByNamespace.GetValueOrDefault(name, [])
                .Select(f => new ForwardedTypeInfo(module, f))
                .ToList();
            var info = new NamespaceInfo(module, name, types, forwarded);
            var node = new BrowseNode(this, new NodeSubject.Namespace(info)) { EnclosingNamespace = FindEnclosing(namespaces, name) };
            namespaces.Add(name, node);
            result.Add(node);
        }
        return result.ToArray();
    }

    /// <summary>The namespace <paramref name="name"/> extends by a segment, made already, as names
    /// sort after the names they extend; null for an outermost name.</summary>
    private static BrowseNode? FindEnclosing(Dictionary<string, BrowseNode> namespaces, string name)
    {
        var enclosing = NamespaceNames.EnclosingName(name);
        if (enclosing == null)
            return null;
        return namespaces[enclosing];
    }

    /// <summary>The types the namespace defines and those it forwards, sorted together by name, then by
    /// how many type parameters they have.</summary>
    private BrowseNode[] ListNamespace(NamespaceInfo ns)
    {
        var children = new List<(string Name, int Arity, BrowseNode Node)>(ns.Types.Count + ns.Forwarded.Count);
        foreach (var type in ns.Types)
        {
            var node = ForType(ns.Module, this, type);
            if (node != null)
                children.Add((type.Name, type.TypeParameterCount, node));
        }
        foreach (var forwarded in ns.Forwarded)
            children.Add((forwarded.SortName, forwarded.Arity, new BrowseNode(this, new NodeSubject.ForwardedType(forwarded))));
        return children
            .OrderBy(static c => c.Name, NaturalStringComparer.Instance)
            .ThenBy(static c => c.Arity)
            .Select(static c => c.Node)
            .ToArray();
    }

    /// <summary>The module's name, version and the framework it was built for. The framework picked to
    /// resolve its references against is left out, so that the label, by which paths find the node, stays
    /// the same when another is picked.</summary>
    private static string LabelOf(LoadedModule module)
    {
        var details = new[] { module.Version?.ToString(), BuiltForName(module) }.OfType<string>().ToArray();
        return details.Length == 0 ? module.Name : $"{module.Name} ({string.Join(", ", details)})";
    }

    private static string? BuiltForName(LoadedModule module)
    {
        if (module.DetectedTargetFramework == null)
            return null;
        return FrameworkIds.ReadableName(module.DetectedTargetFramework);
    }
}

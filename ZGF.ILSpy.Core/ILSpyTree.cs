using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Output;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>What the real ILSpy records about a bookmark besides its line: the assembly it is in,
/// the metadata token of the entity it names or sits in, that entity's full name, the name of the
/// node whose document holds it, and the path ILSpy's tree finds that node by.</summary>
public readonly record struct ILSpyBookmarkInfo(
    string AssemblyFullName, string ModuleName, uint Token, string MemberName, string? LocationNodeName, IReadOnlyList<string>? TreePath);

/// <summary>
/// How the real ILSpy names the nodes of its assembly tree, so bookmark files can be exchanged with
/// it: the assembly by its file, a namespace by its name, a type by its reflection name, and a
/// member by its kind and IL signature. Each name is what ILSpy's tree node returns from ToString.
/// </summary>
public static class ILSpyTree
{
    private static readonly ILAmbience Signatures = new()
    {
        ConversionFlags = ConversionFlags.ShowTypeParameterList
            | ConversionFlags.PlaceReturnTypeAfterParameterList
            | ConversionFlags.ShowReturnType
            | ConversionFlags.ShowParameterList
            | ConversionFlags.ShowParameterModifiers,
    };

    /// <summary>The name ILSpy's tree gives the node standing for <paramref name="node"/>, or null
    /// for a node it has no named counterpart of.</summary>
    public static string? NameOf(BrowseNode node) => NameOf(node.Subject);

    private static string? NameOf(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => type.Definition.Symbol.ReflectionName,
        NodeSubject.MemberDefinition member => member.Definition.Symbol switch
        {
            IMethod method => "Method " + Signatures.ConvertSymbol(method),
            IProperty property => "Property " + Signatures.ConvertSymbol(property),
            IField field => "Field " + field.Name,
            IEvent @event => "Event " + @event.Name,
            _ => null,
        },
        NodeSubject.References or NodeSubject.AssemblyReference or NodeSubject.ReferencedTypes or NodeSubject.TypeReference
            or NodeSubject.MemberReference or NodeSubject.ModuleReference or NodeSubject.ImportedMethod or NodeSubject.ForwardedType
            or NodeSubject.BaseTypes or NodeSubject.BaseType or NodeSubject.DerivedTypes or NodeSubject.DerivedType or NodeSubject.Resources
            or NodeSubject.ResourceContainer or NodeSubject.ResourceEntry or NodeSubject.Metadata or NodeSubject.MetadataTables or NodeSubject.MetadataTable
            or NodeSubject.Package or NodeSubject.PackageFile or NodeSubject.FailedAssembly => null,
        NodeSubject.Module module => module.Value.FilePath.Text,
        NodeSubject.Namespace ns => ns.Value.Name,
        _ => throw new UnreachableException(),
    };

    /// <summary>The names of the nodes down from the assembly to <paramref name="node"/>, as ILSpy's
    /// tree finds a node by; null when one of them has no name there.</summary>
    public static IReadOnlyList<string>? PathOf(BrowseNode node)
    {
        var path = new List<string>();
        for (var n = node; n != null; n = n.Parent)
        {
            var name = NameOf(n);
            if (name == null) return null;
            path.Add(name);
        }
        path.Reverse();
        return path;
    }

    /// <summary>The node a bookmark ILSpy made in the assembly at <paramref name="filePath"/> sits
    /// in, as <see cref="Describe"/> records it: the node <paramref name="treePath"/> names, else the
    /// node of the entity with metadata <paramref name="token"/>, when its full name is
    /// <paramref name="memberName"/>, or its nearest ancestor. Loads the assembly without listing it; null
    /// when it cannot be opened or has neither.</summary>
    public static BrowseNode? Locate(AssemblyList list, BrowseSession session, AssemblyPath filePath, IReadOnlyList<string>? treePath, uint token,
        string memberName)
    {
        if (treePath != null && treePath.Count > 0)
        {
            var found = Find(list, session, treePath);
            if (found != null)
                return found;
        }
        return FindEntity(list, session, filePath, token, memberName);
    }

    private static BrowseNode? Find(AssemblyList list, BrowseSession session, IReadOnlyList<string> path)
    {
        var filePath = AssemblyPath.Parse(path[0]);
        if (filePath == null) return null;
        var assembly = Load(list, filePath);
        if (assembly == null) return null;
        BrowseNode node = session.RootOf(assembly);
        for (var i = 1; i < path.Count; i++)
        {
            BrowseNode? next = null;
            foreach (var child in session.ChildrenOf(node))
            {
                if (NameOf(child) != path[i]) continue;
                next = child;
                break;
            }
            if (next == null) return null;
            node = next;
        }
        return node;
    }

    /// <summary>The node of the entity <paramref name="token"/> names, or its nearest ancestor; null when
    /// the entity is not the one called <paramref name="memberName"/>, as after a rebuild the token can name
    /// another.</summary>
    private static BrowseNode? FindEntity(AssemblyList list, BrowseSession session, AssemblyPath filePath, uint token, string memberName)
    {
        if (token == 0) return null;
        var assembly = Load(list, filePath);
        if (assembly == null) return null;
        var entity = Resolve(assembly, (int)token);
        if (entity == null || entity.FullName != memberName) return null;
        try
        {
            return session.FindOrAncestor(assembly, entity.MetadataToken);
        }
        catch (Exception e) when (e is ArgumentException or BadImageFormatException)
        {
            return null;
        }
    }

    /// <summary>What ILSpy records about a bookmark on a line of <paramref name="place"/>'s document
    /// named by <paramref name="anchor"/>, or by none: then the token is that of the entity the place
    /// stands for, as ILSpy falls back to.</summary>
    public static ILSpyBookmarkInfo Describe(BrowseNode place, CodeAnchor? anchor)
    {
        var assembly = place.Assembly;
        var entity = BrowseNode.EntityOf(place);
        var token = entity is null ? 0u : (uint)MetadataTokens.GetToken(entity.MetadataToken);
        if (anchor != null && anchor.Value.FilePath.Equals(assembly.FilePath))
        {
            token = (uint)anchor.Value.Token;
            if (assembly is LoadedModule module)
                entity = Resolve(module, anchor.Value.Token) ?? entity;
        }
        else if (anchor != null)
        {
            token = (uint)anchor.Value.Token;
        }
        var location = BrowseNode.EntityOf(place)?.FullName ?? NameOf(place);
        return new ILSpyBookmarkInfo(assembly.FullName, assembly.FilePath.FileName, token, entity?.FullName ?? location ?? place.Text, location,
            PathOf(place));
    }

    private static IEntity? Resolve(LoadedModule assembly, int token)
    {
        try
        {
            return assembly.MainModule.ResolveEntity(MetadataTokens.EntityHandle(token));
        }
        catch (Exception e) when (e is ArgumentException or BadImageFormatException or InvalidOperationException)
        {
            return null;
        }
    }

    private static LoadedModule? Load(AssemblyList list, AssemblyPath filePath)
    {
        try
        {
            return list.TryLoadAssembly(filePath);
        }
        catch (Exception e) when (e is IOException or BadImageFormatException or InvalidOperationException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

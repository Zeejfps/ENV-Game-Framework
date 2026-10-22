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
    public static string? NameOf(BrowseNode node) => node switch
    {
        AssemblyNode => node.Assembly.FilePath,
        NamespaceNode ns => ns.Name,
        TypeNode type => type.Type.ReflectionName,
        MemberNode { Member: IMethod method } => "Method " + Signatures.ConvertSymbol(method),
        MemberNode { Member: IProperty property } => "Property " + Signatures.ConvertSymbol(property),
        MemberNode { Member: IField field } => "Field " + field.Name,
        MemberNode { Member: IEvent @event } => "Event " + @event.Name,
        _ => null,
    };

    /// <summary>The names of the nodes down from the assembly to <paramref name="node"/>, as ILSpy's
    /// tree finds a node by; null when one of them has no name there.</summary>
    public static IReadOnlyList<string>? PathOf(BrowseNode node)
    {
        var path = new List<string>();
        for (var n = node; n != null; n = n.Parent)
        {
            if (NameOf(n) is not { } name) return null;
            path.Add(name);
        }
        path.Reverse();
        return path;
    }

    /// <summary>The node a bookmark ILSpy made in the assembly at <paramref name="filePath"/> sits
    /// in, as <see cref="Describe"/> records it: the node <paramref name="treePath"/> names, else the
    /// node of the entity with metadata <paramref name="token"/> or its nearest ancestor. Loads the
    /// assembly without listing it; null when it cannot be opened or has neither.</summary>
    public static NodePath? Locate(AssemblyList list, string filePath, IReadOnlyList<string>? treePath, uint token) =>
        (treePath is { Count: > 0 } ? Find(list, treePath) : null) ?? FindEntity(list, filePath, token);

    private static NodePath? Find(AssemblyList list, IReadOnlyList<string> path)
    {
        if (Load(list, path[0]) is not { } assembly) return null;
        var node = assembly.Root;
        for (var i = 1; i < path.Count; i++)
        {
            BrowseNode? next = null;
            foreach (var child in node.Children)
            {
                if (NameOf(child) != path[i]) continue;
                next = child;
                break;
            }
            if (next == null) return null;
            node = next;
        }
        return node.Path;
    }

    private static NodePath? FindEntity(AssemblyList list, string filePath, uint token)
    {
        if (token == 0 || Load(list, filePath) is not { } assembly) return null;
        try
        {
            return assembly.FindNodeOrAncestor(MetadataTokens.EntityHandle((int)token))?.Path;
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
        var entity = place is EntityNode owner ? owner.Entity : null;
        var token = entity is null ? 0u : (uint)MetadataTokens.GetToken(entity.MetadataToken);
        if (anchor is { } a && string.Equals(a.FilePath, assembly.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            token = (uint)a.Token;
            entity = Resolve(assembly, a.Token) ?? entity;
        }
        else if (anchor is { } other)
        {
            token = (uint)other.Token;
        }
        var location = place is EntityNode { Entity: { } shown } ? shown.FullName : NameOf(place);
        return new ILSpyBookmarkInfo(assembly.FullName, Path.GetFileName(assembly.FilePath), token, entity?.FullName ?? location ?? place.Text, location,
            PathOf(place));
    }

    private static IEntity? Resolve(LoadedAssembly assembly, int token)
    {
        try
        {
            return assembly.Module.ResolveEntity(MetadataTokens.EntityHandle(token));
        }
        catch (Exception e) when (e is ArgumentException or BadImageFormatException or InvalidOperationException)
        {
            return null;
        }
    }

    private static LoadedAssembly? Load(AssemblyList list, string filePath)
    {
        try
        {
            return list.TryLoad(filePath);
        }
        catch (Exception e) when (e is IOException or BadImageFormatException or InvalidOperationException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

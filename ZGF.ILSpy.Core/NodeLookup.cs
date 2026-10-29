using System.Reflection.Metadata;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Core;

/// <summary>The nodes of a <see cref="BrowseSession"/> that paths, references and documentation ids
/// name, found through the files of an <see cref="AssemblyList"/>. May load files and build children:
/// call it off the UI thread.</summary>
public static class NodeLookup
{
    /// <summary>The node <paramref name="path"/> names, opening and listing its assembly if it is not
    /// open and its file still exists, or the root of a file listed as failed. Null if the file cannot
    /// be opened or no longer has that node.</summary>
    public static BrowseNode? Find(AssemblyList list, BrowseSession session, NodePath path)
    {
        try
        {
            var item = list.OpenListed(path.FilePath);
            if (item == null)
                return null;
            return session.Find(item, path);
        }
        catch (Exception e) when (AssemblyList.IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The node <paramref name="path"/> leads to in an assembly the list holds now, or null when
    /// it holds none at that path or the node is no longer there. Opens and lists nothing, so a lookup
    /// that outlives a switch to another list leaves the new list as it is.</summary>
    public static BrowseNode? FindListed(AssemblyList list, BrowseSession session, NodePath path)
    {
        var item = list.FindListedLoaded(path.FilePath);
        if (item == null)
            return null;
        try
        {
            return session.Find(item, path);
        }
        catch (Exception e) when (AssemblyList.IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The node <paramref name="reference"/> points at, opening and listing the assembly that
    /// defines it if it is not open yet and its file can be found. For an entity hidden from the tree,
    /// such as a compiler-generated one, the nearest visible ancestor. Null if the reference is not
    /// navigable or cannot be resolved.</summary>
    public static BrowseNode? Find(AssemblyList list, BrowseSession session, CodeReference reference)
    {
        try
        {
            var (assembly, handle) = list.ResolveListed(reference);
            if (assembly is null)
                return null;
            if (handle.Kind == HandleKind.AssemblyDefinition)
                return session.RootOf(assembly);
            return session.FindOrAncestor(assembly, handle);
        }
        catch (Exception e) when (AssemblyList.IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The node <paramref name="address"/> leads to, as <see cref="Find(AssemblyList, BrowseSession, CodeReference)"/>
    /// finds the node of a reference to it.</summary>
    public static BrowseNode? Find(AssemblyList list, BrowseSession session, NavigationAddress address) =>
        Find(list, session, address.ToReference());

    /// <summary>The metadata table row that defines what <paramref name="reference"/> points at: the row
    /// of a type or member, or an assembly's own row, opening and listing the assembly that defines it
    /// as <see cref="Find(AssemblyList, BrowseSession, CodeReference)"/> does. Null if the reference is
    /// not navigable or cannot be resolved.</summary>
    public static MetadataTarget? FindMetadataRow(AssemblyList list, BrowseSession session, CodeReference reference)
    {
        try
        {
            var (assembly, handle) = list.ResolveListed(reference);
            if (assembly is null)
                return null;
            return session.FindMetadataRow(assembly, handle);
        }
        catch (Exception e) when (AssemblyList.IsUnreadable(e))
        {
            return null;
        }
    }

    /// <summary>The node of the namespace, type or member <paramref name="idString"/> names, written as
    /// XML documentation ids are, such as <c>T:System.String</c>, in the first of <paramref name="assemblies"/>
    /// that defines it; null when none does.</summary>
    public static BrowseNode? FindById(BrowseSession session, string idString, IEnumerable<LoadedModule> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var node = FindById(session, idString, assembly);
            if (node != null)
                return node;
        }
        return null;
    }

    private static BrowseNode? FindById(BrowseSession session, string idString, LoadedModule assembly)
    {
        try
        {
            return session.FindById(assembly, idString);
        }
        catch (Exception e) when (AssemblyList.IsUnreadable(e))
        {
            return null;
        }
    }
}

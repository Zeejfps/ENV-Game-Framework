using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

internal sealed partial class SubjectNode
{
    private const string LocalFunctionMarker = ">g__";

    private static bool HasReferences(LoadedModule module)
    {
        if (!module.File.AssemblyReferences.IsEmpty)
            return true;
        return module.File.Metadata.GetTableRowCount(TableIndex.ModuleRef) > 0;
    }

    /// <summary>The references of <paramref name="module"/>, sorted by name, under <paramref name="parent"/>.
    /// What the module uses of them is read here, off the UI thread, so that listing what each holds
    /// never reads it there.</summary>
    internal static BrowseNode[] ListAssemblyReferences(LoadedModule module, BrowseNode parent)
    {
        module.ReadReferenceUsage();
        return module.File.AssemblyReferences
            .OrderBy(r => r.Name, NaturalStringComparer.Instance)
            .Select(r => (BrowseNode)new SubjectNode(parent, new NodeSubject.AssemblyReference(new AssemblyReferenceInfo(module, r))))
            .ToArray();
    }

    /// <summary>The types the module uses from the reference, then the references of the assembly it
    /// resolves to, found where following it finds it but not listed; only the types when it resolves
    /// to none.</summary>
    private BrowseNode[] ListReferenceContents(AssemblyReferenceInfo reference)
    {
        var children = new List<BrowseNode>();
        if (reference.Module.ReferenceUsage.Uses(reference.Reference.Handle))
            children.Add(new SubjectNode(this, new NodeSubject.ReferencedTypes(reference)));
        var referenced = ResolveReferenced(reference);
        if (referenced != null)
            children.AddRange(ListAssemblyReferences(referenced, this));
        return children.ToArray();
    }

    /// <summary>The assembly <paramref name="reference"/> resolves to, loaded if it is not; null when it
    /// cannot be found or read.</summary>
    private static LoadedModule? ResolveReferenced(AssemblyReferenceInfo reference)
    {
        if (reference.Module.Resolver.ResolveAssembly(reference.Reference) is not LoadedModule referenced)
            return null;
        return referenced;
    }

    /// <summary>The types the module uses from <paramref name="reference"/>, sorted by namespace, then
    /// name, and after them the types it forwards there.</summary>
    private BrowseNode[] ListUsedTypes(AssemblyReferenceInfo reference)
    {
        var module = reference.Module;
        var usage = module.ReferenceUsage;
        var metadata = module.File.Metadata;
        var handle = reference.Reference.Handle;
        var used = usage.TypesIn(handle)
            .Select(h => UsedType(module, h))
            .OrderBy(n => n.Namespace, NaturalStringComparer.Instance)
            .ThenBy(n => n.Text, NaturalStringComparer.Instance)
            .Cast<BrowseNode>();
        var forwarded = usage.ForwardedTo(handle)
            .Select(h => ForwardedTypes.Read(metadata, h))
            .OrderBy(f => f.Namespace, NaturalStringComparer.Instance)
            .ThenBy(f => f.Label, NaturalStringComparer.Instance)
            .Select(f => (BrowseNode)Forwarded(module, this, f));
        return used
            .Concat(forwarded)
            .ToArray();
    }

    /// <summary>The node of a type forwarded to another assembly, under <paramref name="parent"/>.</summary>
    internal static SubjectNode Forwarded(LoadedModule module, BrowseNode parent, ForwardedType type) =>
        new(parent, new NodeSubject.ForwardedType(new ForwardedTypeInfo(module, type)));

    /// <summary>The node of the type <paramref name="handle"/> names, used from a reference, under this node.</summary>
    private SubjectNode UsedType(LoadedModule module, TypeReferenceHandle handle)
    {
        var metadata = module.File.Metadata;
        var reference = metadata.GetTypeReference(handle);
        var name = metadata.GetString(reference.Name);
        var bare = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name, out var arity);
        var label = ForwardedTypes.LabelOf(bare, arity);
        var ns = metadata.GetString(reference.Namespace);
        var info = new TypeReferenceInfo(module, handle, ns, label, QualifiedName(ns, label));
        return new SubjectNode(this, new NodeSubject.TypeReference(info));
    }

    /// <summary>The full name of a type labelled <paramref name="label"/> listed under this node: after the
    /// type it is nested in, when this node is one, else after its namespace.</summary>
    private string QualifiedName(string ns, string label)
    {
        if (Subject is NodeSubject.TypeReference)
            return $"{FullName}.{label}";
        if (ns.Length == 0)
            return label;
        return $"{ns}.{label}";
    }

    private static bool UsesAnythingOf(TypeReferenceInfo type)
    {
        var usage = type.Module.ReferenceUsage;
        if (usage.TypesIn(type.Handle).Count > 0)
            return true;
        return usage.MembersOf(type.Handle).Count > 0;
    }

    /// <summary>The types nested in the used type and the members of it the module uses, each sorted by
    /// label; a member used under several type arguments once.</summary>
    private BrowseNode[] ListUsedOf(TypeReferenceInfo type)
    {
        var usage = type.Module.ReferenceUsage;
        var module = type.Module.TypeSystem.MainModule;
        var nested = usage.TypesIn(type.Handle)
            .Select(h => UsedType(type.Module, h))
            .OrderBy(n => n.Text, NaturalStringComparer.Instance)
            .Cast<BrowseNode>();
        var members = usage.MembersOf(type.Handle)
            .Select(h => UsedMember(type.Module, h, ResolveDeclared(module, h)))
            .DistinctBy(n => n.Text)
            .OrderBy(n => n.Text, NaturalStringComparer.Instance);
        return nested
            .Concat(members)
            .ToArray();
    }

    private SubjectNode UsedMember(LoadedModule module, MemberReferenceHandle handle, IMember declared)
    {
        var info = new MemberReferenceInfo(module, handle, declared, $"{FullName}.{EntityText.Label(declared)}");
        return new SubjectNode(this, new NodeSubject.MemberReference(info));
    }

    /// <summary>The member <paramref name="handle"/> uses as its type declares it: for an accessor, the
    /// property or event it belongs to.</summary>
    private static IMember ResolveDeclared(MetadataModule module, MemberReferenceHandle handle)
    {
        var declared = ResolveUsed(module, handle).MemberDefinition;
        if (declared is IMethod method && method.AccessorOwner != null)
            return method.AccessorOwner;
        return declared;
    }

    private static IMember ResolveUsed(MetadataModule module, MemberReferenceHandle handle)
    {
        var reference = module.MetadataFile.Metadata.GetMemberReference(handle);
        if (reference.GetKind() == MemberReferenceKind.Field)
            return (IMember)module.ResolveEntity(handle);
        return module.ResolveMethod(handle, default);
    }

    /// <summary>The modules <paramref name="module"/> references, sorted by name, under <paramref name="parent"/>.
    /// Which methods import from each is read here, off the UI thread, so that their rows can tell whether
    /// they have children without reading it there.</summary>
    private static BrowseNode[] ListModuleReferences(LoadedModule module, BrowseNode parent)
    {
        var metadata = module.File.Metadata;
        var imports = ListImportsByModule(metadata);
        return Enumerable.Range(1, metadata.GetTableRowCount(TableIndex.ModuleRef))
            .Select(MetadataTokens.ModuleReferenceHandle)
            .Select(h => ModuleReferenceOf(module, parent, h, imports))
            .OrderBy(n => n.Text, NaturalStringComparer.Instance)
            .Cast<BrowseNode>()
            .ToArray();
    }

    private static SubjectNode ModuleReferenceOf(LoadedModule module, BrowseNode parent, ModuleReferenceHandle handle,
        Dictionary<ModuleReferenceHandle, List<MethodDefinitionHandle>> imports)
    {
        var metadata = module.File.Metadata;
        var name = metadata.GetString(metadata.GetModuleReference(handle).Name);
        var info = new ModuleReferenceInfo(module, name, IsModuleWithMetadata(metadata, name), ImportsFrom(imports, handle));
        return new SubjectNode(parent, new NodeSubject.ModuleReference(info));
    }

    /// <summary>Whether the assembly's file table lists the module as one holding metadata.</summary>
    private static bool IsModuleWithMetadata(MetadataReader metadata, string name)
    {
        foreach (var handle in metadata.AssemblyFiles)
        {
            var file = metadata.GetAssemblyFile(handle);
            if (metadata.StringComparer.Equals(file.Name, name))
                return file.ContainsMetadata;
        }
        return false;
    }

    /// <summary>The methods imported from each module, read from what each method's import names.</summary>
    private static Dictionary<ModuleReferenceHandle, List<MethodDefinitionHandle>> ListImportsByModule(MetadataReader metadata)
    {
        var imports = new Dictionary<ModuleReferenceHandle, List<MethodDefinitionHandle>>();
        foreach (var handle in metadata.MethodDefinitions)
        {
            var module = metadata.GetMethodDefinition(handle).GetImport().Module;
            if (module.IsNil)
                continue;
            if (!imports.TryGetValue(module, out var methods))
            {
                methods = new List<MethodDefinitionHandle>();
                imports.Add(module, methods);
            }
            methods.Add(handle);
        }
        return imports;
    }

    private static IReadOnlyList<MethodDefinitionHandle> ImportsFrom(Dictionary<ModuleReferenceHandle, List<MethodDefinitionHandle>> imports,
        ModuleReferenceHandle module)
    {
        if (imports.TryGetValue(module, out var methods))
            return methods;
        return [];
    }

    /// <summary>The methods imported from the module, sorted by label, then by the type declaring each.</summary>
    private BrowseNode[] ListImports(ModuleReferenceInfo module)
    {
        var typeSystem = module.Module.TypeSystem.MainModule;
        var imports = new List<ImportedMethodInfo>();
        foreach (var handle in module.Imports)
        {
            var import = ImportOf(typeSystem.GetDefinition(handle), module);
            if (import != null)
                imports.Add(import);
        }
        return imports
            .OrderBy(i => i.Label, NaturalStringComparer.Instance)
            .ThenBy(i => i.DeclaringTypeName, NaturalStringComparer.Instance)
            .Select(i => (BrowseNode)new SubjectNode(this, new NodeSubject.ImportedMethod(i)))
            .ToArray();
    }

    /// <summary>The import of <paramref name="method"/>: labelled as the tree labels the method, beside the
    /// type declaring it. A stub the compiler generates as a local function, as <c>[LibraryImport]</c>
    /// does, is labelled and goes as the method declaring it, with the stub's own parameters, the ones
    /// the module is called with. Null for a method no type declares or with no address.</summary>
    private static ImportedMethodInfo? ImportOf(IMethod method, ModuleReferenceInfo module)
    {
        var declaring = method.DeclaringTypeDefinition;
        if (declaring == null)
            return null;
        var shown = DefinedMember.Of(module.Module, FindEnclosingMethod(method, declaring));
        if (shown == null)
            return null;
        var entryPoint = ReadEntryPoint(module.Module.File.Metadata, method);
        return new ImportedMethodInfo(shown, ImportLabelOf(method), EntityText.FullName(declaring), entryPoint, module.Name);
    }

    /// <summary>The tree's label for <paramref name="method"/>, named as the method enclosing it when it is a local function.</summary>
    private static string ImportLabelOf(IMethod method)
    {
        var label = EntityText.Label(method);
        var enclosing = FindEnclosingName(method.Name);
        if (enclosing == null)
            return label;
        return enclosing + label.Substring(method.Name.Length);
    }

    /// <summary>The name of the method a local function is declared in, as the compiler writes it into the
    /// function's name, e.g. <c>CloseHandle</c> for <c>&lt;CloseHandle&gt;g____PInvoke|277_0</c>; null
    /// for a method that is no local function.</summary>
    private static string? FindEnclosingName(string name)
    {
        if (!name.StartsWith('<'))
            return null;
        var end = name.IndexOf(LocalFunctionMarker, StringComparison.Ordinal);
        if (end < 0)
            return null;
        return name.Substring(1, end - 1);
    }

    /// <summary>The method <paramref name="method"/> is declared in when it is a local function and its type
    /// declares a method of that name, else <paramref name="method"/> itself.</summary>
    private static IMethod FindEnclosingMethod(IMethod method, ITypeDefinition declaring)
    {
        var enclosing = FindEnclosingName(method.Name);
        if (enclosing == null)
            return method;
        var declared = declaring.Methods.FirstOrDefault(m => m.Name == enclosing);
        if (declared == null)
            return method;
        return declared;
    }

    /// <summary>The name the method's import gives, else the method's own, as the runtime looks it up.</summary>
    private static string ReadEntryPoint(MetadataReader metadata, IMethod method)
    {
        var handle = (MethodDefinitionHandle)method.MetadataToken;
        var import = metadata.GetMethodDefinition(handle).GetImport();
        if (import.Name.IsNil)
            return method.Name;
        return metadata.GetString(import.Name);
    }
}

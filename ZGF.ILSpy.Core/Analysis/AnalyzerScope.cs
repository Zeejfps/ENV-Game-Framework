// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>
/// Where the symbols related to an analyzed entity can be: only inside its type when it is
/// private, its assembly and the friends it shows internals to when it is internal, and otherwise
/// every open assembly that references its type.
/// </summary>
internal sealed class AnalyzerScope
{
    private const GetMemberOptions MemberDefinitions = GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions;

    private readonly AssemblyList _list;
    private readonly IReadOnlyList<LoadedModule> _assemblies;
    private readonly Accessibility _effectiveAccessibility;

    public AnalyzerScope(AssemblyList list, IEntity entity)
    {
        _list = list;
        _assemblies = list.Assemblies;
        DetermineEffectiveAccessibility(entity, out var typeScope, out _effectiveAccessibility);
        TypeScope = typeScope;
        IsLocal = _effectiveAccessibility.LessThanOrEqual(Accessibility.Private);
    }

    /// <summary>Whether the analyzed symbol is reachable only from inside <see cref="TypeScope"/>.</summary>
    public bool IsLocal { get; }

    public ITypeDefinition TypeScope { get; }

    public IEnumerable<MetadataFile> GetModulesInScope(CancellationToken ct)
    {
        if (IsLocal)
            return [TypeScope.ParentModule!.MetadataFile!];
        if (_effectiveAccessibility.LessThanOrEqual(Accessibility.Internal))
            return GetModuleAndAnyFriends(TypeScope, ct);
        return GetReferencingModules(TypeScope.ParentModule!.MetadataFile!, ct);
    }

    public DecompilerTypeSystem ConstructTypeSystem(MetadataFile module) => _list.TypeSystemOf(module);

    public IEnumerable<ITypeDefinition> GetTypesInScope(CancellationToken ct)
    {
        if (IsLocal)
        {
            foreach (var type in TreeTraversal.PreOrder(TypeScope, static t => t.NestedTypes))
                yield return type;
        }
        else
        {
            foreach (var module in GetModulesInScope(ct))
            {
                foreach (var type in ConstructTypeSystem(module).MainModule.TypeDefinitions)
                    yield return type;
            }
        }
    }

    /// <summary>The members in scope with code <paramref name="matches"/> accepts, type by type:
    /// each such method, then each property and event with such an accessor.</summary>
    public IEnumerable<IEntity> MembersWhoseCode(Func<IMethod, bool> matches, CancellationToken ct)
    {
        foreach (var type in GetTypesInScope(ct))
        {
            if (type.ParentModule?.MetadataFile == null)
                continue;
            foreach (var method in type.GetMembers(static m => m is IMethod, MemberDefinitions).OfType<IMethod>())
            {
                if (matches(method))
                    yield return method;
            }
            foreach (var member in type.Properties.Concat<IMember>(type.Events))
            {
                if (Members.Accessors(member).Any(matches))
                    yield return member;
            }
        }
    }

    private static void DetermineEffectiveAccessibility(IEntity input, out ITypeDefinition typeScope, out Accessibility accessibility)
    {
        if (input is ITypeDefinition definition)
        {
            accessibility = Accessibility.Public;
            typeScope = definition;
        }
        else
        {
            accessibility = input.Accessibility;
            typeScope = input.DeclaringTypeDefinition!;
        }
        var previous = typeScope;
        while (typeScope != null && !accessibility.LessThanOrEqual(Accessibility.Private))
        {
            accessibility = accessibility.Intersect(typeScope.Accessibility);
            previous = typeScope;
            typeScope = previous.DeclaringTypeDefinition!;
        }
        typeScope ??= previous;
    }

    private IEnumerable<MetadataFile> GetReferencingModules(MetadataFile self, CancellationToken ct)
    {
        yield return self;
        var scopeName = TypeScope.Name;
        if (TypeScope.TypeParameterCount > 0)
            scopeName += "`" + TypeScope.TypeParameterCount;
        var toWalk = new Stack<MetadataFile>();
        var checkedFiles = new HashSet<MetadataFile> { self };
        toWalk.Push(self);
        do
        {
            var current = toWalk.Pop();
            foreach (var assembly in _assemblies)
            {
                ct.ThrowIfCancellationRequested();
                var module = assembly.File;
                if (!module.IsAssembly || checkedFiles.Contains(module)) continue;
                var found = false;
                foreach (var reference in module.AssemblyReferences)
                {
                    if (_list.FindKnown(reference, exactOrNewer: true)?.File == current)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found || !checkedFiles.Add(module)) continue;
                if (ModuleReferencesScopeType(module.Metadata, scopeName, TypeScope.Namespace))
                    yield return module;
                if (ModuleForwardsScopeType(module.Metadata, scopeName, TypeScope.Namespace))
                    toWalk.Push(module);
            }
        } while (toWalk.Count > 0);
    }

    private IEnumerable<MetadataFile> GetModuleAndAnyFriends(ITypeDefinition typeScope, CancellationToken ct)
    {
        var self = typeScope.ParentModule!.MetadataFile!;
        yield return self;
        var provider = MetadataExtensions.MinimalAttributeTypeProvider;
        var friends = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var handle in self.Metadata.CustomAttributes)
        {
            var attribute = self.Metadata.GetCustomAttribute(handle);
            if (!IsInternalsVisibleTo(self.Metadata, attribute)) continue;
            if (attribute.DecodeValue(provider).FixedArguments[0].Value is string name)
                friends.Add(name.Split(',')[0]);
        }
        if (friends.Count == 0) yield break;
        foreach (var assembly in _assemblies)
        {
            ct.ThrowIfCancellationRequested();
            if (!friends.Contains(assembly.Name) || assembly.File.IsMetadataOnly) continue;
            if (ModuleReferencesScopeType(assembly.File.Metadata, typeScope.Name, typeScope.Namespace))
                yield return assembly.File;
        }
    }

    private static bool IsInternalsVisibleTo(MetadataReader metadata, CustomAttribute attribute)
    {
        var type = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
            HandleKind.MethodDefinition => metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
            _ => default(EntityHandle),
        };
        var (ns, name) = type.Kind switch
        {
            HandleKind.TypeReference when metadata.GetTypeReference((TypeReferenceHandle)type) is var r => (r.Namespace, r.Name),
            HandleKind.TypeDefinition when metadata.GetTypeDefinition((TypeDefinitionHandle)type) is var d => (d.Namespace, d.Name),
            _ => (default(StringHandle), default(StringHandle)),
        };
        return !name.IsNil && metadata.StringComparer.Equals(ns, "System.Runtime.CompilerServices")
            && metadata.StringComparer.Equals(name, "InternalsVisibleToAttribute");
    }

    private static bool ModuleReferencesScopeType(MetadataReader metadata, string typeScopeName, string typeScopeNamespace)
    {
        foreach (var handle in metadata.TypeReferences)
        {
            var typeRef = metadata.GetTypeReference(handle);
            if (metadata.StringComparer.Equals(typeRef.Name, typeScopeName) && metadata.StringComparer.Equals(typeRef.Namespace, typeScopeNamespace))
                return true;
        }
        return false;
    }

    private static bool ModuleForwardsScopeType(MetadataReader metadata, string typeScopeName, string typeScopeNamespace)
    {
        foreach (var handle in metadata.ExportedTypes)
        {
            var exported = metadata.GetExportedType(handle);
            if (exported.IsForwarder && metadata.StringComparer.Equals(exported.Name, typeScopeName)
                && metadata.StringComparer.Equals(exported.Namespace, typeScopeNamespace))
                return true;
        }
        return false;
    }
}

using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Tests;

/// <summary>
/// The dependency graph of the browse tree, read from Core's own metadata and IL: what each type names in
/// its signatures and method bodies, nested types and lambdas counted as the type they are in. No arrow
/// points back up:
/// <code>
/// UI → BrowseSession, NodeContentLoader
/// NodeContentLoader → BrowseSession, NodeDecompiler, MetadataGrid, ResourceImage
/// BrowseSession → BrowseNode → NodeSubject, LoadedModule (reading only)
/// NodeDecompiler, MetadataGrid → domain data (LoadedModule, type system)
/// </code>
/// </summary>
public sealed class DependencyTests
{
    private static readonly Lazy<CoreGraph> Graph = new(static () => CoreGraph.Read(typeof(BrowseNode).Assembly.Location));

    public static IEnumerable<object[]> ForbiddenEdges() =>
    [
        [typeof(NodeDecompiler), typeof(BrowseNode)],
        [typeof(NodeDecompiler), typeof(BrowseSession)],
        [typeof(NodeDecompiler), typeof(NodeSubject)],
        [typeof(NodeDecompiler), typeof(NodeContentLoader)],
        [typeof(MetadataGrid), typeof(BrowseNode)],
        [typeof(MetadataGrid), typeof(BrowseSession)],
        [typeof(MetadataGrid), typeof(NodeSubject)],
        [typeof(MetadataGrid), typeof(NodeContentLoader)],
        [typeof(BrowseSession), typeof(NodeContentLoader)],
        [typeof(BrowseSession), typeof(NodeDecompiler)],
        [typeof(BrowseNode), typeof(BrowseSession)],
        [typeof(BrowseNode), typeof(NodeContentLoader)],
        [typeof(BrowseNode), typeof(NodeDecompiler)],
        [typeof(NodeSubject), typeof(BrowseNode)],
        [typeof(NodeSubject), typeof(BrowseSession)],
        [typeof(LoadedAssembly), typeof(BrowseNode)],
        [typeof(LoadedAssembly), typeof(BrowseSession)],
        [typeof(LoadedModule), typeof(BrowseNode)],
        [typeof(LoadedModule), typeof(BrowseSession)],
        [typeof(AssemblyList), typeof(BrowseNode)],
        [typeof(AssemblyList), typeof(BrowseSession)],
    ];

    [Theory]
    [MemberData(nameof(ForbiddenEdges))]
    public void A_type_lower_in_the_graph_never_names_one_above_it(Type from, Type to)
    {
        Assert.DoesNotContain(NameOf(to), Graph.Value.Uses(NameOf(from)));
    }

    [Fact]
    public void The_graph_sees_the_edges_that_are_there()
    {
        Assert.Contains(NameOf(typeof(BrowseSession)), Graph.Value.Uses(NameOf(typeof(NodeContentLoader))));
        Assert.Contains(NameOf(typeof(NodeDecompiler)), Graph.Value.Uses(NameOf(typeof(NodeContentLoader))));
        Assert.Contains(NameOf(typeof(NodeSubject)), Graph.Value.Uses(NameOf(typeof(BrowseNode))));
        Assert.Contains(NameOf(typeof(LoadedModule)), Graph.Value.Uses(NameOf(typeof(NodeDecompiler))));
    }

    private static string NameOf(Type type) => type.FullName!;

    /// <summary>For each top-level type of an assembly, the other top-level types of it that it names.</summary>
    private sealed class CoreGraph
    {
        private readonly Dictionary<string, HashSet<string>> _uses;

        private CoreGraph(Dictionary<string, HashSet<string>> uses)
        {
            _uses = uses;
        }

        public IReadOnlySet<string> Uses(string type)
        {
            Assert.True(_uses.ContainsKey(type), $"{type} is not a top-level type of the assembly.");
            return _uses[type];
        }

        public static CoreGraph Read(string path)
        {
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            var metadata = pe.GetMetadataReader();
            var scan = new Scan(pe, metadata);
            foreach (var handle in metadata.TypeDefinitions)
                scan.Type(handle);
            return new CoreGraph(scan.Uses);
        }
    }

    /// <summary>Reads what the types of one assembly name, by the top-level types they are in.</summary>
    private sealed class Scan : ISignatureTypeProvider<ImmutableArray<TypeDefinitionHandle>, object?>
    {
        private readonly PEReader _pe;
        private readonly MetadataReader _metadata;

        public Scan(PEReader pe, MetadataReader metadata)
        {
            _pe = pe;
            _metadata = metadata;
        }

        public Dictionary<string, HashSet<string>> Uses { get; } = new();

        public void Type(TypeDefinitionHandle handle)
        {
            var uses = UsesOf(Outermost(handle));
            var type = _metadata.GetTypeDefinition(handle);
            Add(uses, Types(type.BaseType));
            foreach (var implementation in type.GetInterfaceImplementations())
                Add(uses, Types(_metadata.GetInterfaceImplementation(implementation).Interface));
            foreach (var field in type.GetFields())
                Add(uses, _metadata.GetFieldDefinition(field).DecodeSignature(this, null));
            foreach (var property in type.GetProperties())
                Add(uses, Flatten(_metadata.GetPropertyDefinition(property).DecodeSignature(this, null)));
            foreach (var method in type.GetMethods())
                Method(uses, method);
        }

        private void Method(HashSet<string> uses, MethodDefinitionHandle handle)
        {
            var method = _metadata.GetMethodDefinition(handle);
            Add(uses, Flatten(method.DecodeSignature(this, null)));
            if (method.RelativeVirtualAddress == 0)
                return;
            var body = _pe.GetMethodBody(method.RelativeVirtualAddress);
            if (!body.LocalSignature.IsNil)
            {
                foreach (var local in _metadata.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(this, null))
                    Add(uses, local);
            }
            var il = body.GetILReader();
            while (il.RemainingBytes > 0)
            {
                var opCode = il.DecodeOpCode();
                if (opCode.GetOperandType() is OperandType.Field or OperandType.Method or OperandType.Tok or OperandType.Type)
                    Add(uses, Types(MetadataTokenHelpers.EntityHandleOrNil(il.ReadInt32())));
                else
                    il.SkipOperand(opCode);
            }
        }

        private HashSet<string> UsesOf(TypeDefinitionHandle outermost)
        {
            var name = NameOf(outermost);
            if (!Uses.TryGetValue(name, out var uses))
            {
                uses = new HashSet<string>(StringComparer.Ordinal);
                Uses.Add(name, uses);
            }
            return uses;
        }

        private void Add(HashSet<string> uses, ImmutableArray<TypeDefinitionHandle> types)
        {
            foreach (var type in types)
                uses.Add(NameOf(Outermost(type)));
        }

        /// <summary>The types of this assembly <paramref name="handle"/> names: a type, a member's type and
        /// signature, or a generic instance's arguments.</summary>
        private ImmutableArray<TypeDefinitionHandle> Types(EntityHandle handle)
        {
            if (handle.IsNil)
                return [];
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return [(TypeDefinitionHandle)handle];
                case HandleKind.TypeSpecification:
                    return _metadata.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(this, null);
                case HandleKind.FieldDefinition:
                    var field = _metadata.GetFieldDefinition((FieldDefinitionHandle)handle);
                    return [field.GetDeclaringType(), .. field.DecodeSignature(this, null)];
                case HandleKind.MethodDefinition:
                    var method = _metadata.GetMethodDefinition((MethodDefinitionHandle)handle);
                    return [method.GetDeclaringType(), .. Flatten(method.DecodeSignature(this, null))];
                case HandleKind.MemberReference:
                    var member = _metadata.GetMemberReference((MemberReferenceHandle)handle);
                    var signature = member.GetKind() == MemberReferenceKind.Field
                        ? member.DecodeFieldSignature(this, null)
                        : Flatten(member.DecodeMethodSignature(this, null));
                    return [.. Types(member.Parent), .. signature];
                case HandleKind.MethodSpecification:
                    var specification = _metadata.GetMethodSpecification((MethodSpecificationHandle)handle);
                    return [.. Types(specification.Method), .. specification.DecodeSignature(this, null).SelectMany(static a => a)];
                default:
                    return [];
            }
        }

        private static ImmutableArray<TypeDefinitionHandle> Flatten(MethodSignature<ImmutableArray<TypeDefinitionHandle>> signature) =>
            [.. signature.ReturnType, .. signature.ParameterTypes.SelectMany(static p => p)];

        private TypeDefinitionHandle Outermost(TypeDefinitionHandle handle)
        {
            while (true)
            {
                var declaring = _metadata.GetTypeDefinition(handle).GetDeclaringType();
                if (declaring.IsNil)
                    return handle;
                handle = declaring;
            }
        }

        private string NameOf(TypeDefinitionHandle handle)
        {
            var type = _metadata.GetTypeDefinition(handle);
            var name = _metadata.GetString(type.Name);
            var ns = _metadata.GetString(type.Namespace);
            return ns.Length == 0 ? name : $"{ns}.{name}";
        }

        public ImmutableArray<TypeDefinitionHandle> GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => [handle];

        public ImmutableArray<TypeDefinitionHandle> GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => [];

        public ImmutableArray<TypeDefinitionHandle> GetTypeFromSpecification(MetadataReader reader, object? context, TypeSpecificationHandle handle,
            byte rawTypeKind) => Types(handle);

        public ImmutableArray<TypeDefinitionHandle> GetPrimitiveType(PrimitiveTypeCode typeCode) => [];

        public ImmutableArray<TypeDefinitionHandle> GetSZArrayType(ImmutableArray<TypeDefinitionHandle> elementType) => elementType;

        public ImmutableArray<TypeDefinitionHandle> GetArrayType(ImmutableArray<TypeDefinitionHandle> elementType, ArrayShape shape) => elementType;

        public ImmutableArray<TypeDefinitionHandle> GetByReferenceType(ImmutableArray<TypeDefinitionHandle> elementType) => elementType;

        public ImmutableArray<TypeDefinitionHandle> GetPointerType(ImmutableArray<TypeDefinitionHandle> elementType) => elementType;

        public ImmutableArray<TypeDefinitionHandle> GetPinnedType(ImmutableArray<TypeDefinitionHandle> elementType) => elementType;

        public ImmutableArray<TypeDefinitionHandle> GetGenericInstantiation(ImmutableArray<TypeDefinitionHandle> genericType,
            ImmutableArray<ImmutableArray<TypeDefinitionHandle>> typeArguments) =>
            [.. genericType, .. typeArguments.SelectMany(static a => a)];

        public ImmutableArray<TypeDefinitionHandle> GetGenericMethodParameter(object? context, int index) => [];

        public ImmutableArray<TypeDefinitionHandle> GetGenericTypeParameter(object? context, int index) => [];

        public ImmutableArray<TypeDefinitionHandle> GetFunctionPointerType(MethodSignature<ImmutableArray<TypeDefinitionHandle>> signature) => Flatten(signature);

        public ImmutableArray<TypeDefinitionHandle> GetModifiedType(ImmutableArray<TypeDefinitionHandle> modifier, ImmutableArray<TypeDefinitionHandle> unmodifiedType,
            bool isRequired) => unmodifiedType;
    }
}

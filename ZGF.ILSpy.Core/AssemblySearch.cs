using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>A search hit: the <see cref="Node"/> found, its label, and where it lives.</summary>
/// <param name="Location">Where it lives, as ILSpy's results show it: the namespace and enclosing
/// type of a type or member, <c>-</c> for the global namespace, the resources file holding a
/// resource entry, an assembly's file; empty for a namespace or a resource of its own.</param>
/// <param name="Fitness">How closely the hit matches, as ILSpy ranks it: the shorter the name the
/// closer, and 0 for compiler-generated names.</param>
public sealed record SearchResult(BrowseNode Node, string Text, string Location, float Fitness);

internal static class AssemblySearch
{
    private const int BatchSize = 256;
    private static readonly long FlushTicks = Stopwatch.Frequency / 20;

    public static async IAsyncEnumerable<IReadOnlyList<SearchResult>> Run(ImmutableArray<LoadedAssembly> assemblies, SearchQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (query.IsEmpty || assemblies.IsEmpty) yield break;

        var channel = Channel.CreateUnbounded<IReadOnlyList<SearchResult>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = stop.Token;
        _ = Task.Run(() =>
        {
            try
            {
                var search = new Search(query, channel.Writer, token);
                foreach (var assembly in assemblies)
                    search.Run(assembly);
                search.Flush();
                channel.Writer.TryComplete();
            }
            catch (Exception e)
            {
                channel.Writer.TryComplete(e);
            }
        }, CancellationToken.None);

        try
        {
            await foreach (var batch in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return batch;
        }
        finally
        {
            stop.Cancel();
        }
    }

    private sealed class Search(SearchQuery query, ChannelWriter<IReadOnlyList<SearchResult>> writer, CancellationToken cancellationToken)
    {
        private readonly HashSet<BrowseNode> _found = new(ReferenceEqualityComparer.Instance);
        private List<SearchResult> _batch = new();
        private long _lastFlush = Stopwatch.GetTimestamp();

        public void Run(LoadedAssembly assembly)
        {
            if (query.InAssembly is { } scope && !InAssembly(assembly, scope)) return;
            var kinds = query.Kinds;
            Scan(kinds, SearchKinds.Assemblies, MatchAssembly, assembly);
            Scan(kinds, SearchKinds.Namespaces, ScanNamespaces, assembly);
            Scan(kinds, SearchKinds.TypesAndMembers, ScanEntities, assembly);
            Scan(kinds, SearchKinds.Tokens, FindToken, assembly);
            Scan(kinds, SearchKinds.Literals, ScanLiterals, assembly);
            Scan(kinds, SearchKinds.Resources, ScanResources, assembly);
            Flush();
        }

        private static void Scan(SearchKinds kinds, SearchKinds wanted, Action<LoadedAssembly> scan, LoadedAssembly assembly)
        {
            if ((kinds & wanted) == 0) return;
            try
            {
                scan(assembly);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
            }
        }

        public void Flush()
        {
            _lastFlush = Stopwatch.GetTimestamp();
            if (_batch.Count == 0) return;
            writer.TryWrite(_batch);
            _batch = new List<SearchResult>();
        }

        private static bool InAssembly(LoadedAssembly assembly, string scope) =>
            Path.GetFileName(assembly.FilePath).Contains(scope, StringComparison.OrdinalIgnoreCase) ||
            assembly.FullName.Contains(scope, StringComparison.OrdinalIgnoreCase);

        private bool InNamespace(string ns) =>
            query.InNamespace is not { } scope || (scope.Length == 0 ? ns.Length == 0 : ns.Contains(scope, StringComparison.OrdinalIgnoreCase));

        private void ScanEntities(LoadedAssembly assembly)
        {
            var reader = assembly.File.Metadata;
            var kinds = query.Kinds;
            var types = (kinds & SearchKinds.Types) != 0;
            var methods = (kinds & SearchKinds.Methods) != 0;
            var fields = (kinds & SearchKinds.Fields) != 0;
            var properties = (kinds & SearchKinds.Properties) != 0;
            var events = (kinds & SearchKinds.Events) != 0;
            foreach (var typeHandle in reader.TypeDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var type = reader.GetTypeDefinition(typeHandle);
                if (!InNamespace(NamespaceOf(reader, type))) continue;
                var typeName = query.FullName ? TypeName(reader, typeHandle) : TypeLabel(reader, typeHandle);
                if (types && query.IsMatch(typeName))
                    Add(assembly, typeHandle);
                if (fields)
                {
                    foreach (var handle in type.GetFields())
                        MatchMember(assembly, typeName, Name(reader, reader.GetFieldDefinition(handle).Name), handle);
                }
                if (properties)
                {
                    foreach (var handle in type.GetProperties())
                        MatchMember(assembly, typeName, Name(reader, reader.GetPropertyDefinition(handle).Name), handle);
                }
                if (events)
                {
                    foreach (var handle in type.GetEvents())
                        MatchMember(assembly, typeName, Name(reader, reader.GetEventDefinition(handle).Name), handle);
                }
                if (methods)
                {
                    foreach (var handle in type.GetMethods())
                    {
                        var method = reader.GetMethodDefinition(handle);
                        if (!reader.StringComparer.StartsWith(method.Name, "."))
                            MatchMember(assembly, typeName, WithTypeParameters(reader, Name(reader, method.Name), method.GetGenericParameters(), 0), handle);
                    }
                }
            }
        }

        private void MatchMember(LoadedAssembly assembly, string typeName, string name, EntityHandle handle)
        {
            if (query.IsMatch(query.FullName ? typeName + "." + name : name))
                Add(assembly, handle);
        }

        private string Name(MetadataReader reader, StringHandle handle)
        {
            var name = reader.GetString(handle);
            if (query.KeepArity && !query.Generics) return name;
            var tick = name.IndexOf('`');
            return tick < 0 ? name : name[..tick];
        }

        private string TypeLabel(MetadataReader reader, TypeDefinitionHandle handle)
        {
            var type = reader.GetTypeDefinition(handle);
            var name = Name(reader, type.Name);
            if (!query.Generics) return name;
            var declaring = type.GetDeclaringType();
            var inherited = declaring.IsNil ? 0 : reader.GetTypeDefinition(declaring).GetGenericParameters().Count;
            return WithTypeParameters(reader, name, type.GetGenericParameters(), inherited);
        }

        private string WithTypeParameters(MetadataReader reader, string name, GenericParameterHandleCollection parameters, int inherited)
        {
            if (!query.Generics || parameters.Count <= inherited) return name;
            var text = new StringBuilder(name).Append('<');
            for (var i = inherited; i < parameters.Count; i++)
            {
                if (i > inherited) text.Append(", ");
                text.Append(reader.GetString(reader.GetGenericParameter(parameters[i]).Name));
            }
            return text.Append('>').ToString();
        }

        private string TypeName(MetadataReader reader, TypeDefinitionHandle handle)
        {
            var type = reader.GetTypeDefinition(handle);
            var name = TypeLabel(reader, handle);
            var declaring = type.GetDeclaringType();
            if (!declaring.IsNil) return TypeName(reader, declaring) + "." + name;
            var ns = reader.GetString(type.Namespace);
            return ns.Length == 0 ? name : ns + "." + name;
        }

        private static string NamespaceOf(MetadataReader reader, TypeDefinition type)
        {
            while (true)
            {
                var declaring = type.GetDeclaringType();
                if (declaring.IsNil) return reader.GetString(type.Namespace);
                type = reader.GetTypeDefinition(declaring);
            }
        }

        private void ScanLiterals(LoadedAssembly assembly)
        {
            var file = assembly.File;
            var reader = file.Metadata;
            var literal = query.Literal();
            foreach (var handle in reader.MethodDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var method = reader.GetMethodDefinition(handle);
                if (!method.HasBody() || !InNamespace(NamespaceOf(reader, reader.GetTypeDefinition(method.GetDeclaringType())))) continue;
                BlobReader body;
                try
                {
                    body = file.GetMethodBody(method.RelativeVirtualAddress).GetILReader();
                }
                catch (BadImageFormatException)
                {
                    continue;
                }
                if (BodyMatches(ref body, reader, literal))
                    AddOrAncestor(assembly, handle);
            }
            foreach (var handle in reader.FieldDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var field = reader.GetFieldDefinition(handle);
                if ((field.Attributes & FieldAttributes.Literal) == 0) continue;
                var constantHandle = field.GetDefaultValue();
                if (constantHandle.IsNil || !InNamespace(NamespaceOf(reader, reader.GetTypeDefinition(field.GetDeclaringType())))) continue;
                var constant = reader.GetConstant(constantHandle);
                var blob = reader.GetBlobReader(constant.Value);
                if (ConstantMatches(blob.ReadConstant(constant.TypeCode), literal))
                    Add(assembly, handle);
            }
        }

        private bool ConstantMatches(object? value, object? literal) => (value, literal) switch
        {
            (null, _) => false,
            (string or char or bool, long or float or double) => false,
            (_, long number) => value is not float and not double && Convert.ToInt64(value is ulong u ? (long)u : value) == number,
            (_, float single) => value is float f && f == single,
            (_, double real) => value is double d && d == real,
            _ => query.IsMatch(value.ToString() ?? ""),
        };

        private bool BodyMatches(ref BlobReader body, MetadataReader reader, object? literal)
        {
            while (body.RemainingBytes > 0)
            {
                var code = body.DecodeOpCode();
                switch (literal)
                {
                    case long number when IsIntegerLoad(code):
                        if (IntegerOperand(ref body, code) == number) return true;
                        continue;
                    case float single when code == ILOpCode.Ldc_r4:
                        if (body.ReadSingle() == single) return true;
                        continue;
                    case double real when code == ILOpCode.Ldc_r8:
                        if (body.ReadDouble() == real) return true;
                        continue;
                    case null when code == ILOpCode.Ldstr:
                        if (query.IsMatch(body.DecodeUserString(reader))) return true;
                        continue;
                }
                body.SkipOperand(code);
            }
            return false;
        }

        private static bool IsIntegerLoad(ILOpCode code) => code is >= ILOpCode.Ldc_i4_m1 and <= ILOpCode.Ldc_i8;

        private static long IntegerOperand(ref BlobReader body, ILOpCode code) => code switch
        {
            ILOpCode.Ldc_i4_s => body.ReadSByte(),
            ILOpCode.Ldc_i4 => body.ReadInt32(),
            ILOpCode.Ldc_i8 => body.ReadInt64(),
            _ => code - ILOpCode.Ldc_i4_0,
        };

        private void FindToken(LoadedAssembly assembly)
        {
            var term = query.Terms.Count == 1 ? query.Terms[0] : "";
            if (term.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) term = term[2..];
            if (!int.TryParse(term, System.Globalization.NumberStyles.AllowHexSpecifier, null, out var token)) return;
            var handle = MetadataTokenHelpers.EntityHandleOrNil(token);
            if (handle.IsNil) return;
            var reader = assembly.File.Metadata;
            var rows = handle.Kind switch
            {
                HandleKind.TypeDefinition => reader.TypeDefinitions.Count,
                HandleKind.MethodDefinition => reader.MethodDefinitions.Count,
                HandleKind.FieldDefinition => reader.FieldDefinitions.Count,
                HandleKind.PropertyDefinition => reader.PropertyDefinitions.Count,
                HandleKind.EventDefinition => reader.EventDefinitions.Count,
                _ => 0,
            };
            var row = MetadataTokens.GetRowNumber(handle);
            if (row >= 1 && row <= rows)
                AddOrAncestor(assembly, handle);
        }

        private void ScanResources(LoadedAssembly assembly)
        {
            foreach (var child in assembly.Root.Children)
            {
                if (child.Kind == NodeKind.ResourcesFolder)
                    ScanResources(child);
            }
        }

        private void ScanResources(BrowseNode parent)
        {
            foreach (var node in parent.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (node.Kind is NodeKind.Error or NodeKind.Loading) continue;
                if (query.IsMatch(node.Text))
                    Add(node, parent.Kind == NodeKind.ResourcesFile ? parent.Text : "");
                if (node.HasChildren)
                    ScanResources(node);
            }
        }

        private void MatchAssembly(LoadedAssembly assembly)
        {
            var matches = query.AssemblyName switch
            {
                AssemblyNameKind.FilePath => query.IsMatch(assembly.FilePath),
                AssemblyNameKind.FullName => query.IsMatch(assembly.FullName),
                _ => query.IsMatch(assembly.Name) || query.IsMatch(Path.GetFileName(assembly.FilePath)),
            };
            if (matches)
                Add(assembly.Root, assembly.FilePath);
        }

        private void ScanNamespaces(LoadedAssembly assembly)
        {
            foreach (var node in assembly.Root.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (node is NamespaceNode ns && InNamespace(ns.Name) && query.IsMatch(ns.Text))
                    Add(node, "");
            }
        }

        private void Add(LoadedAssembly assembly, EntityHandle handle)
        {
            if (Find(assembly, handle) is { Parent: not MemberNode } node)
                Add(node);
        }

        private void AddOrAncestor(LoadedAssembly assembly, EntityHandle handle)
        {
            BrowseNode? node;
            try
            {
                node = assembly.FindNodeOrAncestor(handle);
            }
            catch (Exception e) when (e is BadImageFormatException or InvalidOperationException)
            {
                return;
            }
            if (node is { Parent: MemberNode owner })
                node = owner;
            if (node is not null)
                Add(node);
        }

        private static BrowseNode? Find(LoadedAssembly assembly, EntityHandle handle)
        {
            try
            {
                return assembly.FindNode(handle);
            }
            catch (Exception e) when (e is BadImageFormatException or InvalidOperationException)
            {
                return null;
            }
        }

        private void Add(BrowseNode node)
        {
            var ns = node.Parent;
            while (ns is not null and not NamespaceNode)
                ns = ns.Parent;
            var name = ns?.Text ?? "";
            if (node.Parent is TypeNode type)
                Add(node, name.Length == 0 ? type.Text : $"{name}.{type.Text}");
            else
                Add(node, name.Length == 0 ? "-" : name);
        }

        private void Add(BrowseNode node, string location)
        {
            if (!_found.Add(node)) return;
            _batch.Add(new SearchResult(node, node.Text, location, Fitness(node)));
            if (_batch.Count >= BatchSize || Stopwatch.GetTimestamp() - _lastFlush >= FlushTicks)
                Flush();
        }

        private static float Fitness(BrowseNode node)
        {
            var name = node switch
            {
                EntityNode { Entity: IMethod { IsConstructor: true, DeclaringTypeDefinition: { } type } } => type.Name,
                EntityNode entity => entity.Entity.Name,
                _ => node.Text,
            };
            return name.Length == 0 || name[0] == '<' ? 0f : 1f / name.Length;
        }
    }
}

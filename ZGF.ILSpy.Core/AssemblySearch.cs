using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace ZGF.ILSpy.Core;

/// <summary>What <see cref="AssemblyList.SearchAsync"/> looks at.</summary>
[Flags]
public enum SearchTargets
{
    Types = 1,
    Members = 2,
    All = Types | Members,
}

/// <summary>A search hit: the <see cref="Node"/> found, its label, and where it lives.</summary>
/// <param name="DeclaringType">The enclosing type's label, for members and nested types; null for top-level types.</param>
public sealed record SearchResult(BrowseNode Node, string Text, string? DeclaringType, string Namespace, string AssemblyName);

internal static class AssemblySearch
{
    private const int BatchSize = 256;
    private static readonly long FlushTicks = Stopwatch.Frequency / 20;

    public static async IAsyncEnumerable<IReadOnlyList<SearchResult>> Run(ImmutableArray<LoadedAssembly> assemblies, string text,
        SearchTargets targets, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        text = text.Trim();
        if (text.Length == 0 || assemblies.IsEmpty) yield break;

        var channel = Channel.CreateUnbounded<IReadOnlyList<SearchResult>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = stop.Token;
        _ = Task.Run(() =>
        {
            try
            {
                var search = new Search(text, targets, channel.Writer, token);
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

    private sealed class Search(string text, SearchTargets targets, ChannelWriter<IReadOnlyList<SearchResult>> writer, CancellationToken cancellationToken)
    {
        private List<SearchResult> _batch = new();
        private long _lastFlush = Stopwatch.GetTimestamp();

        public void Run(LoadedAssembly assembly)
        {
            try
            {
                Scan(assembly);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
            }
            Flush();
        }

        public void Flush()
        {
            _lastFlush = Stopwatch.GetTimestamp();
            if (_batch.Count == 0) return;
            writer.TryWrite(_batch);
            _batch = new List<SearchResult>();
        }

        private void Scan(LoadedAssembly assembly)
        {
            var reader = assembly.File.Metadata;
            var types = (targets & SearchTargets.Types) != 0;
            var members = (targets & SearchTargets.Members) != 0;
            foreach (var typeHandle in reader.TypeDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var type = reader.GetTypeDefinition(typeHandle);
                if (types && Matches(reader.GetString(type.Name)))
                    Add(assembly, typeHandle);
                if (!members) continue;

                foreach (var handle in type.GetFields())
                {
                    if (Matches(reader.GetString(reader.GetFieldDefinition(handle).Name)))
                        Add(assembly, handle);
                }
                foreach (var handle in type.GetProperties())
                {
                    if (Matches(reader.GetString(reader.GetPropertyDefinition(handle).Name)))
                        Add(assembly, handle);
                }
                foreach (var handle in type.GetEvents())
                {
                    if (Matches(reader.GetString(reader.GetEventDefinition(handle).Name)))
                        Add(assembly, handle);
                }
                foreach (var handle in type.GetMethods())
                {
                    var name = reader.GetString(reader.GetMethodDefinition(handle).Name);
                    if (!name.StartsWith('.') && Matches(name))
                        Add(assembly, handle);
                }
            }
        }

        private bool Matches(string name)
        {
            var span = name.AsSpan();
            var tick = span.IndexOf('`');
            if (tick >= 0) span = span[..tick];
            return span.Contains(text, StringComparison.OrdinalIgnoreCase);
        }

        private void Add(LoadedAssembly assembly, EntityHandle handle)
        {
            BrowseNode? node;
            try
            {
                node = assembly.FindNode(handle);
            }
            catch (Exception e) when (e is BadImageFormatException or InvalidOperationException)
            {
                return;
            }
            if (node is null || node.Parent is MemberNode) return;

            string? declaringType = node.Parent is TypeNode parent ? parent.Text : null;
            var ns = node.Parent;
            while (ns is not null and not NamespaceNode)
                ns = ns.Parent;
            _batch.Add(new SearchResult(node, node.Text, declaringType, ns?.Text ?? "", assembly.Name));

            if (_batch.Count >= BatchSize || Stopwatch.GetTimestamp() - _lastFlush >= FlushTicks)
                Flush();
        }
    }
}

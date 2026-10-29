using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class BrowseSessionTests : IDisposable
{
    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Children_are_built_once_and_every_caller_gets_the_same_ones()
    {
        var session = new BrowseSession();
        var root = session.RootOf(new AssemblyList().Open(Trees.TestAssemblyPath));

        var waits = Enumerable.Range(0, 8).Select(_ => session.LoadChildrenAsync(root)).ToArray();
        var blocking = Enumerable.Range(0, 8).AsParallel().WithDegreeOfParallelism(8).Select(_ => session.LoadChildren(root)).ToArray();
        var waited = Task.WhenAll(waits).GetAwaiter().GetResult();

        var children = Assert.IsType<ChildrenState.Ready>(session.PeekChildren(root)).Children;
        Assert.All(waited.Concat(blocking), s => Assert.Same(children, Assert.IsType<ChildrenState.Ready>(s).Children));
    }

    [Fact]
    public async Task Cancelling_a_wait_leaves_the_build_running_for_the_others()
    {
        var session = new BrowseSession();
        var root = session.RootOf(new AssemblyList().Open(Trees.TestAssemblyPath));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.LoadChildrenAsync(root, new CancellationToken(canceled: true)));
        var state = await session.LoadChildrenAsync(root);

        Assert.NotEmpty(Assert.IsType<ChildrenState.Ready>(state).Children);
    }

    [Fact]
    public void Children_that_fail_to_build_say_why_once_and_count_as_children()
    {
        var session = new BrowseSession();
        var assembly = new AssemblyList().Open(BrokenAssemblies.WriteUnreadableAvaloniaContainer(_temp));
        var resources = session.ChildrenOf(session.RootOf(assembly)).Single(c => c.Kind == NodeKind.ResourcesFolder);
        var container = Assert.Single(session.ChildrenOf(resources));

        var failed = Assert.IsType<ChildrenState.Failed>(session.LoadChildren(container));

        Assert.IsType<InvalidDataException>(failed.Error);
        Assert.Same(failed, session.LoadChildren(container));
        Assert.True(session.HasChildren(container));
        Assert.Empty(session.ChildrenOf(container));
    }

    [Fact]
    public void Each_session_builds_a_tree_of_its_own()
    {
        var assembly = new AssemblyList().Open(Trees.TestAssemblyPath);
        var first = new BrowseSession();
        var second = new BrowseSession();

        Assert.Same(first.RootOf(assembly), first.RootOf(assembly));
        Assert.NotSame(first.RootOf(assembly), second.RootOf(assembly));
        Assert.IsType<ChildrenState.Unloaded>(second.PeekChildren(first.RootOf(assembly)));
    }
}

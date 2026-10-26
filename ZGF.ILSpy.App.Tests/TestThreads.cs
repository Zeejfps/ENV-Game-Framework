using System.Runtime.CompilerServices;

namespace ZGF.ILSpy.App.Tests;

/// <summary>
/// The thread pool the tests run on. Each test drives the app on its own thread and blocks it while
/// the app's background work runs on the pool, and xUnit runs many tests at once; with no more pool
/// threads than processors, the blocked tests leave the background work queued behind them, the pool
/// adds threads only slowly, and the tests time out together. Starting the pool with headroom keeps
/// the work running.
/// </summary>
internal static class TestThreads
{
    private const int MinPoolThreads = 128;

    [ModuleInitializer]
    internal static void GivePoolHeadroom()
    {
        ThreadPool.GetMinThreads(out var workers, out var completionPorts);
        ThreadPool.SetMinThreads(Math.Max(workers, MinPoolThreads), Math.Max(completionPorts, MinPoolThreads));
    }
}

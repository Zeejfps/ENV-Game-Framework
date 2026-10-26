namespace ZGF.ILSpy.App.Tests;

/// <summary>What frames allocate on the test's thread once warmed up, per frame.</summary>
internal static class SteadyAllocations
{
    private const int Runs = 3;

    /// <summary>The least that any of a few runs of <paramref name="frames"/> calls of
    /// <paramref name="frame"/>, each given its index in the run, allocated per frame. Something
    /// allocated once, such as a cache another test emptied filling again, lands in one run only;
    /// what every frame allocates lands in each.</summary>
    public static long PerFrame(int frames, Action<int> frame)
    {
        var least = long.MaxValue;
        for (var run = 0; run < Runs; run++)
        {
            var perFrame = MeasurePerFrame(frames, frame);
            least = Math.Min(least, perFrame);
        }
        return least;
    }

    private static long MeasurePerFrame(int frames, Action<int> frame)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < frames; i++)
            frame(i);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        return allocated / frames;
    }
}

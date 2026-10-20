using System.Diagnostics.Tracing;
using ZGF.Gui.Immediate;

namespace ZGF.Gui.Benchmarks;

/// <summary>
/// Breaks an immediate-mode frame's allocations down by frame stage and by type, so a regression
/// in <see cref="ImmediateFrameBenchmarks"/> can be traced to its source. Run with
/// <c>--immediate-alloc</c>.
/// </summary>
internal static class ImmediateAllocationProbe
{
    private const int WarmupFrames = 500;
    private const int MeasuredFrames = 20_000;

    public static void Run()
    {
        var bench = new ImmediateFrameBenchmarks();
        bench.Setup();
        for (var i = 0; i < WarmupFrames; i++)
            bench.Frame();

        var stages = new long[ImmediateFrameBenchmarks.StageCount];
        for (var i = 0; i < MeasuredFrames; i++)
            bench.MeasureStages(stages);

        Console.WriteLine($"Allocated bytes per frame by stage ({MeasuredFrames} frames):");
        long total = 0;
        for (var s = 0; s < stages.Length; s++)
        {
            var perFrame = stages[s] / (double)MeasuredFrames;
            total += stages[s];
            Console.WriteLine($"  {ImmediateFrameBenchmarks.StageNames[s],-24} {perFrame,10:F1}");
        }
        Console.WriteLine($"  {"total",-24} {total / (double)MeasuredFrames,10:F1}");

        using var listener = new AllocationListener();
        for (var i = 0; i < MeasuredFrames; i++)
            bench.Frame();

        Console.WriteLine();
        Console.WriteLine("Sampled allocations by type (one sample per ~100 KB):");
        foreach (var (type, bytes) in listener.ByType.OrderByDescending(kv => kv.Value).Take(15))
            Console.WriteLine($"  {bytes / (double)MeasuredFrames,10:F1} B/frame  {type}");
    }

    private sealed class AllocationListener : EventListener
    {
        private const EventKeywords GcKeyword = (EventKeywords)0x1;

        public Dictionary<string, long> ByType { get; } = new();

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "Microsoft-Windows-DotNETRuntime")
                EnableEvents(source, EventLevel.Verbose, GcKeyword);
        }

        protected override void OnEventWritten(EventWrittenEventArgs e)
        {
            if (e.EventName is null || !e.EventName.StartsWith("GCAllocationTick", StringComparison.Ordinal))
                return;
            var type = e.Payload?[e.PayloadNames!.IndexOf("TypeName")] as string ?? "?";
            var amountIndex = e.PayloadNames!.IndexOf("AllocationAmount64");
            var amount = amountIndex >= 0
                ? Convert.ToInt64(e.Payload![amountIndex])
                : Convert.ToInt64(e.Payload![e.PayloadNames.IndexOf("AllocationAmount")]);
            lock (ByType)
                ByType[type] = ByType.GetValueOrDefault(type) + amount;
        }
    }
}

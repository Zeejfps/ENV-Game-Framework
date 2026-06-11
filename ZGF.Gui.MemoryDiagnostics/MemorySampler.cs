using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace ZGF.Gui.MemoryDiagnostics;

/// <summary>
/// Samples a managed-vs-unmanaged memory time series for the current process and writes it
/// to CSV, plus captures categorized native snapshots (footprint / vmmap) at start and end.
///
/// The split is the whole point: <see cref="Sample.ManagedLiveBytes"/> is the live managed
/// heap measured right after a full GC, and <see cref="Sample.WorkingSetBytes"/> is total
/// resident memory. <see cref="Sample.UnmanagedBytes"/> = total - managed-live, so when that
/// column climbs while managed stays flat you've confirmed a native leak — exactly the case
/// Rider flagged. The footprint/vmmap diffs then name the growing native region.
/// </summary>
public sealed class MemorySampler
{
    public readonly record struct Sample(
        double ElapsedSeconds,
        long Frames,
        long ManagedLiveBytes,
        long GcHeapBytes,
        long WorkingSetBytes,
        long UnmanagedBytes);

    private readonly List<Sample> _samples = new();
    private readonly Process _self = Process.GetCurrentProcess();
    private readonly string _outDir;

    public MemorySampler(string outDir)
    {
        _outDir = outDir;
        Directory.CreateDirectory(_outDir);
    }

    public int Pid => _self.Id;

    public Sample Capture(double elapsedSeconds, long frames)
    {
        // forceFullCollection: true gives the true live managed set and collapses
        // uncollected garbage, so any *managed* growth in the series is real retention,
        // not just GC latency.
        var managedLive = GC.GetTotalMemory(forceFullCollection: true);
        var gcHeap = GC.GetGCMemoryInfo().HeapSizeBytes;
        _self.Refresh();
        var workingSet = _self.WorkingSet64;
        var unmanaged = Math.Max(0, workingSet - managedLive);

        var s = new Sample(elapsedSeconds, frames, managedLive, gcHeap, workingSet, unmanaged);
        _samples.Add(s);
        return s;
    }

    /// <summary>
    /// Shells out to Apple's <c>footprint</c> and <c>vmmap --summary</c> for a categorized
    /// native breakdown. Diff the start vs end files to see which region (IOSurface, Metal,
    /// MALLOC_*, …) grew. No-op / best-effort off macOS.
    /// </summary>
    public void CaptureNativeSnapshot(string label)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;
        RunToFile("footprint", _self.Id.ToString(), Path.Combine(_outDir, $"footprint-{label}.txt"));
        RunToFile("vmmap", $"--summary {_self.Id}", Path.Combine(_outDir, $"vmmap-{label}.txt"));
    }

    private static void RunToFile(string exe, string args, string outPath)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi);
            if (p == null) return;
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10_000);
            File.WriteAllText(outPath, stdout);
        }
        catch (Exception e)
        {
            Console.WriteLine($"  ({exe} snapshot failed: {e.Message})");
        }
    }

    public string WriteCsv(string scenario)
    {
        var path = Path.Combine(_outDir, $"memreport-{scenario}.csv");
        var sb = new StringBuilder();
        sb.AppendLine("elapsed_s,frames,managed_live_mb,gc_heap_mb,working_set_mb,unmanaged_mb");
        foreach (var s in _samples)
        {
            sb.Append(s.ElapsedSeconds.ToString("F1", CultureInfo.InvariantCulture)).Append(',')
              .Append(s.Frames).Append(',')
              .Append(Mb(s.ManagedLiveBytes)).Append(',')
              .Append(Mb(s.GcHeapBytes)).Append(',')
              .Append(Mb(s.WorkingSetBytes)).Append(',')
              .Append(Mb(s.UnmanagedBytes))
              .AppendLine();
        }
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    public void PrintSummary()
    {
        if (_samples.Count < 2)
        {
            Console.WriteLine("Not enough samples for a summary.");
            return;
        }

        var first = _samples[0];
        var last = _samples[^1];
        var minutes = (last.ElapsedSeconds - first.ElapsedSeconds) / 60.0;
        if (minutes <= 0) minutes = 1.0 / 60.0;

        Console.WriteLine();
        Console.WriteLine("==================== SUMMARY ====================");
        Console.WriteLine($"  duration: {last.ElapsedSeconds - first.ElapsedSeconds:F0}s   frames: {last.Frames - first.Frames}");
        Console.WriteLine($"  {"",-14}{"start",10}{"end",10}{"delta",10}{"per-min",10}");
        Row("managed-live", first.ManagedLiveBytes, last.ManagedLiveBytes, minutes);
        Row("gc-heap", first.GcHeapBytes, last.GcHeapBytes, minutes);
        Row("working-set", first.WorkingSetBytes, last.WorkingSetBytes, minutes);
        Row("unmanaged", first.UnmanagedBytes, last.UnmanagedBytes, minutes);
        // Tail analysis is the real signal: a per-frame leak is still growing at the END of the
        // run, whereas warmup costs (JIT tiering, Metal/driver lazy init, atlas baking) step up
        // early and then plateau. Averaging start->end slope would misread a plateau as a leak,
        // so judge the verdict on growth across the final third of the run instead.
        var tailIdx = _samples.Count * 2 / 3;
        var tail = _samples[tailIdx];
        var tailMinutes = Math.Max((last.ElapsedSeconds - tail.ElapsedSeconds) / 60.0, 1.0 / 60.0);
        var tailUnmanagedPerMin = (last.UnmanagedBytes - tail.UnmanagedBytes) / tailMinutes;
        var tailManagedPerMin = (last.ManagedLiveBytes - tail.ManagedLiveBytes) / tailMinutes;
        Console.WriteLine($"  tail (last {last.ElapsedSeconds - tail.ElapsedSeconds:F0}s): " +
                          $"unmanaged {Mb((long)tailUnmanagedPerMin)}/m   managed {Mb((long)tailManagedPerMin)}/m");
        Console.WriteLine("=================================================");

        // WorkingSet64 jitters ~0.5 MB from OS paging alone, so a "leak" verdict needs a tail
        // rate WELL above that floor. A genuinely leaking run keeps climbing several MB/min at
        // the end; warmup (JIT/Metal/atlas) steps up early then the tail goes flat.
        const double leakPerMin = 2_000_000;  // 2 MB/min sustained at the tail = real
        const double noisePerMin = 500_000;   // below ~0.5 MB/min is paging jitter
        var earlyGrowth = last.UnmanagedBytes - first.UnmanagedBytes;

        if (tailUnmanagedPerMin > leakPerMin && tailUnmanagedPerMin > tailManagedPerMin * 2)
            Console.WriteLine(">> Verdict: UNMANAGED still climbing at end-of-run -> likely a real native leak. " +
                              "Diff the footprint-*/vmmap-* files to name the region.");
        else if (tailManagedPerMin > leakPerMin)
            Console.WriteLine(">> Verdict: MANAGED still climbing at end-of-run -> take two dotnet-gcdumps and diff.");
        else if (tailUnmanagedPerMin > noisePerMin && tailUnmanagedPerMin <= leakPerMin)
            Console.WriteLine(">> Verdict: small tail drift — could be a slow leak or paging noise. " +
                              "Re-run with --seconds 600 to tell them apart (noise won't accumulate).");
        else if (earlyGrowth > 2_000_000)
            Console.WriteLine(">> Verdict: grew early then PLATEAUED (flat tail) -> warmup/caches (JIT, Metal lazy init, " +
                              "atlas), not a per-frame leak.");
        else
            Console.WriteLine(">> Verdict: no significant growth in this run/scenario.");
    }

    private static void Row(string label, long start, long end, double minutes)
    {
        var perMin = (end - start) / minutes;
        Console.WriteLine($"  {label,-14}{Mb(start),10}{Mb(end),10}{Mb(end - start),10}{Mb((long)perMin) + "/m",10}");
    }

    private static string Mb(long bytes) => (bytes / (1024.0 * 1024.0)).ToString("F1", CultureInfo.InvariantCulture);
}

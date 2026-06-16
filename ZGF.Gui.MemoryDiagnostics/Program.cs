using System.Diagnostics;
using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Observable;
using ZGF.Gui.MemoryDiagnostics;

// Memory diagnostic harness. Boots the real GuiApp/Metal stack, runs a stress scenario for a
// fixed duration, samples managed-vs-unmanaged memory over time, and captures categorized
// native snapshots (footprint/vmmap) at start and end.
//
// Usage:
//   dotnet run --project ZGF.Gui.MemoryDiagnostics -- [scenario] [--seconds N] [--interval N] [--warmup N] [--out DIR]
//
//   scenario   one of: idle (default), fontsizes, textchurn, listchurn, derivedchurn
//   --seconds  total sampling duration (default 120)
//   --interval seconds between samples (default 5)
//   --warmup   seconds to settle before the baseline sample (default 5)
//   --out      output directory for CSV + native snapshots (default ./mem-diagnostics)
//
// Tip: leave it running and point Instruments (Allocations + VM Tracker, generation marks)
// or `vmmap --summary <pid>` / `leaks <pid>` at the PID it prints for a deeper drill-down.

var scenarioName = Scenarios.Names[0];
if (args.Length > 0 && !args[0].StartsWith("--"))
    scenarioName = args[0];

var durationSec = GetArg("--seconds", 120);
var intervalSec = GetArg("--interval", 5);
var warmupSec = GetArg("--warmup", 5);
var outDir = GetArgStr("--out", Path.Combine(Directory.GetCurrentDirectory(), "mem-diagnostics"));

IScenario scenario;
try
{
    scenario = Scenarios.Create(scenarioName);
}
catch (ArgumentException e)
{
    Console.WriteLine(e.Message);
    return 1;
}

var sampler = new MemorySampler(outDir);

Console.WriteLine("======== ZGF GUI memory diagnostics ========");
Console.WriteLine($"  PID:       {sampler.Pid}   <-- attach Instruments / run `vmmap --summary {sampler.Pid}` against this");
Console.WriteLine($"  scenario:  {scenario.Name}");
Console.WriteLine($"  duration:  {durationSec}s   sample every {intervalSec}s   warmup {warmupSec}s");
Console.WriteLine($"  output:    {outDir}");
Console.WriteLine("============================================");

var config = new StartupConfig
{
    WindowWidth = 1280,
    WindowHeight = 720,
    WindowTitle = $"MemDiag: {scenario.Name}",
    IsUndecorated = false,
};

var builder = GuiApp.CreateBuilder(config);
var guiApp = builder
    .UseContent(ctx => scenario.BuildRoot(ctx.Canvas))
    .Build();
var dispatcher = builder.Services.Get<IUiDispatcher>()
                 ?? throw new InvalidOperationException("IUiDispatcher not registered by GuiApp.");

// Driver runs off the UI thread: it paces scenario mutations (marshaled onto the UI thread
// via the dispatcher), samples memory, and hard-exits when the run completes. The UI/render
// loop owns the main thread via guiApp.Run() below.
var driver = new Thread(() => DriverLoop())
{
    IsBackground = true,
    Name = "mem-diagnostics-driver",
};
driver.Start();

guiApp.Run();
return 0;

void DriverLoop()
{
    try
    {
        Thread.Sleep(TimeSpan.FromSeconds(warmupSec));
        Console.WriteLine("[warmup complete] capturing baseline native snapshot...");
        sampler.CaptureNativeSnapshot("start");

        var sw = Stopwatch.StartNew();
        long frame = 0;
        var nextSampleAt = 0.0;

        while (sw.Elapsed.TotalSeconds < durationSec)
        {
            var f = frame;
            dispatcher.Post(() => scenario.Tick(f));
            frame++;

            var elapsed = sw.Elapsed.TotalSeconds;
            if (elapsed >= nextSampleAt)
            {
                var s = sampler.Capture(elapsed, frame);
                Console.WriteLine(
                    $"  t={elapsed,6:F0}s  ws={Mb(s.WorkingSetBytes),7} MB  managed={Mb(s.ManagedLiveBytes),6} MB  unmanaged={Mb(s.UnmanagedBytes),7} MB");
                nextSampleAt += intervalSec;
            }

            Thread.Sleep(16);
        }

        Console.WriteLine("[run complete] capturing end native snapshot...");
        sampler.CaptureNativeSnapshot("end");
        var csv = sampler.WriteCsv(scenario.Name);
        sampler.PrintSummary();
        Console.WriteLine();
        Console.WriteLine($"CSV written:        {csv}");
        Console.WriteLine($"Native snapshots:   {outDir}");
        Console.WriteLine($"  diff footprint-start.txt footprint-end.txt   # phys footprint by category");
        Console.WriteLine($"  diff vmmap-start.txt vmmap-end.txt           # VM regions (IOSurface / Metal / MALLOC)");
    }
    catch (Exception e)
    {
        Console.WriteLine($"[driver error] {e}");
    }
    finally
    {
        Environment.Exit(0);
    }
}

static double Mb(long bytes) => Math.Round(bytes / (1024.0 * 1024.0), 1);

int GetArg(string name, int fallback)
{
    var idx = Array.IndexOf(args, name);
    if (idx >= 0 && idx + 1 < args.Length && int.TryParse(args[idx + 1], out var v)) return v;
    return fallback;
}

string GetArgStr(string name, string fallback)
{
    var idx = Array.IndexOf(args, name);
    if (idx >= 0 && idx + 1 < args.Length) return args[idx + 1];
    return fallback;
}

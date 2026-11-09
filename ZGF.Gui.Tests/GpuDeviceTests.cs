using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit.Abstractions;
using ZGF.Geometry;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Tests;

[Collection(GpuLifecycleCollection.Name)]
public sealed class GpuDeviceTests(ITestOutputHelper output)
{
    private static void Fill(ICanvas canvas, RectF position, uint argb) =>
        canvas.DrawRect(new DrawRectInputs
        {
            Position = position,
            Style = new RectStyle { BackgroundColor = argb },
            ZIndex = 0,
        });

    private static unsafe bool HasCurrentContext() => GL46.glGetString(GL46.GL_VERSION) != null;

    [GpuFact]
    public void A_device_is_created_and_disposed_repeatedly()
    {
        for (var i = 0; i < 3; i++)
        {
            var clock = Stopwatch.StartNew();
            using var device = GpuDevice.Create();
            var created = clock.Elapsed;
            using (var session = GlCanvasSession.Create(device, 32, 32))
            {
                session.Draw(c => Fill(c, new RectF(0, 0, 32, 32), 0xFF00FF00u));
                Assert.Equal(new Rgba8(0, 255, 0, 255), session.Read()[16, 16]);
            }
            output.WriteLine($"device {i}: created in {created.TotalMilliseconds:F1} ms, {device.Description}");
        }
    }

    [GpuFact]
    public void A_second_live_OpenGL_device_is_refused_until_the_first_is_disposed()
    {
        var first = GpuDevice.Create();
        try
        {
            var refused = Assert.Throws<InvalidOperationException>(GpuDevice.Create);
            Assert.Contains("already live", refused.Message);
        }
        finally
        {
            first.Dispose();
        }

        using var second = GpuDevice.Create();
        Assert.True(second.Run(() => second.IsRenderThread));
    }

    [GpuFact]
    public void Run_executes_on_the_render_thread_and_rethrows_what_the_work_throws()
    {
        using var device = GpuDevice.Create();
        var callerThread = Environment.CurrentManagedThreadId;

        Assert.NotEqual(callerThread, device.Run(() => Environment.CurrentManagedThreadId));
        Assert.False(device.IsRenderThread);
        var thrown = Assert.Throws<FormatException>(() => device.Run(() => throw new FormatException("from the render thread")));
        Assert.Equal("from the render thread", thrown.Message);
        Assert.Equal(7, device.Run(() => device.Run(() => 7)));
    }

    [GpuFact]
    public void Disposing_from_the_render_thread_is_refused_and_leaves_the_device_working()
    {
        using var device = GpuDevice.Create();

        Assert.Throws<InvalidOperationException>(() => device.Run(device.Dispose));

        Assert.True(device.Run(HasCurrentContext));
        Assert.Throws<InvalidOperationException>(GpuDevice.Create);
        using var session = GlCanvasSession.Create(device, 8, 8);
        session.Draw(c => Fill(c, new RectF(0, 0, 8, 8), 0xFFFF0000u));
        Assert.Equal(new Rgba8(255, 0, 0, 255), session.Read()[4, 4]);
    }

    [GpuFact]
    public void Concurrent_disposes_and_runs_end_with_one_release_and_every_accepted_run_on_a_live_context()
    {
        GpuDevice? stale = null;
        for (var round = 0; round < 10; round++)
        {
            var device = GpuDevice.Create();
            stale = device;
            var failures = new ConcurrentQueue<string>();
            using var start = new Barrier(12);
            using var firstAccepted = new ManualResetEventSlim();
            var runners = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                start.SignalAndWait();
                while (true)
                {
                    try
                    {
                        if (!device.Run(HasCurrentContext))
                            failures.Enqueue("a run executed with no current context");
                        firstAccepted.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        failures.Enqueue(e.ToString());
                        return;
                    }
                }
            }));
            var disposers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            {
                start.SignalAndWait();
                if (!firstAccepted.Wait(Timeout))
                    failures.Enqueue("no run was accepted");
                device.Dispose();
            }));
            Assert.True(Task.WaitAll([.. runners, .. disposers], Timeout));

            Assert.Empty(failures);
        }

        var creations = Enumerable.Range(0, 4)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    return GpuDevice.Create();
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }))
            .ToArray();
        var created = creations.Select(t => t.Result).Where(d => d != null).ToList();
        Assert.Single(created);
        var replacement = created[0]!;
        try
        {
            stale!.Dispose();
            Assert.Throws<InvalidOperationException>(GpuDevice.Create);
            Assert.True(replacement.Run(HasCurrentContext));
        }
        finally
        {
            replacement.Dispose();
        }
        GpuDevice.Create().Dispose();
    }

    [GpuFact]
    public void A_disposed_device_refuses_work_and_its_open_sessions_are_already_released()
    {
        var device = GpuDevice.Create();
        var session = GlCanvasSession.Create(device, 8, 8);
        var target = GlOffscreenTarget.Create(device, TargetSize.Of(4, 4));
        device.Dispose();

        Assert.Throws<ObjectDisposedException>(() => device.Run(() => { }));
        Assert.Throws<ObjectDisposedException>(() => session.Draw(_ => { }));
        session.Dispose();
        target.Dispose();
    }

    [GpuFact]
    public void Every_concurrent_disposer_returns_only_after_teardown()
    {
        var device = GpuDevice.Create();
        var session = GlCanvasSession.Create(device, 8, 8);
        using var start = new Barrier(4);
        var serial = new Lock();
        var disposers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            start.SignalAndWait();
            device.Dispose();
            lock (serial)
                GpuDevice.Create().Dispose();
        })).ToArray();

        Assert.True(Task.WaitAll(disposers, Timeout));
        Assert.Throws<ObjectDisposedException>(() => session.Draw(_ => { }));
    }

    [GpuFact]
    public void A_device_shutting_down_mid_draw_finishes_the_draw_before_releasing_the_session()
    {
        var device = GpuDevice.Create();
        var session = GlCanvasSession.Create(device, 200, 40);
        using var drawing = new ManualResetEventSlim();
        using var disposing = new ManualResetEventSlim();
        var overlapped = false;
        var drawFinished = false;
        var draw = Task.Run(() => session.Draw(c =>
        {
            drawing.Set();
            overlapped = disposing.Wait(Timeout);
            c.DrawText(new DrawTextInputs
            {
                Position = new RectF(0, 0, 200, 40),
                Text = "Shaped while the device shuts down",
                Style = new TextStyle(),
                ZIndex = 0,
            });
            Volatile.Write(ref drawFinished, true);
        }));
        Assert.True(drawing.Wait(Timeout));
        var dispose = Task.Run(() =>
        {
            disposing.Set();
            device.Dispose();
            return Volatile.Read(ref drawFinished);
        });

        Assert.True(dispose.Wait(Timeout));
        Assert.True(dispose.Result, "Dispose returned before the draw it was queued behind finished");
        Assert.True(overlapped);
        Assert.True(draw.IsCompletedSuccessfully);
        session.Dispose();
    }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}

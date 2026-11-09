using System.Diagnostics;
using PngSharp.Api;
using Xunit.Abstractions;
using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Immediate.Widgets;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Immediate.Tests;

[Collection(GpuCollection.Name)]
public sealed class GpuFramesTests(GpuDeviceFixture fixture, ITestOutputHelper output)
{
    private const uint Red = 0xFFFF0000;
    private const uint Green = 0xFF00FF00;
    private const uint Blue = 0xFF0000FF;
    private static readonly Rgba8 OpaqueRed = new(255, 0, 0, 255);
    private static readonly Rgba8 OpaqueGreen = new(0, 255, 0, 255);
    private static readonly Rgba8 OpaqueBlue = new(0, 0, 255, 255);

    private GpuDevice Device => fixture.Device;

    private static IReadOnlyList<SystemFontFile> NoFallbacks() => [];

    private GpuFrames<DefaultUiState> Running(Action<Ui<DefaultUiState>> body, int width = 240, int height = 160, float dpiScale = 1f,
        Func<IReadOnlyList<SystemFontFile>>? fallbackFonts = null) =>
        GpuFrames.Running(Device, new DefaultUiState(), body, width, height, dpiScale, fallbackFonts: fallbackFonts ?? NoFallbacks, output: output.WriteLine);

    private static void Toggle(Ui<DefaultUiState> ui, ref bool on)
    {
        ui.WindowBackground = on ? Green : Red;
        if (ui.Button("Toggle", id: "toggle"))
            on = !on;
    }

    [GpuFact]
    public void A_click_through_a_selector_changes_what_is_drawn()
    {
        var on = false;
        using var frames = Running(ui => Toggle(ui, ref on));
        Assert.Equal(OpaqueRed, frames.Read()[230, 150]);

        frames.ClickOn(Selector.AtPath("toggle")).Frame();

        Assert.True(on);
        Assert.Equal(OpaqueGreen, frames.Read()[230, 150]);
    }

    /// <summary>A 1x1 blue image.</summary>
    private static readonly byte[] BluePixel = [0, 0, 255, 255];

    [GpuFact]
    public void A_frame_paints_its_boxes_and_the_images_it_was_given()
    {
        using var frames = Running(static ui =>
        {
            using (ui.Row(new Box { Gap = 0f }))
            {
                ui.Leaf(new Box { Width = 20f, Height = 20f, Background = Red });
                if (!ui.HasImage("blue"))
                    ui.SetImage("blue", 1, 1, BluePixel);
                ui.Image("blue", 20f, 20f);
            }
        }, 40, 40);

        var image = frames.Read();

        Assert.Equal(OpaqueRed, image[10, 10]);
        Assert.Equal(OpaqueBlue, image[30, 10]);
    }

    [GpuFact]
    public void Look_writes_the_snapshot_and_a_png_of_the_readback()
    {
        var on = false;
        using var frames = Running(ui => Toggle(ui, ref on));

        var files = frames.Look("gpuframes-look");

        var text = File.ReadAllText(files.Text);
        Assert.Contains("button path=\"toggle\" \"Toggle\"", text);
        var png = Png.DecodeFromFile(files.Png);
        var image = frames.Read();
        Assert.Equal((240u, 160u), (png.Ihdr.Width, png.Ihdr.Height));
        Assert.Equal(image.ToStraightAlpha(), png.PixelData);
        Assert.True(Path.IsPathFullyQualified(files.Png));
    }

    private static void PopupOverBlue(Ui<DefaultUiState> ui)
    {
        ui.WindowBackground = Blue;
        using (ui.Popup(ui.GetId("menu"), new UiRect(40f, 20f, 10f, 10f), new Box
        {
            Background = Green,
            Shadow = new UiShadow(0f, 4f, 12f, 0f, 0x80000000),
        }))
        {
            ui.Text("Popup item", new Box { Width = 120f, Height = 40f });
        }
    }

    [GpuFact]
    public void A_popup_draws_on_its_own_surface_and_over_the_window_in_the_composite_at_its_rect()
    {
        using var frames = Running(PopupOverBlue);

        var snapshot = frames.Snapshot();
        var item = snapshot.Nodes.Single(n => n.Label == "Popup item");
        Assert.Equal(SurfaceKind.Popup, item.Surface.Kind);
        var popup = item.Surface;
        var composite = frames.Read();
        var alone = frames.Read(popup);
        frames.Look("gpuframes-popup");

        Assert.Equal(new UiRect(40f, 30f, 120f, 40f), item.Bounds);
        Assert.Equal(((int)MathF.Ceiling(popup.Rect.Width), (int)MathF.Ceiling(popup.Rect.Height)), (alone.Width, alone.Height));
        Assert.Equal(OpaqueGreen, composite[45, 35]);
        Assert.Equal(OpaqueGreen, composite[155, 65]);
        Assert.Equal(OpaqueBlue, composite[5, 150]);
        Assert.Equal(OpaqueBlue, frames.Read(snapshot.Surfaces[0])[100, 50]);

        var shadow = composite[100, 74];
        Assert.True(shadow.B is > 0 and < 255 && shadow.A == 255, $"The shadow below the popup should darken the blue, not {shadow}.");
        Assert.Equal(0, shadow.R);
        Assert.Equal(0, shadow.G);
    }

    [GpuFact]
    public void A_window_dialog_draws_on_its_own_surface_centered_on_the_monitor()
    {
        var open = true;
        var oks = 0;
        WindowBounds? bounds = null;
        using var frames = Running(ui =>
        {
            ui.WindowBackground = Blue;
            using (var dialog = ui.WindowDialog("export", "Export", ref open, 160f, 140f, ref bounds))
            {
                if (dialog.IsOpen && ui.Button("OK", id: "ok"))
                    oks++;
            }
        });

        var ok = frames.Find(Selector.AtPath("ok"));
        frames.Look("gpuframes-dialog");

        Assert.Equal(SurfaceKind.Window, ok.Surface.Kind);
        Assert.Equal(new UiRect(40f, 10f, 160f, 140f), ok.Surface.Rect);
        var image = frames.Read();
        var dimmed = image[5, 5];
        Assert.True(dimmed.R == 0 && dimmed.G == 0 && dimmed.B is > 0 and < 255, $"The modal should dim the blue window, not {dimmed}.");
        Assert.Equal(frames.Read(ok.Surface)[80, 40], image[120, 50]);
        Assert.NotEqual(dimmed, image[120, 50]);
        frames.ClickOn(Selector.AtPath("ok"));
        Assert.Equal(1, oks);
    }

    [GpuFact]
    public void An_element_crop_is_its_bounds_and_margin_at_the_frames_own_scale()
    {
        var on = false;
        using var frames = Running(ui => Toggle(ui, ref on), dpiScale: 2f);
        var button = frames.Find(Selector.AtPath("toggle")).Bounds;

        var files = frames.Look("gpuframes-crop", Selector.AtPath("toggle"), margin: 4f);
        var magnified = frames.Look("gpuframes-crop-x3", Selector.AtPath("toggle"), margin: 4f, magnify: 3);

        var width = (int)MathF.Ceiling((button.Right + 4f) * 2f) - Math.Max(0, (int)MathF.Floor((button.X - 4f) * 2f));
        var height = (int)MathF.Ceiling((button.Bottom + 4f) * 2f) - Math.Max(0, (int)MathF.Floor((button.Y - 4f) * 2f));
        var png = Png.DecodeFromFile(files.Png);
        Assert.Equal(((uint)width, (uint)height), (png.Ihdr.Width, png.Ihdr.Height));
        var big = Png.DecodeFromFile(magnified.Png);
        Assert.Equal(((uint)width * 3, (uint)height * 3), (big.Ihdr.Width, big.Ihdr.Height));
        Assert.StartsWith("# path=\"toggle\"", File.ReadAllText(files.Text));
    }

    [GpuFact]
    public void A_fractional_scale_draws_at_the_rounded_pixel_size_and_still_clicks_by_selector()
    {
        var on = false;
        using var frames = Running(ui => Toggle(ui, ref on), width: 201, height: 101, dpiScale: 1.25f);

        frames.ClickOn(Selector.OfRole(SemanticRole.Button, "Toggle")).Frame();
        var image = frames.Read();
        var files = frames.Look("gpuframes-fractional");

        Assert.True(on);
        Assert.Equal((251, 126), (image.Width, image.Height));
        Assert.Equal(OpaqueGreen, image[250, 125]);
        var png = Png.DecodeFromFile(files.Png);
        Assert.Equal((251u, 126u), (png.Ihdr.Width, png.Ihdr.Height));
    }

    [GpuFact]
    public void Running_returns_only_once_a_delayed_fallback_font_load_has_been_drawn_with()
    {
        const string text = "日本 ★ ✓";
        void Body(Ui<DefaultUiState> ui)
        {
            ui.WindowBackground = 0xFF000000;
            ui.Text(text);
        }

        var finished = false;
        var clock = Stopwatch.StartNew();
        RgbaImage delayed;
        using (var frames = Running(Body, 160, 40, fallbackFonts: () =>
               {
                   Thread.Sleep(400);
                   var fonts = SystemFallbackFonts.Find();
                   finished = true;
                   return fonts;
               }))
        {
            Assert.True(finished);
            Assert.True(clock.Elapsed >= TimeSpan.FromMilliseconds(400), $"Ready after {clock.Elapsed.TotalMilliseconds} ms.");
            delayed = frames.Read();
            frames.Look("gpuframes-fallback");
        }
        RgbaImage loaded;
        using (var frames = Running(Body, 160, 40, fallbackFonts: () => SystemFallbackFonts.Find()))
            loaded = frames.Read();
        RgbaImage without;
        using (var frames = Running(Body, 160, 40))
            without = frames.Read();

        Assert.True(delayed.Premultiplied.SequenceEqual(loaded.Premultiplied));
        if (SystemFallbackFonts.Find().Any(font => FontCharacters.TryRead(File.ReadAllBytes(font.Path), font.FaceIndex)?.Contains(0x65E5) == true))
            Assert.False(delayed.Premultiplied.SequenceEqual(without.Premultiplied));
    }

    [GpuFact]
    public void Text_painted_by_custom_code_is_listed_beside_the_snapshot()
    {
        using var frames = Running(ui =>
        {
            ui.Text("Annotated");
            using (ui.Box(new Box { Width = 120f, Height = 30f }))
                ui.Paint(painter => painter.Text(painter.Rect, "Painted 42", 0xFFFFFFFF));
        });

        var text = File.ReadAllText(frames.Look("gpuframes-painted").Text);
        var drawn = frames.DrawnText();

        Assert.Contains(drawn, d => d.Text == "Painted 42");
        Assert.Contains("text \"Annotated\"", text);
        var listed = text[text.IndexOf("# drawn text no node shows:", StringComparison.Ordinal)..];
        Assert.Contains("text \"Painted 42\"", listed);
        Assert.DoesNotContain("Annotated", listed);
    }

    [GpuFact]
    public void A_target_that_is_not_there_fails_with_a_look_of_that_moment()
    {
        var on = false;
        using var frames = Running(ui => Toggle(ui, ref on));

        var failure = Assert.Throws<UiTargetException>(() => frames.ClickOn(Selector.AtPath("missing")));

        Assert.IsType<TargetResult.NotFound>(failure.Result);
        var png = failure.Message.Split('\n').Single(line => line.EndsWith(".png", StringComparison.Ordinal)).Trim();
        Assert.True(File.Exists(png), png);
        Assert.True(File.Exists(Path.ChangeExtension(png, ".txt")));
    }

    [GpuFact]
    public void A_look_at_another_scale_is_named_for_it_and_leaves_the_frame_at_its_own()
    {
        var on = false;
        using var frames = Running(ui => Toggle(ui, ref on));
        var before = frames.Read();

        var files = frames.LookAtScale("gpuframes-rescaled", 2f);

        Assert.Matches(@"gpuframes-rescaled@2x-frame\d+\.png$", files.Png);
        var png = Png.DecodeFromFile(files.Png);
        Assert.Equal((480u, 320u), (png.Ihdr.Width, png.Ihdr.Height));
        Assert.True(before.Premultiplied.SequenceEqual(frames.Read().Premultiplied));
    }

    private static void Filled(Ui<DefaultUiState> ui, uint color, string text)
    {
        using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Background = color }))
            ui.Text(text);
    }

    [GpuFact]
    public void Running_does_not_advance_time_and_says_what_still_animates()
    {
        using var frames = Running(ui =>
        {
            ui.WindowBackground = Blue;
            ui.Text(ui.Time < 0.5 ? "Toast" : "Gone");
            ui.RequestAnimationFrame();
        });

        var look = File.ReadAllText(frames.Look("gpuframes-animating").Text);

        Assert.Equal(0.0, frames.RealTime);
        Assert.Equal(FramePending.Animation, frames.Animating & FramePending.Animation);
        Assert.Contains(frames.DrawnText(), d => d.Text == "Toast");
        Assert.Contains("# still animating: an animation asks for the next frame", look);
    }

    [GpuFact]
    public void Running_runs_a_long_chain_of_required_updates_and_a_deferred_focus_move_to_their_end()
    {
        var left = 50;
        var moved = false;
        var name = "";
        using var frames = Running(ui =>
        {
            if (left-- > 0)
                ui.RequestUpdate();
            ui.TextField("name", ref name);
            if (!moved)
                ui.MoveFocus(1);
            moved = true;
        });

        Assert.True(left < 0);
        Assert.True(frames.WantsTextInput);
        Assert.Equal(FramePending.None, frames.Animating);
    }

    [GpuFact]
    public void Running_does_not_wait_for_a_tween_that_needs_time()
    {
        var first = true;
        var value = 0f;
        using var frames = Running(ui =>
        {
            value = ui.Animate("v", first ? 0f : 1f, Tween.Linear(1000f));
            if (first)
                ui.RequestUpdate();
            first = false;
        });

        Assert.Equal(0f, value);
        Assert.Equal(FramePending.Animation, frames.Animating & FramePending.Animation);
    }

    [GpuFact]
    public void A_centered_window_stays_put_from_its_first_frame_to_its_second_at_a_fractional_scale()
    {
        var open = true;
        WindowBounds? bounds = null;
        using var frames = Running(ui =>
        {
            using (ui.Window(ui.GetId("centered"), "Centered", ref open, 100.8f, 50.8f, ref bounds, out _))
                ui.Text("centered");
        }, dpiScale: 1.25f);
        var first = frames.Find(Selector.WithText("centered")).Surface.Rect;

        frames.Frame();
        var second = frames.Find(Selector.WithText("centered")).Surface.Rect;
        frames.Frame();

        Assert.Equal(first, second);
        Assert.Equal(second, frames.Find(Selector.WithText("centered")).Surface.Rect);
        Assert.Equal((int)MathF.Round(first.X), bounds!.Value.X);
    }

    [GpuFact]
    public void A_surface_of_a_window_since_replaced_at_the_same_place_is_refused()
    {
        var showA = true;
        bool openA = true, openB = true;
        WindowBounds? boundsA = null, boundsB = null;
        using var frames = Running(ui =>
        {
            if (showA)
                using (ui.Window(ui.GetId("a"), "A", ref openA, 120f, 80f, ref boundsA, out _))
                    Filled(ui, Red, "a");
            else
                using (ui.Window(ui.GetId("b"), "B", ref openB, 120f, 80f, ref boundsB, out _))
                    Filled(ui, Green, "b");
        });
        var a = frames.Find(Selector.WithText("a")).Surface;

        showA = false;
        frames.Frame();
        var b = frames.Find(Selector.WithText("b")).Surface;

        Assert.Equal(a.Rect, b.Rect);
        Assert.Equal(a.Index, b.Index);
        Assert.NotEqual(a, b);
        Assert.Contains("replaced", Assert.Throws<ArgumentException>(() => frames.Read(a)).Message);
        Assert.Equal(OpaqueGreen, frames.Read(b)[5, 5]);
    }

    [GpuFact]
    public void A_surface_from_another_harness_is_refused()
    {
        using var one = Running(ui => ui.Text("one"));
        using var other = Running(ui => ui.Text("other"));
        var main = one.Snapshot().Surfaces[0];
        other.Snapshot();

        Assert.Contains("another harness", Assert.Throws<ArgumentException>(() => other.Read(main)).Message);
    }

    [GpuFact]
    public void Readiness_waits_off_the_render_thread_so_tracked_work_can_use_the_device()
    {
        var background = new BackgroundWork();
        var ranOnDevice = false;
        background.Track("device work", Task.Run(() =>
        {
            Thread.Sleep(50);
            Device.Run(() => ranOnDevice = Device.IsRenderThread);
        }));

        using var frames = GpuFrames.Running(Device, new DefaultUiState(), _ => { }, 40, 40, background: background,
            fallbackFonts: NoFallbacks, readyTimeout: TimeSpan.FromSeconds(5));

        Assert.True(ranOnDevice);
    }

    [GpuFact]
    public void Two_windows_of_the_same_size_at_the_same_place_each_read_back_their_own_pixels()
    {
        bool red = true, green = true;
        WindowBounds? redBounds = null, greenBounds = null;
        using var frames = Running(ui =>
        {
            using (ui.Window(ui.GetId("red"), "Red", ref red, 120f, 80f, ref redBounds, out _))
                Filled(ui, Red, "red window");
            using (ui.Window(ui.GetId("green"), "Green", ref green, 120f, 80f, ref greenBounds, out _))
                Filled(ui, Green, "green window");
        });

        var redSurface = frames.Find(Selector.WithText("red window")).Surface;
        var greenSurface = frames.Find(Selector.WithText("green window")).Surface;

        Assert.Equal(redSurface.Rect, greenSurface.Rect);
        Assert.Equal(OpaqueRed, frames.Read(redSurface)[5, 5]);
        Assert.Equal(OpaqueGreen, frames.Read(greenSurface)[5, 5]);
    }

    [GpuFact]
    public void A_file_picker_opened_from_a_window_hides_popups_but_keeps_the_window()
    {
        var open = true;
        WindowBounds? bounds = null;
        var picked = 0;
        using var frames = Running(ui =>
        {
            ui.WindowBackground = Blue;
            using (ui.Window(ui.GetId("dialog"), "Dialog", ref open, 160f, 100f, ref bounds, out _))
            using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Background = Green }))
            {
                if (ui.Button("Browse", id: "browse"))
                    ui.PickFile("Open", null, null, _ => picked++);
            }
        });
        frames.FilePicker.Answer = "notes.txt";

        frames.ClickOn(Selector.AtPath("browse"));
        var surface = frames.Find(Selector.AtPath("browse")).Surface;
        var alone = frames.Read(surface);
        var composite = frames.Read();
        frames.Frame();

        Assert.Equal(["Open"], frames.FilePicker.Titles);
        Assert.Equal(1, picked);
        Assert.Equal(OpaqueGreen, alone[alone.Width - 3, alone.Height - 3]);
        Assert.Equal(OpaqueGreen, composite[(int)surface.Rect.Right - 3, (int)surface.Rect.Bottom - 3]);
    }

    [GpuFact]
    public void A_window_of_fractional_size_lays_out_on_a_whole_point_canvas_at_a_fractional_scale()
    {
        var open = true;
        WindowBounds? bounds = null;
        using var frames = Running(ui =>
        {
            ui.WindowBackground = Blue;
            using (ui.Window(ui.GetId("odd"), "Odd", ref open, 100.8f, 50.8f, ref bounds, out _))
                Filled(ui, Green, "odd");
        }, dpiScale: 1.25f);

        var surface = frames.Find(Selector.WithText("odd")).Surface;
        var window = frames.Snapshot().Nodes.Single(n => n.Role == SemanticRole.SubWindow);
        var image = frames.Read(surface);

        Assert.Equal(new UiRect(surface.Rect.X, surface.Rect.Y, 101f, 51f), surface.Rect);
        Assert.Equal(surface.Rect, window.Bounds);
        Assert.Equal(((int)MathF.Round(101f * 1.25f), (int)MathF.Round(51f * 1.25f)), (image.Width, image.Height));
        Assert.Equal(OpaqueGreen, image[0, 0]);
        Assert.Equal(OpaqueGreen, image[image.Width - 1, image.Height - 1]);
    }

    [GpuFact]
    public void A_crop_of_an_element_on_a_window_partly_outside_the_main_one_is_cut_from_the_windows_own_pixels()
    {
        var open = true;
        WindowBounds? bounds = new WindowBounds(-60, 20, 160, 100, false);
        using var frames = Running(ui =>
        {
            ui.WindowBackground = Blue;
            using (ui.Window(ui.GetId("aside"), "Aside", ref open, 160f, 100f, ref bounds, out _))
            using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Background = Green, Padding = 10f }))
                ui.Button("Edge", id: "edge");
        });

        var edge = frames.Find(Selector.AtPath("edge"));
        var files = frames.Look("gpuframes-crop-aside", Selector.AtPath("edge"), margin: 2f);

        Assert.True(edge.Bounds.X < 0, $"The button should start left of the main window, at {edge.Bounds}.");
        var window = frames.Read(edge.Surface);
        var x = edge.Bounds.X - edge.Surface.Rect.X;
        var y = edge.Bounds.Y - edge.Surface.Rect.Y;
        var left = (int)MathF.Floor(x - 2f);
        var top = (int)MathF.Floor(y - 2f);
        var expected = window.Crop(left, top, (int)MathF.Ceiling(x + edge.Bounds.Width + 2f) - left, (int)MathF.Ceiling(y + edge.Bounds.Height + 2f) - top);
        var png = Png.DecodeFromFile(files.Png);
        Assert.Equal(((uint)expected.Width, (uint)expected.Height), (png.Ihdr.Width, png.Ihdr.Height));
        Assert.Equal(expected.ToStraightAlpha(), png.PixelData);
        Assert.Equal(OpaqueGreen, expected[0, 0]);
    }

    [GpuFact]
    public void A_window_closed_and_reopened_with_new_bounds_opens_at_them()
    {
        var open = true;
        WindowBounds? bounds = new WindowBounds(10, 10, 80, 60, false);
        using var frames = Running(ui =>
        {
            if (open)
                using (ui.Window(ui.GetId("w"), "W", ref open, 80f, 60f, ref bounds, out _))
                    ui.Text("inside");
        });
        Assert.Equal(new UiRect(10f, 10f, 80f, 60f), frames.Find(Selector.WithText("inside")).Surface.Rect);

        open = false;
        frames.Frame();
        bounds = new WindowBounds(100, 50, 80, 60, false);
        open = true;
        frames.Frame();

        Assert.Equal(new UiRect(100f, 50f, 80f, 60f), frames.Find(Selector.WithText("inside")).Surface.Rect);
    }

    [GpuFact]
    public void Painted_text_is_listed_though_a_label_elsewhere_holds_it()
    {
        using var frames = Running(ui =>
        {
            ui.Text("Total 42");
            using (ui.Box(new Box { Width = 120f, Height = 30f }))
                ui.Paint(painter => painter.Text(painter.Rect, "42", 0xFFFFFFFF));
        });

        var text = File.ReadAllText(frames.Look("gpuframes-dedup").Text);

        var listed = text[text.IndexOf("# drawn text no node shows:", StringComparison.Ordinal)..];
        Assert.Contains("text \"42\"", listed);
        Assert.DoesNotContain("Total 42", listed);
    }

    [GpuFact]
    public void A_closed_popups_surface_is_disposed_on_the_render_thread()
    {
        var show = true;
        using var frames = Running(ui =>
        {
            if (show)
                using (ui.Popup(ui.GetId("p"), new UiRect(10f, 10f, 10f, 10f), new Box { Background = Green }))
                    ui.Text("pop", new Box { Width = 40f, Height = 20f });
        });
        var surface = frames.Find(Selector.WithText("pop")).Surface;
        var released = new List<bool>();
        frames.SurfaceReleased = () => released.Add(Device.IsRenderThread);

        show = false;
        frames.Frame();

        Assert.Equal([true], released);
        Assert.Throws<ArgumentException>(() => frames.Read(surface));
    }
}

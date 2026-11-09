using System.Globalization;
using System.Runtime.CompilerServices;
using PngSharp.Api;
using Xunit.Abstractions;
using ZGF.Geometry;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Tests;

/// <summary>
/// Every canvas primitive draws as the recorded baseline for this backend did, each channel of each pixel within
/// <see cref="Tolerance"/>. Each scene is drawn over an opaque background at 1x and at a fractional scale. Drivers
/// differ in their derivatives, so each backend's baselines hold for the driver they were recorded on: wgl on an
/// NVIDIA GPU, egl on Mesa's llvmpipe as CI runs it. Run with <c>ZGF_RECORD_SHADER_BASELINES=1</c> to record the
/// baselines for this backend; the run then fails, so a recording is never mistaken for a pass.
/// </summary>
[Collection(GpuCollection.Name)]
public sealed class ShaderBaselineTests(GpuDeviceFixture fixture, ITestOutputHelper output)
{
    private const int Tolerance = 2;
    private const int Width = 160;
    private const int Height = 120;
    private const uint Background = 0xFF203040u;
    private const string ImageId = "baseline-checker";

    public static TheoryData<string, float> Scenes()
    {
        var data = new TheoryData<string, float>();
        foreach (var scene in SceneNames)
        foreach (var scale in new[] { 1f, 1.5f })
            data.Add(scene, scale);
        return data;
    }

    private static readonly string[] SceneNames = ["rects", "text", "images", "shadows", "shapes", "rounded-clips"];

    [GpuTheory]
    [MemberData(nameof(Scenes))]
    public void A_scene_draws_as_its_baseline(string scene, float scale)
    {
        var device = fixture.Device;
        using var session = device.CreateSession(Width, Height, scale);
        device.Run(() => session.Canvas.CreateOrUpdateRgbaImage(ImageId, 4, 4, Checker()));
        session.Draw(canvas => Draw(canvas, scene), Background);
        var actual = session.Read();

        var name = string.Create(CultureInfo.InvariantCulture, $"{scene}@{scale:0.0#}x");
        var path = Path.Combine(BaselineDirectory(device.Backend), name + ".png");
        if (Environment.GetEnvironmentVariable("ZGF_RECORD_SHADER_BASELINES") == "1")
        {
            actual.SavePng(path);
            Assert.Fail($"Recorded {path}.");
        }

        Assert.True(File.Exists(path), $"No baseline at {path}; record it with ZGF_RECORD_SHADER_BASELINES=1.");
        var baseline = Png.DecodeFromFile(path);
        Assert.Equal(((uint)actual.Width, (uint)actual.Height), (baseline.Ihdr.Width, baseline.Ihdr.Height));

        var worst = 0;
        var over = 0;
        var expected = baseline.PixelData;
        var straight = actual.ToStraightAlpha();
        for (var i = 0; i < straight.Length; i++)
        {
            var diff = Math.Abs(straight[i] - expected[i]);
            worst = Math.Max(worst, diff);
            if (diff > Tolerance) over++;
        }

        if (over == 0) return;
        var actualPath = Path.Combine(Path.GetTempPath(), "zgf-shader-baselines", device.Backend.ToString(), name + ".png");
        actual.SavePng(actualPath);
        output.WriteLine(actualPath);
        Assert.Fail($"{name}: {over} channel(s) differ from {path} by more than {Tolerance}, at worst {worst}; drawn: {actualPath}. " +
                    $"The device is {device.Description}.");
    }

    private static string BaselineDirectory(GpuBackend backend, [CallerFilePath] string source = "") =>
        Path.Combine(Path.GetDirectoryName(source) ?? throw new InvalidOperationException($"{source} has no directory."), "Baselines", "Shaders", backend.ToString().ToLowerInvariant());

    /// <summary>A 4x4 image: red and white squares, the top-left one red, with a translucent blue one in the
    /// bottom-right corner.</summary>
    private static byte[] Checker()
    {
        var pixels = new byte[4 * 4 * 4];
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++)
        {
            var i = (y * 4 + x) * 4;
            var red = (x + y) % 2 == 0;
            pixels[i] = 255;
            pixels[i + 1] = red ? (byte)0 : (byte)255;
            pixels[i + 2] = red ? (byte)0 : (byte)255;
            pixels[i + 3] = 255;
        }
        var corner = (3 * 4 + 3) * 4;
        pixels[corner] = 0;
        pixels[corner + 1] = 0;
        pixels[corner + 2] = 255;
        pixels[corner + 3] = 128;
        return pixels;
    }

    private static void Draw(RenderedCanvasBase canvas, string scene)
    {
        switch (scene)
        {
            case "rects": Rects(canvas); break;
            case "text": Text(canvas); break;
            case "images": Images(canvas); break;
            case "shadows": Shadows(canvas); break;
            case "shapes": Shapes(canvas); break;
            case "rounded-clips": RoundedClips(canvas); break;
            default: throw new ArgumentOutOfRangeException(nameof(scene), scene, "No such scene.");
        }
    }

    private static void Rects(RenderedCanvasBase canvas)
    {
        Rect(canvas, new RectF(4, 84, 30, 30), new RectStyle { BackgroundColor = 0xFFE0A030u });
        Rect(canvas, new RectF(40, 84, 40, 30), new RectStyle
        {
            BackgroundColor = 0xFF3080E0u,
            BorderRadius = BorderRadiusStyle.All(10f),
            BorderSize = BorderSizeStyle.All(2f),
            BorderColor = BorderColorStyle.All(0xFFFFFFFFu),
        });
        Rect(canvas, new RectF(86, 84, 40, 30), new RectStyle
        {
            BackgroundColor = 0xFF40C060u,
            BorderSize = new BorderSizeStyle { Left = 1f, Right = 3f, Top = 4f, Bottom = 0f },
            BorderColor = new BorderColorStyle { Left = 0xFFFF0000u, Right = 0xFF00FF00u, Top = 0xFF0000FFu, Bottom = 0xFFFFFF00u },
        });
        Rect(canvas, new RectF(4, 44, 50, 34), new RectStyle
        {
            BackgroundColor = 0xFFC040C0u,
            BorderRadius = new BorderRadiusStyle { TopLeft = 2f, TopRight = 14f, BottomRight = 0f, BottomLeft = 8f },
            BorderSize = BorderSizeStyle.All(3f),
            BorderColor = BorderColorStyle.All(0x00000000u),
        });
        Rect(canvas, new RectF(60, 44, 40, 34), new RectStyle
        {
            BackgroundColor = 0x80FFFFFFu,
            BorderRadius = BorderRadiusStyle.All(6f),
            BorderSize = BorderSizeStyle.All(8f),
            BorderColor = BorderColorStyle.All(0xFF000000u),
        });
        canvas.PushScale(1.13f, 1.07f, 130f, 20f);
        Rect(canvas, new RectF(106, 6, 40, 30), new RectStyle
        {
            BackgroundColor = 0xFFFFFFFFu,
            BorderSize = BorderSizeStyle.All(1.5f),
            BorderColor = BorderColorStyle.All(0xFF800000u),
        });
        Rect(canvas, new RectF(106, 44, 40, 30), new RectStyle
        {
            BackgroundColor = 0xFF00A0A0u,
            BorderRadius = BorderRadiusStyle.All(9f),
            BorderSize = BorderSizeStyle.All(1f),
            BorderColor = BorderColorStyle.All(0xFFFFFF00u),
        });
        canvas.PopScale();
        Rect(canvas, new RectF(4, 4, 96, 34), new RectStyle
        {
            BackgroundColor = 0xFF000000u,
            BorderRadius = BorderRadiusStyle.All(17f),
        });
    }

    private static void Rect(RenderedCanvasBase canvas, RectF position, RectStyle style) =>
        canvas.DrawRect(new DrawRectInputs { Position = position, Style = style, ZIndex = 0 });

    private static void Text(RenderedCanvasBase canvas)
    {
        canvas.DrawText(new DrawTextInputs
        {
            Position = new RectF(4, 90, 152, 26),
            Text = "Shader Ag 0123",
            Style = new TextStyle { TextColor = 0xFFFFFFFFu, FontSize = 18f },
            ZIndex = 0,
        });
        canvas.DrawText(new DrawTextInputs
        {
            Position = new RectF(4, 60, 152, 20),
            Text = "translucent, smaller",
            Style = new TextStyle { TextColor = 0xA0FFC040u, FontSize = 11f },
            ZIndex = 0,
        });
        canvas.DrawText(new DrawTextInputs
        {
            Position = new RectF(20, 10, 120, 40),
            Text = "Rotated",
            Style = new TextStyle { TextColor = 0xFF80FF80u, FontSize = 16f, Rotation = 0.35f },
            ZIndex = 0,
        });
    }

    private static void Images(RenderedCanvasBase canvas)
    {
        Image(canvas, new RectF(4, 76, 40, 40), 0xFFFFFFFFu, 0f, default);
        Image(canvas, new RectF(50, 76, 40, 40), 0xFF80FF80u, 0f, new System.Numerics.Vector4(12f, 4f, 0f, 20f));
        Image(canvas, new RectF(100, 76, 40, 40), 0xC0FFFFFFu, 0.4f, default);
        canvas.PushScale(1.11f, 1.11f, 0f, 0f);
        Image(canvas, new RectF(4.3f, 10.2f, 37f, 33f), 0xFFFFFFFFu, 0f, default);
        Image(canvas, new RectF(50.6f, 10.7f, 37f, 33f), 0xFFFFFFFFu, 0f, new System.Numerics.Vector4(8f));
        canvas.PopScale();
    }

    private static void Image(RenderedCanvasBase canvas, RectF position, uint tint, float rotation, System.Numerics.Vector4 radius) =>
        canvas.DrawImage(new DrawImageInputs
        {
            Position = position,
            ImageId = ImageId,
            TintColor = tint,
            Rotation = rotation,
            CornerRadius = radius,
            ZIndex = 0,
        });

    private static void Shadows(RenderedCanvasBase canvas)
    {
        Shadow(canvas, new RectF(14, 70, 40, 30), BorderRadiusStyle.All(0f), new BoxShadowStyle { Blur = 8f, Color = 0xFF000000u });
        Shadow(canvas, new RectF(80, 70, 50, 30), BorderRadiusStyle.All(10f),
            new BoxShadowStyle { Blur = 12f, OffsetX = 4f, OffsetY = -6f, Spread = 2f, Color = 0xC0FF8000u });
        Shadow(canvas, new RectF(20, 14, 60, 30), new BorderRadiusStyle { TopLeft = 16f, BottomRight = 4f },
            new BoxShadowStyle { Blur = 3f, Spread = -2f, Color = 0x8000FFFFu });
        Shadow(canvas, new RectF(104, 14, 40, 30), BorderRadiusStyle.All(6f), new BoxShadowStyle { Blur = 0.5f, Color = 0xFFFFFFFFu });
    }

    private static void Shadow(RenderedCanvasBase canvas, RectF position, BorderRadiusStyle radius, BoxShadowStyle shadow) =>
        canvas.DrawBoxShadow(new DrawBoxShadowInputs { Position = position, BorderRadius = radius, Shadow = shadow, ZIndex = 0 });

    private static void Shapes(RenderedCanvasBase canvas)
    {
        canvas.DrawCircle(new DrawCircleInputs { Center = new PointF(18, 100), Radius = 12f, Color = 0xFFFF4040u, ZIndex = 0 });
        canvas.DrawCircle(new DrawCircleInputs { Center = new PointF(50, 100), Radius = 12.5f, Thickness = 3f, Color = 0xFF40FF40u, ZIndex = 0 });
        canvas.DrawCircle(new DrawCircleInputs { Center = new PointF(80.5f, 100.3f), Radius = 6.3f, Color = 0x80FFFFFFu, ZIndex = 0 });
        Line(canvas, new PointF(100, 112), new PointF(154, 88), LineCap.Round, 0, 0, null);
        Line(canvas, new PointF(8, 76), new PointF(70, 76), LineCap.Butt, 0, 0, null);
        Line(canvas, new PointF(8, 66), new PointF(70, 66), LineCap.Square, 0, 0, null);
        Line(canvas, new PointF(8, 56), new PointF(150, 70), LineCap.Butt, 6f, 4f, null);
        Line(canvas, new PointF(80, 60), new PointF(150, 40), LineCap.Round, 0, 0, 0xFF0080FFu);
        canvas.DrawBezier(new DrawBezierInputs
        {
            Start = new PointF(8, 8), Control = new PointF(40, 60), End = new PointF(72, 8),
            Thickness = 3f, Color = 0xFFFFFF00u, ZIndex = 0,
        });
        canvas.DrawBezier(new DrawBezierInputs
        {
            Start = new PointF(80, 8), Control = new PointF(110, 50), End = new PointF(154, 12),
            Thickness = 2.5f, Color = 0xFFFF00FFu, GradientEndColor = 0xFF00FFFFu, DashLength = 5f, GapLength = 3f, ZIndex = 0,
        });
        canvas.DrawBezier(new DrawBezierInputs
        {
            Start = new PointF(8, 40), Control = new PointF(30, 40), End = new PointF(60, 40),
            Thickness = 2f, Color = 0xFFFFFFFFu, ZIndex = 0,
        });
    }

    private static void Line(RenderedCanvasBase canvas, PointF start, PointF end, LineCap cap, float dash, float gap, uint? gradientEnd) =>
        canvas.DrawLine(new DrawLineInputs
        {
            Start = start, End = end, Thickness = 5f, Color = 0xFFFFA000u, Cap = cap,
            DashLength = dash, GapLength = gap, GradientEndColor = gradientEnd, ZIndex = 0,
        });

    private static void RoundedClips(RenderedCanvasBase canvas)
    {
        canvas.PushClip(new RectF(4, 4, 152, 112), 24f);
        Rect(canvas, new RectF(0, 0, Width, Height), new RectStyle { BackgroundColor = 0xFFE0E0E0u });
        Shadow(canvas, new RectF(10, 80, 60, 30), BorderRadiusStyle.All(4f), new BoxShadowStyle { Blur = 10f, Color = 0xFF000000u });
        Image(canvas, new RectF(100, 70, 60, 50), 0xFFFFFFFFu, 0f, default);
        canvas.DrawCircle(new DrawCircleInputs { Center = new PointF(150, 10), Radius = 20f, Color = 0xFF0000FFu, ZIndex = 0 });
        canvas.DrawText(new DrawTextInputs
        {
            Position = new RectF(-6, 2, 120, 30),
            Text = "Clipped text",
            Style = new TextStyle { TextColor = 0xFF000000u, FontSize = 20f },
            ZIndex = 0,
        });
        canvas.PushClip(new RectF(60, 40, 40, 30), 15f);
        Rect(canvas, new RectF(50, 30, 60, 50), new RectStyle { BackgroundColor = 0xFFFF0000u });
        canvas.PopClip();
        canvas.PopClip();
    }
}

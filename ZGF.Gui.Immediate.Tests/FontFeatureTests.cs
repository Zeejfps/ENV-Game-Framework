using Xunit.Abstractions;
using ZGF.Fonts;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>Text shaped with OpenType features, through the shipping canvas and its default font (Inter, whose digits
/// are proportional unless tabular figures are asked for).</summary>
[Collection(GpuCollection.Name)]
public sealed class FontFeatureTests(GpuDeviceFixture fixture, ITestOutputHelper output)
{
    private static IReadOnlyList<SystemFontFile> NoFallbacks() => [];

    private (float One, float Eight) MeasureDigits(FontFeatureSet features)
    {
        var widths = (One: 0f, Eight: 0f);
        using var frames = GpuFrames.Running(fixture.Device, new DefaultUiState(), ui =>
        {
            widths = (ui.MeasureText("1111", features: features), ui.MeasureText("8888", features: features));
            ui.Label("1111", features: features);
        }, 240, 80, fallbackFonts: NoFallbacks, output: output.WriteLine);
        return widths;
    }

    [GpuFact]
    public void Tabular_figures_make_every_digit_as_wide()
    {
        var tabular = MeasureDigits(FontFeatureSet.TabularFigures);

        Assert.Equal(tabular.Eight, tabular.One);
    }

    [GpuFact]
    public void Without_features_the_default_font_keeps_its_proportional_digits()
    {
        var proportional = MeasureDigits(FontFeatureSet.None);

        Assert.True(proportional.One < proportional.Eight, $"1111 is {proportional.One} wide, 8888 {proportional.Eight}");
    }
}

using ZGF.Gui;
using ZGF.Gui.Mobile.Controls;
using ZGF.Gui.Views;

namespace ZGF.Gui.iOS.Espresso;

/// <summary>
/// The espresso dial-in screen: three sliders (Dose / Yield / TDS) drive real brewing math and a
/// live point on the <see cref="ChartView"/>, with a one-line taste verdict underneath. Owns its
/// own view tree and shot state; <see cref="MetalViewController"/> just mounts <see cref="Root"/>
/// and runs the render loop.
///
/// Math: Extraction Yield % = (yield x TDS) / dose, Brew Ratio = yield / dose. The point is
/// (EY, TDS); where it lands relative to the ideal zone is the taste estimate.
/// </summary>
public sealed class EspressoDialingScreen
{
    private const uint ScreenColor = 0xFF0C0F16;
    private const uint TitleColor = 0xFFF2F5FB;
    private const uint LabelColor = 0xFFB9C2D6;
    private const uint ValueColor = 0xFFF2F5FB;
    private const uint MutedColor = 0xFF8A93A8;

    private const uint Balanced = 0xFF55E08A;
    private const uint Caution = 0xFFE0B33B;
    private const uint Bad = 0xFFE0563B;

    private float _dose = 18f;
    private float _yield = 36f;
    private float _tds = 9f;

    private readonly ChartView _chart = new();
    private TextView _verdict = null!;
    private TextView _readout = null!;
    private TextView _doseValue = null!;
    private TextView _yieldValue = null!;
    private TextView _tdsValue = null!;

    public MultiChildView Root { get; }

    public EspressoDialingScreen(int width, int height, Context context)
    {
        Root = Build(width, height, context);
        Recompute();
    }

    public void Resize(int width, int height)
    {
        Root.Width = width;
        Root.Height = height;
    }

    private MultiChildView Build(int width, int height, Context context)
    {
        var title = new TextView
        {
            Text = "Espresso Dial-In",
            TextColor = TitleColor,
            FontSize = 22f,
            HorizontalTextAlignment = TextAlignment.Start,
        };

        _verdict = new TextView
        {
            Text = "",
            TextColor = TitleColor,
            FontSize = 15f,
            TextWrap = TextWrap.Wrap,
            HorizontalTextAlignment = TextAlignment.Start,
        };

        _readout = new TextView
        {
            Text = "",
            TextColor = MutedColor,
            FontSize = 13f,
            HorizontalTextAlignment = TextAlignment.Start,
        };

        var doseSlider = new SliderView { Min = 14f, Max = 22f, Value = _dose };
        doseSlider.ValueChanged += v => { _dose = v; Recompute(); };
        _doseValue = MakeValueLabel();

        var yieldSlider = new SliderView { Min = 20f, Max = 60f, Value = _yield };
        yieldSlider.ValueChanged += v => { _yield = v; Recompute(); };
        _yieldValue = MakeValueLabel();

        var tdsSlider = new SliderView { Min = 6f, Max = 14f, Value = _tds };
        tdsSlider.ValueChanged += v => { _tds = v; Recompute(); };
        _tdsValue = MakeValueLabel();

        var column = new FlexColumnView
        {
            Gap = 12f,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                title,
                new FlexItem { Grow = 1f, Child = _chart },
                _verdict,
                _readout,
                SliderRow("Dose", doseSlider, _doseValue),
                SliderRow("Yield", yieldSlider, _yieldValue),
                SliderRow("TDS", tdsSlider, _tdsValue),
            },
        };

        var panel = new RectView
        {
            BackgroundColor = ScreenColor,
            Padding = new PaddingStyle { Left = 20, Right = 20, Top = 64, Bottom = 30 },
            Children = { column },
        };

        return new MultiChildView
        {
            Width = width,
            Height = height,
            Context = context,
            Children = { panel },
        };
    }

    private static TextView MakeValueLabel() => new()
    {
        Text = "",
        Width = 74f,
        TextColor = ValueColor,
        FontSize = 14f,
        HorizontalTextAlignment = TextAlignment.End,
        VerticalTextAlignment = TextAlignment.Center,
    };

    private static FlexRowView SliderRow(string label, SliderView slider, TextView valueLabel)
    {
        return new FlexRowView
        {
            Gap = 12f,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new TextView
                {
                    Text = label,
                    Width = 56f,
                    TextColor = LabelColor,
                    FontSize = 14f,
                    VerticalTextAlignment = TextAlignment.Center,
                },
                new FlexItem { Grow = 1f, Child = slider },
                valueLabel,
            },
        };
    }

    private void Recompute()
    {
        var ey = _dose > 0f ? _yield * _tds / _dose : 0f;
        var ratio = _dose > 0f ? _yield / _dose : 0f;

        var color = Classify(ey, _tds, out var verdict);

        _chart.SetShot(ey, _tds, color);
        _verdict.Text = verdict;
        _readout.Text = $"EY {ey:0.0}%   •   Ratio 1:{ratio:0.0}   •   {_yield:0} g out / {_dose:0.0} g in";
        _doseValue.Text = $"{_dose:0.0} g";
        _yieldValue.Text = $"{_yield:0} g";
        _tdsValue.Text = $"{_tds:0.0}%";
    }

    private static uint Classify(float ey, float tds, out string verdict)
    {
        var extOk = ey >= ChartView.IdealEyMin && ey <= ChartView.IdealEyMax;
        var bodyOk = tds >= ChartView.IdealTdsMin && tds <= ChartView.IdealTdsMax;

        if (extOk && bodyOk)
        {
            verdict = "Balanced — sweet, even, classic espresso.";
            return Balanced;
        }

        var ext = ey < ChartView.IdealEyMin
            ? "Under-extracted — sour & sharp"
            : ey > ChartView.IdealEyMax
                ? "Over-extracted — bitter & dry"
                : "Well-extracted";

        var body = tds < ChartView.IdealTdsMin
            ? "thin, watery body"
            : tds > ChartView.IdealTdsMax
                ? "intense, syrupy body"
                : "full body";

        verdict = $"{ext}; {body}.";
        return extOk || bodyOk ? Caution : Bad;
    }
}

using System;
using System.Collections.Generic;
using ZGF.Gui.Mobile.Controllers;
using ZGF.Gui.Mobile.Controls;
using ZGF.Gui.Views;

namespace ZGF.Gui.iOS.Espresso;

/// <summary>
/// The espresso dial-in screen — a refractometer-free coach. You set Dose, Yield and the measured
/// Shot Time, and optionally tap how it tasted; it estimates extraction from the flow rate, plots
/// an estimated point on the <see cref="ChartView"/>, and tells you what to change next.
///
/// Model: ratio = yield / dose. Flow rate = yield / time is the proxy for grind/resistance, so
/// extraction is estimated from it (faster flow → coarser/under, slower → finer/over). Strength
/// then follows exactly: TDS = EY / ratio — which is why the point rides the brew-ratio diagonal as
/// time changes. A taste tap (sour/bitter) nudges the estimate and overrides the coaching advice,
/// since taste is the real ground truth.
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

    private const uint TasteIdleBg = 0xFF222A38;
    private const uint TasteIdleText = 0xFFB9C2D6;
    private const uint TasteSelText = 0xFF0C0F16;

    // A balanced espresso flows roughly here; used to anchor the extraction estimate and targets.
    private const float IdealFlow = 1.35f;
    private const float FastFlow = 1.65f;
    private const float SlowFlow = 1.05f;

    private const int TopPad = 60;
    private const int BottomPad = 28;
    private const float TitleH = 34f;
    private const float CoachH = 46f;
    private const float ReadoutH = 22f;
    private const float RowH = 40f;
    private const int RowGap = 14;

    private enum Taste { None, Sour, Good, Bitter }

    private float _dose = 18f;
    private float _yield = 36f;
    private float _time = 28f;
    private Taste _taste = Taste.None;

    private readonly ChartView _chart = new();
    private TextView _coach = null!;
    private TextView _readout = null!;
    private TextView _doseValue = null!;
    private TextView _yieldValue = null!;
    private TextView _timeValue = null!;
    private readonly List<(Taste taste, RectView btn, TextView text)> _tasteButtons = new();

    public MultiChildView Root { get; }

    public EspressoDialingScreen(int width, int height, Context context)
    {
        Root = Build(width, height, context);
        RefreshTasteButtons();
        Recompute();
    }

    public void Resize(int width, int height)
    {
        Root.Width = width;
        Root.Height = height;
        _chart.Height = ChartHeightFor(height);
    }

    // The chart takes whatever vertical space the fixed bands leave. Computing it explicitly (vs a
    // flex-grow slot) keeps the whole column deterministic, so changing text never moves the graph.
    private static float ChartHeightFor(int height)
    {
        var fixedH = TitleH + CoachH + ReadoutH + 4f * RowH + 7f * RowGap;
        return MathF.Max(200f, height - TopPad - BottomPad - fixedH);
    }

    private MultiChildView Build(int width, int height, Context context)
    {
        // NOTE: TextView.MeasureHeight ignores its Height (it always reports the measured text
        // height) while OnLayoutSelf honours it — so a fixed-Height TextView is allocated the
        // wrong space and overlaps its neighbours. Reserve fixed bands with a container instead.
        var title = new TextView
        {
            Text = "Espresso Dial-In",
            TextColor = TitleColor,
            FontSize = 22f,
            HorizontalTextAlignment = TextAlignment.Start,
        };

        _coach = new TextView
        {
            Text = "",
            TextColor = TitleColor,
            FontSize = 15f,
            TextWrap = TextWrap.Wrap,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Start,
        };

        _readout = new TextView
        {
            Text = "",
            TextColor = MutedColor,
            FontSize = 13f,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Start,
        };

        var doseSlider = new SliderView { Min = 14f, Max = 22f, Value = _dose };
        doseSlider.ValueChanged += v => { _dose = v; Recompute(); };
        _doseValue = MakeValueLabel();

        var yieldSlider = new SliderView { Min = 20f, Max = 60f, Value = _yield };
        yieldSlider.ValueChanged += v => { _yield = v; Recompute(); };
        _yieldValue = MakeValueLabel();

        var timeSlider = new SliderView { Min = 12f, Max = 45f, Value = _time };
        timeSlider.ValueChanged += v => { _time = v; Recompute(); };
        _timeValue = MakeValueLabel();

        _chart.Height = ChartHeightFor(height);

        var column = new ColumnView
        {
            Gap = RowGap,
            Children =
            {
                Band(TitleH, title),
                _chart,
                Band(CoachH, _coach),
                Band(ReadoutH, _readout),
                SliderRow("Dose", doseSlider, _doseValue),
                SliderRow("Yield", yieldSlider, _yieldValue),
                SliderRow("Time", timeSlider, _timeValue),
                TasteRow(),
            },
        };

        var panel = new RectView
        {
            BackgroundColor = ScreenColor,
            Padding = new PaddingStyle { Left = 20, Right = 20, Top = TopPad, Bottom = BottomPad },
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

    // A fixed-height band wrapping a single child, used to reserve exact vertical space for text
    // (a container honours Height during measurement; a bare TextView does not — see note above).
    private static MultiChildView Band(float height, View child) => new()
    {
        Height = height,
        Children = { child },
    };

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
            Height = RowH,
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

    private FlexRowView TasteRow()
    {
        return new FlexRowView
        {
            Gap = 10f,
            Height = RowH,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new TextView
                {
                    Text = "Taste",
                    Width = 56f,
                    TextColor = LabelColor,
                    FontSize = 14f,
                    VerticalTextAlignment = TextAlignment.Center,
                },
                new FlexItem { Grow = 1f, Child = TasteButton("Sour", Taste.Sour) },
                new FlexItem { Grow = 1f, Child = TasteButton("Good", Taste.Good) },
                new FlexItem { Grow = 1f, Child = TasteButton("Bitter", Taste.Bitter) },
            },
        };
    }

    private RectView TasteButton(string label, Taste value)
    {
        var text = new TextView
        {
            Text = label,
            FontSize = 13f,
            TextColor = TasteIdleText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var btn = new RectView
        {
            BackgroundColor = TasteIdleBg,
            BorderRadius = BorderRadiusStyle.All(9f),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 8, Bottom = 8 },
            Children = { text },
        };

        btn.UsePointerController(_ => new ButtonPointerController(btn)
        {
            Clicked = () => ToggleTaste(value),
        });

        _tasteButtons.Add((value, btn, text));
        return btn;
    }

    private void ToggleTaste(Taste value)
    {
        _taste = _taste == value ? Taste.None : value;
        RefreshTasteButtons();
        Recompute();
    }

    private void RefreshTasteButtons()
    {
        foreach (var (taste, btn, text) in _tasteButtons)
        {
            var selected = taste == _taste;
            btn.BackgroundColor = selected ? TasteColor(taste) : TasteIdleBg;
            text.TextColor = selected ? TasteSelText : TasteIdleText;
        }
    }

    private static uint TasteColor(Taste taste) => taste switch
    {
        Taste.Sour => Caution,
        Taste.Good => Balanced,
        Taste.Bitter => Bad,
        _ => TasteIdleBg,
    };

    private void Recompute()
    {
        var ratio = _dose > 0f ? _yield / _dose : 0f;
        var flow = _time > 0f ? _yield / _time : 0f;

        // Estimate extraction from flow (grind proxy), then nudge by tasted feedback. Strength
        // follows exactly from the ratio, so the point sits on the brew-ratio diagonal.
        var ey = 20.5f - (flow - IdealFlow) * 3.0f;
        if (_taste == Taste.Sour) ey -= 1.5f;
        else if (_taste == Taste.Bitter) ey += 1.5f;
        ey = Math.Clamp(ey, 13f, 27f);

        var tds = ratio > 0f ? ey / ratio : 0f;

        var color = ZoneColor(ey, tds);
        var targetTime = _yield / IdealFlow;

        _chart.SetShot(ey, tds, color);
        _coach.Text = Coach(flow, targetTime);
        _readout.Text = $"1:{ratio:0.0}  •  {flow:0.0} g/s  •  est. EY {ey:0.0}% / TDS {tds:0.0}%";
        _doseValue.Text = $"{_dose:0.0} g";
        _yieldValue.Text = $"{_yield:0} g";
        _timeValue.Text = $"{_time:0} s";
    }

    private string Coach(float flow, float targetTime)
    {
        switch (_taste)
        {
            case Taste.Sour:
                return $"Tastes sour → grind finer, slow it past ~{targetTime:0}s.";
            case Taste.Bitter:
                return "Tastes bitter → grind coarser, or shorten the shot.";
            case Taste.Good:
                return "Dialed in — lock this dose, grind and ratio.";
        }

        if (flow > FastFlow)
            return $"Running fast → grind finer to reach ~{targetTime:0}s.";
        if (flow < SlowFlow)
            return $"Running slow → grind coarser to reach ~{targetTime:0}s.";
        return "Good flow — taste it, then tap Sour or Bitter to fine-tune.";
    }

    private static uint ZoneColor(float ey, float tds)
    {
        var extOk = ey >= ChartView.IdealEyMin && ey <= ChartView.IdealEyMax;
        var bodyOk = tds >= ChartView.IdealTdsMin && tds <= ChartView.IdealTdsMax;
        if (extOk && bodyOk) return Balanced;
        return extOk || bodyOk ? Caution : Bad;
    }
}

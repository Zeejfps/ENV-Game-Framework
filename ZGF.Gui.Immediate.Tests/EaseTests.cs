namespace ZGF.Gui.Immediate.Tests;

public sealed class EaseTests
{
    public static TheoryData<string, Func<float, float>> Curves => new()
    {
        { nameof(Ease.Linear), Ease.Linear },
        { nameof(Ease.InQuad), Ease.InQuad },
        { nameof(Ease.OutQuad), Ease.OutQuad },
        { nameof(Ease.InOutQuad), Ease.InOutQuad },
        { nameof(Ease.InCubic), Ease.InCubic },
        { nameof(Ease.OutCubic), Ease.OutCubic },
        { nameof(Ease.InOutCubic), Ease.InOutCubic },
        { nameof(Ease.InQuart), Ease.InQuart },
        { nameof(Ease.OutQuart), Ease.OutQuart },
        { nameof(Ease.InOutQuart), Ease.InOutQuart },
        { nameof(Ease.InSine), Ease.InSine },
        { nameof(Ease.OutSine), Ease.OutSine },
        { nameof(Ease.InOutSine), Ease.InOutSine },
        { nameof(Ease.InExpo), Ease.InExpo },
        { nameof(Ease.OutExpo), Ease.OutExpo },
        { nameof(Ease.InOutExpo), Ease.InOutExpo },
        { nameof(Ease.InBack), Ease.InBack },
        { nameof(Ease.OutBack), Ease.OutBack },
        { nameof(Ease.InOutBack), Ease.InOutBack },
        { nameof(Ease.InElastic), Ease.InElastic },
        { nameof(Ease.OutElastic), Ease.OutElastic },
        { nameof(Ease.InBounce), Ease.InBounce },
        { nameof(Ease.OutBounce), Ease.OutBounce },
        { nameof(Ease.InOutBounce), Ease.InOutBounce },
    };

    [Theory]
    [MemberData(nameof(Curves))]
    public void Every_curve_starts_at_zero_and_ends_at_one(string name, Func<float, float> curve)
    {
        Assert.True(MathF.Abs(curve(0f)) < 1e-5f, $"{name}(0) = {curve(0f)}");
        Assert.True(MathF.Abs(curve(1f) - 1f) < 1e-5f, $"{name}(1) = {curve(1f)}");
    }

    [Fact]
    public void Polynomial_curves_hit_their_known_midpoints()
    {
        Assert.Equal(0.25f, Ease.InQuad(0.5f), 5);
        Assert.Equal(0.875f, Ease.OutCubic(0.5f), 5);
        Assert.Equal(0.5f, Ease.InOutCubic(0.5f), 5);
    }

    [Fact]
    public void Back_overshoots_and_bounce_stays_in_range()
    {
        var backPeak = 0f;
        var bounceMin = 1f;
        var bounceMax = 0f;
        for (var i = 0; i <= 100; i++)
        {
            var t = i / 100f;
            backPeak = MathF.Max(backPeak, Ease.OutBack(t));
            bounceMin = MathF.Min(bounceMin, Ease.OutBounce(t));
            bounceMax = MathF.Max(bounceMax, Ease.OutBounce(t));
        }

        Assert.True(backPeak > 1.05f);
        Assert.True(bounceMin >= 0f && bounceMax <= 1f + 1e-5f);
    }

    [Fact]
    public void Css_ease_matches_the_browser_curve()
    {
        var ease = Ease.CubicBezier(0.25f, 0.1f, 0.25f, 1f);

        Assert.Equal(0.8024f, ease.Evaluate(0.5f), 3);
        Assert.Equal(0f, ease.Evaluate(0f));
        Assert.Equal(1f, ease.Evaluate(1f));
    }

    [Fact]
    public void Straight_cubic_bezier_is_linear()
    {
        var linear = Ease.CubicBezier(0f, 0f, 1f, 1f);
        for (var i = 1; i < 10; i++)
            Assert.Equal(i / 10f, linear.Evaluate(i / 10f), 4);
    }

    [Fact]
    public void Cubic_bezier_rejects_x_controls_outside_unit_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Ease.CubicBezier(1.2f, 0f, 0.5f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Ease.CubicBezier(0.2f, 0f, -0.1f, 1f));
    }
}

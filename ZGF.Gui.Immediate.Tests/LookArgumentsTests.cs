namespace ZGF.Gui.Immediate.Tests;

public sealed class LookArgumentsTests
{
    [Theory]
    [InlineData("look")]
    [InlineData("todo-added")]
    [InlineData("frame@1.25x-frame3")]
    [InlineData("a.b")]
    public void A_plain_file_name_is_taken(string name) => LookArguments.RequireFileName(name);

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../escape")]
    [InlineData("sub/look")]
    [InlineData(@"sub\look")]
    [InlineData("C:look")]
    [InlineData("/look")]
    public void A_name_that_is_not_a_plain_file_name_is_refused(string name) =>
        Assert.Throws<ArgumentException>(() => LookArguments.RequireFileName(name));

    [Theory]
    [InlineData(0f, 1)]
    [InlineData(8f, 1)]
    [InlineData(1000f, LookArguments.MaxMagnify)]
    public void A_finite_margin_of_zero_or_more_and_a_magnify_from_one_to_the_most_are_taken(float margin, int magnify) =>
        LookArguments.RequireCrop(margin, magnify);

    [Theory]
    [InlineData(-0.5f, 1)]
    [InlineData(float.NaN, 1)]
    [InlineData(float.PositiveInfinity, 1)]
    [InlineData(8f, 0)]
    [InlineData(8f, -1)]
    [InlineData(8f, LookArguments.MaxMagnify + 1)]
    public void A_crop_out_of_range_is_refused_by_every_harness(float margin, int magnify)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LookArguments.RequireCrop(margin, magnify));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CaptureTarget.Element(Selector.AtPath("x"), margin, magnify));
    }
}

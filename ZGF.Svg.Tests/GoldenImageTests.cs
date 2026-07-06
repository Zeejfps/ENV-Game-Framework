namespace ZGF.Svg.Tests;

public sealed class GoldenImageTests
{
    public static TheoryData<string> IconNames()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.EnumerateFiles(GoldenImage.IconsDirectory, "*.svg").Order())
            data.Add(Path.GetFileNameWithoutExtension(file));
        return data;
    }

    [Theory]
    [MemberData(nameof(IconNames))]
    public void IconAt24(string name) => RunGolden(name, 24);

    [Theory]
    [MemberData(nameof(IconNames))]
    public void IconAt48(string name) => RunGolden(name, 48);

    [Theory]
    [MemberData(nameof(IconNames))]
    public void IconAt17(string name) => RunGolden(name, 17);

    private static void RunGolden(string name, int size)
    {
        var svg = File.ReadAllText(Path.Combine(GoldenImage.IconsDirectory, name + ".svg"));
        var doc = SvgDocument.Parse(svg);
        var rgba = doc.Rasterize(size, size);
        GoldenImage.AssertMatches(rgba, size, size, $"{name}@{size}");
    }
}

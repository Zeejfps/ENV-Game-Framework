using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The C# versions offered, oldest first, with the names ILSpy gives them.</summary>
internal static class CSharpVersions
{
    public static readonly (CSharpVersion Version, string Label)[] All =
    [
        (CSharpVersion.CSharp1, "C# 1.0 / VS .NET"),
        (CSharpVersion.CSharp2, "C# 2.0 / VS 2005"),
        (CSharpVersion.CSharp3, "C# 3.0 / VS 2008"),
        (CSharpVersion.CSharp4, "C# 4.0 / VS 2010"),
        (CSharpVersion.CSharp5, "C# 5.0 / VS 2012"),
        (CSharpVersion.CSharp6, "C# 6.0 / VS 2015"),
        (CSharpVersion.CSharp7, "C# 7.0 / VS 2017"),
        (CSharpVersion.CSharp7_1, "C# 7.1 / VS 2017.3"),
        (CSharpVersion.CSharp7_2, "C# 7.2 / VS 2017.4"),
        (CSharpVersion.CSharp7_3, "C# 7.3 / VS 2017.7"),
        (CSharpVersion.CSharp8, "C# 8.0 / VS 2019"),
        (CSharpVersion.CSharp9, "C# 9.0 / VS 2019.8"),
        (CSharpVersion.CSharp10, "C# 10.0 / VS 2022"),
        (CSharpVersion.CSharp11, "C# 11.0 / VS 2022.4"),
        (CSharpVersion.CSharp12, "C# 12.0 / VS 2022.8"),
        (CSharpVersion.CSharp13, "C# 13.0 / VS 2022.12"),
        (CSharpVersion.CSharp14, "C# 14.0 / VS 2026"),
    ];

    public static readonly string[] Labels = All.Select(static v => v.Label).ToArray();

    public static string Label(CSharpVersion version)
    {
        foreach (var (v, label) in All)
        {
            if (v == version) return label;
        }
        return version.ToString();
    }
}

namespace ZGF.ILSpy.Core;

/// <summary>The C# version decompiled code is written in: syntax newer than it is not used.</summary>
public enum CSharpVersion
{
    CSharp1 = 1,
    CSharp2 = 2,
    CSharp3 = 3,
    CSharp4 = 4,
    CSharp5 = 5,
    CSharp6 = 6,
    CSharp7 = 7,
    CSharp7_1 = 701,
    CSharp7_2 = 702,
    CSharp7_3 = 703,
    CSharp8 = 800,
    CSharp9 = 900,
    CSharp10 = 1000,
    CSharp11 = 1100,
    CSharp12 = 1200,
    CSharp13 = 1300,
    CSharp14 = 1400,
}

/// <summary>The choices that change how C# is decompiled.</summary>
public sealed record DecompilerOptions
{
    public static DecompilerOptions Default { get; } = new();

    public CSharpVersion LanguageVersion { get; init; } = CSharpVersion.CSharp14;
}

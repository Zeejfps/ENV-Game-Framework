using System.Diagnostics;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;

namespace ZGF.ILSpy.Core;

internal static class Settings
{
    public const string Indentation = "    ";

    public static DecompilerSettings Create() => Create(DecompilerOptions.Default);

    public static DecompilerSettings Create(DecompilerOptions options)
    {
        var settings = new DecompilerSettings(LanguageVersionOf(options.LanguageVersion))
        {
            ThrowOnAssemblyResolveErrors = false,
            ExpandMemberDefinitions = true,
        };
        options.Apply(settings);
        settings.CSharpFormattingOptions.IndentationString = Indentation;
        return settings;
    }

    internal static LanguageVersion LanguageVersionOf(CSharpVersion version) => version switch
    {
        CSharpVersion.CSharp1 => LanguageVersion.CSharp1,
        CSharpVersion.CSharp2 => LanguageVersion.CSharp2,
        CSharpVersion.CSharp3 => LanguageVersion.CSharp3,
        CSharpVersion.CSharp4 => LanguageVersion.CSharp4,
        CSharpVersion.CSharp5 => LanguageVersion.CSharp5,
        CSharpVersion.CSharp6 => LanguageVersion.CSharp6,
        CSharpVersion.CSharp7 => LanguageVersion.CSharp7,
        CSharpVersion.CSharp7_1 => LanguageVersion.CSharp7_1,
        CSharpVersion.CSharp7_2 => LanguageVersion.CSharp7_2,
        CSharpVersion.CSharp7_3 => LanguageVersion.CSharp7_3,
        CSharpVersion.CSharp8 => LanguageVersion.CSharp8_0,
        CSharpVersion.CSharp9 => LanguageVersion.CSharp9_0,
        CSharpVersion.CSharp10 => LanguageVersion.CSharp10_0,
        CSharpVersion.CSharp11 => LanguageVersion.CSharp11_0,
        CSharpVersion.CSharp12 => LanguageVersion.CSharp12_0,
        CSharpVersion.CSharp13 => LanguageVersion.CSharp13_0,
        CSharpVersion.CSharp14 => LanguageVersion.CSharp14_0,
        _ => throw new UnreachableException(),
    };
}

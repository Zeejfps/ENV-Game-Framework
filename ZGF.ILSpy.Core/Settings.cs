using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;

namespace ZGF.ILSpy.Core;

internal static class Settings
{
    public const string Indentation = "    ";

    public static DecompilerSettings Create() => Create(DecompilerOptions.Default);

    public static DecompilerSettings Create(DecompilerOptions options)
    {
        var settings = new DecompilerSettings((LanguageVersion)options.LanguageVersion)
        {
            ThrowOnAssemblyResolveErrors = false,
            ExpandMemberDefinitions = true,
        };
        settings.CSharpFormattingOptions.IndentationString = Indentation;
        return settings;
    }
}

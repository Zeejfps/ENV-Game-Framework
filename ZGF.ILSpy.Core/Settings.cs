using ICSharpCode.Decompiler;

namespace ZGF.ILSpy.Core;

internal static class Settings
{
    public const string Indentation = "    ";

    public static DecompilerSettings Create()
    {
        var settings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            ExpandMemberDefinitions = true,
        };
        settings.CSharpFormattingOptions.IndentationString = Indentation;
        return settings;
    }
}

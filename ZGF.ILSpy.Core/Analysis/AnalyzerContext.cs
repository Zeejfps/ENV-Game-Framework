// Adapted from ILSpyX's analyzer framework (MIT License, Copyright (c) AlphaSierraPapa for the SharpDevelop Team).

using System.Reflection.Metadata;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Finds the symbols related to an analyzed one in a particular way: its users, its
/// overrides, and so on.</summary>
internal interface IAnalyzer
{
    /// <summary>Whether the analysis applies to <paramref name="symbol"/>; false for null.</summary>
    bool Show(ISymbol? symbol);

    IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context);
}

/// <summary>What the analyzers consult, as ILSpy's Language does: how C# maps compiler-generated
/// code to the member it came from, and which members it hides.</summary>
internal sealed class AnalyzerLanguage
{
    public static AnalyzerLanguage CSharp { get; } = new();

    public CodeMappingInfo GetCodeMappingInfo(MetadataFile module, EntityHandle member) => CSharpDecompiler.GetCodeMappingInfo(module, member);

    public bool ShowMember(IEntity member) =>
        member.ParentModule?.MetadataFile is not { } module || !CSharpDecompiler.MemberIsHidden(module, member.MetadataToken, Settings.Create());
}

/// <summary>What an analysis runs against: the open assemblies, the language, and cancellation.</summary>
internal sealed class AnalyzerContext
{
    public required AssemblyList AssemblyList { get; init; }

    public CancellationToken CancellationToken { get; init; }

    public AnalyzerLanguage Language { get; init; } = AnalyzerLanguage.CSharp;

    /// <summary>Set by an analyzer whose results read best sorted.</summary>
    public bool SortResults { get; set; }

    public MethodBodyBlock? GetMethodBody(IMethod method)
    {
        if (!method.HasBody || method.MetadataToken.IsNil || method.ParentModule?.MetadataFile == null)
            return null;
        var module = method.ParentModule.MetadataFile;
        var md = module.Metadata.GetMethodDefinition((MethodDefinitionHandle)method.MetadataToken);
        try
        {
            return module.GetMethodBody(md.RelativeVirtualAddress);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    public AnalyzerScope GetScopeOf(IEntity entity) => new(AssemblyList, entity);

    public DecompilerTypeSystem GetOrCreateTypeSystem(MetadataFile module) => AssemblyList.TypeSystemOf(module);
}

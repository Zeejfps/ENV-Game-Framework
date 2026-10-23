// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using ICSharpCode.BamlDecompiler;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Core;

/// <summary>Decompiles an assembly into a project as ILSpy does, writing its BAML back as XAML
/// pages beside the code-behind, whose generated members stay out of the C# files.</summary>
internal sealed class BamlProjectDecompiler(
    LoadedAssembly assembly, DecompilerSettings settings, IDebugInfoProvider? symbols, CancellationToken cancellationToken)
    : WholeProjectDecompiler(settings, assembly.Resolver, null, null, symbols)
{
    private const string PageItem = "Page";
    private const string ApplicationDefinitionItem = "ApplicationDefinition";

    protected override IEnumerable<ProjectItemInfo> WriteResourceToFile(string fileName, string resourceName, Stream entryStream)
    {
        if (!Baml.IsBamlName(fileName))
            return base.WriteResourceToFile(fileName, resourceName, entryStream);
        return [WriteXaml(fileName, entryStream)];
    }

    private ProjectItemInfo WriteXaml(string fileName, Stream baml)
    {
        var typeSystem = new BamlDecompilerTypeSystem(assembly.File, assembly.Resolver);
        var result = Baml.Decompile(typeSystem, baml, cancellationToken);
        var codeBehind = FindCodeBehind(typeSystem, result);
        if (codeBehind == null)
            return SaveXaml(result, Baml.XamlName(fileName), PageItem);
        var xamlName = CleanUpPath(codeBehind.ReflectionName) + Baml.XamlExtension;
        var item = SaveXaml(result, xamlName, ItemTypeOf(codeBehind));
        item.PartialTypes = [GeneratedPart(codeBehind, result)];
        return item;
    }

    private static ITypeDefinition? FindCodeBehind(BamlDecompilerTypeSystem typeSystem, BamlDecompilationResult result)
    {
        if (result.TypeName == null)
            return null;
        return typeSystem.MainModule.GetTypeDefinition(result.TypeName.Value.TopLevelTypeName);
    }

    private ProjectItemInfo SaveXaml(BamlDecompilationResult result, string xamlName, string itemType)
    {
        var path = Path.Combine(TargetDirectory, xamlName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        result.Xaml.Save(path);
        return new ProjectItemInfo(itemType, xamlName);
    }

    private string ItemTypeOf(ITypeDefinition codeBehind)
    {
        if (IsApplicationDefinition(codeBehind, assembly.File))
            return ApplicationDefinitionItem;
        return PageItem;
    }

    private static PartialTypeInfo GeneratedPart(ITypeDefinition codeBehind, BamlDecompilationResult result)
    {
        var part = new PartialTypeInfo(codeBehind);
        foreach (var member in result.GeneratedMembers)
            part.AddDeclaredMember(member);
        return part;
    }
}

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;
using AssemblyReference = ICSharpCode.Decompiler.Metadata.AssemblyReference;
using MetadataResource = ICSharpCode.Decompiler.Metadata.Resource;

namespace ZGF.ILSpy.Core;

internal static class Members
{
    private static readonly DecompilerSettings Settings = Core.Settings.Create();

    public static bool IsHidden(IEntity entity) => IsMissing(entity) || IsCompilerGenerated(entity);

    /// <summary>Whether <paramref name="entity"/> has no metadata of its own to show.</summary>
    public static bool IsMissing(IEntity entity) => entity.MetadataToken.IsNil;

    public static bool IsCompilerGenerated(IEntity entity)
    {
        var file = entity.ParentModule?.MetadataFile;
        return file != null && CSharpDecompiler.MemberIsHidden(file, entity.MetadataToken, Settings);
    }

    /// <summary>The accessors of a property or event, in ILSpy's order; none for other entities.</summary>
    public static IEnumerable<IMethod> Accessors(IEntity entity)
    {
        IEnumerable<IMethod?> accessors = entity switch
        {
            IProperty property => [property.Getter, property.Setter],
            IEvent @event => [@event.AddAccessor, @event.RemoveAccessor, @event.InvokeAccessor],
            _ => [],
        };
        return accessors.OfType<IMethod>();
    }
}

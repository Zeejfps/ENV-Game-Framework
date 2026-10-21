using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A kind's glyph, drawn in the bold monospace face so icons line up: upper case for
/// types and containers, lower case for members.</summary>
public static class NodeIcons
{
    private readonly record struct Icon(string Glyph, uint Dark, uint Light);

    private static readonly Icon[] Icons = BuildIcons();

    public const string Family = MonoFonts.Bold;

    public static string Glyph(NodeKind kind) => Icons[(int)kind].Glyph;

    public static uint Color(NodeKind kind, bool dark) => dark ? Icons[(int)kind].Dark : Icons[(int)kind].Light;

    private static Icon[] BuildIcons()
    {
        var icons = new Icon[Enum.GetValues<NodeKind>().Length];
        Set(NodeKind.Assembly, "A", 0xFFB0B8C4, 0xFF5A6370);
        Set(NodeKind.ReferencesFolder, "R", 0xFF8C8F98, 0xFF6B7079);
        Set(NodeKind.AssemblyReference, "a", 0xFF8C8F98, 0xFF6B7079);
        Set(NodeKind.ResourcesFolder, "R", 0xFF8C8F98, 0xFF6B7079);
        Set(NodeKind.Resource, "r", 0xFF8C8F98, 0xFF6B7079);
        Set(NodeKind.Namespace, "N", 0xFFC8C8C8, 0xFF555555);
        Set(NodeKind.Class, "C", 0xFFE8A85C, 0xFFB86A12);
        Set(NodeKind.Interface, "I", 0xFF5FC8B8, 0xFF16867A);
        Set(NodeKind.Struct, "S", 0xFF6AA8FF, 0xFF2463C7);
        Set(NodeKind.Enum, "E", 0xFFE0C860, 0xFF9A7E00);
        Set(NodeKind.Delegate, "D", 0xFFC890F0, 0xFF7E3FB0);
        Set(NodeKind.Record, "R", 0xFFE8A85C, 0xFFB86A12);
        Set(NodeKind.RecordStruct, "R", 0xFF6AA8FF, 0xFF2463C7);
        Set(NodeKind.BaseTypes, "B", 0xFF8C8F98, 0xFF6B7079);
        Set(NodeKind.BaseType, "b", 0xFF8C8F98, 0xFF6B7079);
        Set(NodeKind.Field, "f", 0xFF5FB8E8, 0xFF1A73A8);
        Set(NodeKind.Constant, "k", 0xFF5FB8E8, 0xFF1A73A8);
        Set(NodeKind.Property, "p", 0xFF8CD07A, 0xFF3C8A2A);
        Set(NodeKind.Indexer, "p", 0xFF8CD07A, 0xFF3C8A2A);
        Set(NodeKind.Event, "e", 0xFFE0C860, 0xFF9A7E00);
        Set(NodeKind.Method, "m", 0xFFC890F0, 0xFF7E3FB0);
        Set(NodeKind.Constructor, "c", 0xFFC890F0, 0xFF7E3FB0);
        Set(NodeKind.Operator, "o", 0xFFC890F0, 0xFF7E3FB0);
        Set(NodeKind.Error, "!", 0xFFFF6B6B, 0xFFC62828);
        Set(NodeKind.Loading, "…", 0xFF8C8F98, 0xFF6B7079);
        return icons;

        void Set(NodeKind kind, string glyph, uint dark, uint light) => icons[(int)kind] = new Icon(glyph, dark, light);
    }
}

namespace ZGF.Gui.Immediate;

/// <summary>Marks a partial class that holds an icon per key of the reference icon theme
/// <paramref name="referenceTheme"/>, a JSON file the project lists as an <c>AdditionalFiles</c> item,
/// named by the end of its path. A generator gives the class a property per key of the file's
/// <c>icons</c> object, its name the key in Pascal case (<c>node.class</c> becomes <c>NodeClass</c>), a
/// constructor that takes them from an <see cref="IconSet"/>, and a static <c>Keys</c> list.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ThemeIconsAttribute(string referenceTheme) : Attribute
{
    public string ReferenceTheme { get; } = referenceTheme;
}

/// <summary>An icon an icon theme sets by <see cref="Key"/>. <see cref="Description"/> is the comment
/// above the key in the reference theme, or empty.</summary>
public readonly record struct IconKey(string Key, string Description);

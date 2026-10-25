namespace ZGF.ILSpy;

/// <summary>A color or icon theme file as a catalog knows it: its id, its name, the theme it extends and
/// what was wrong in it.</summary>
public interface IThemeFile
{
    /// <summary>The file's name without its extension, which settings and <see cref="Extends"/> know it by.</summary>
    string Id { get; }

    /// <summary>Where the file is, as errors name it.</summary>
    string File { get; }

    ThemeName Name { get; }

    /// <summary>The id of the theme this one builds on, or null.</summary>
    string? Extends { get; }

    /// <summary>The line <see cref="Extends"/> is written on.</summary>
    int ExtendsLine { get; }

    /// <summary>True when the file could not be read as a JSON object, so nothing in it counts.</summary>
    bool IsUnreadable { get; }

    IReadOnlyList<ThemeError> Errors { get; }
}

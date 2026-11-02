namespace ZGF.ILSpy;

/// <summary>A color or icon theme file as a catalog knows it: its id, its name, the theme it extends and
/// what was wrong in it.</summary>
public interface IThemeFile<TSelf> where TSelf : IThemeFile<TSelf>
{
    /// <summary>The file's name without its extension, which settings and <see cref="Extends"/> know it by.</summary>
    ThemeId<TSelf> Id { get; }

    /// <summary>Where the file is, as errors name it.</summary>
    string File { get; }

    ThemeName Name { get; }

    /// <summary>The theme this one builds on, or null.</summary>
    ThemeParent<TSelf>? Extends { get; }

    IReadOnlyList<ThemeError> Errors { get; }
}

/// <summary>A theme file with what it sets, <typeparamref name="TContents"/>, which is null when the file
/// could not be read as a JSON object; what was read of its header before then still counts.</summary>
public interface IThemeFile<TSelf, out TContents> : IThemeFile<TSelf>
    where TSelf : IThemeFile<TSelf, TContents>
    where TContents : class
{
    TContents? Contents { get; }
}

/// <summary>The theme <see cref="Id"/> a theme file extends, written on <see cref="Line"/>.</summary>
public sealed record ThemeParent<TFile>(ThemeId<TFile> Id, int Line) where TFile : IThemeFile<TFile>;

namespace ZGF.ILSpy;

/// <summary>What can be wrong in a color or icon theme file, or in the themes it extends.</summary>
public enum ThemeProblem
{
    /// <summary>The file cannot be read; <see cref="ThemeError.Detail"/> says why.</summary>
    Unreadable,

    /// <summary>The file is not JSON; <see cref="ThemeError.Detail"/> says where it stops being JSON.</summary>
    InvalidJson,

    /// <summary>The file holds something other than one JSON object.</summary>
    RootNotAnObject,

    /// <summary>The value of <see cref="ThemeError.Subject"/> should be an object.</summary>
    NotAnObject,

    /// <summary>The value of <see cref="ThemeError.Subject"/> should be a string.</summary>
    NotAString,

    /// <summary>The name is neither a string nor an object of names by language tag.</summary>
    InvalidName,

    /// <summary>The name gives the language tag of <see cref="ThemeError.Subject"/> more than once,
    /// tags compared ignoring case.</summary>
    DuplicateLanguage,

    /// <summary>No theme file takes the key <see cref="ThemeError.Subject"/>.</summary>
    UnknownKey,

    /// <summary>The type is <see cref="ThemeError.Subject"/>, neither <c>dark</c> nor <c>light</c>.</summary>
    UnknownType,

    /// <summary>The value <see cref="ThemeError.Detail"/> of <see cref="ThemeError.Subject"/> is not a
    /// <c>#RRGGBB</c> or <c>#RRGGBBAA</c> color.</summary>
    NotAColor,

    /// <summary><see cref="ThemeError.Subject"/> is <c>#00000000</c>, which a theme takes as a color
    /// left unset, so it is refused rather than silently derived.</summary>
    TransparentBlack,

    /// <summary><see cref="ThemeError.Subject"/> names the color <see cref="ThemeError.Detail"/>, which
    /// the palette does not have.</summary>
    UnknownPaletteName,

    /// <summary>The theme extends no other and does not say whether it is dark or light.</summary>
    NoType,

    /// <summary>The theme extends <see cref="ThemeError.Subject"/>, which is no theme's id.</summary>
    UnknownParent,

    /// <summary>The themes extend each other in a circle, <see cref="ThemeError.Detail"/>.</summary>
    ExtendsCycle,

    /// <summary>The colors <see cref="ThemeError.Detail"/> are set neither by the theme nor by any it extends.</summary>
    MissingColors,

    /// <summary>A built-in theme, or a user theme read before, already has the id <see cref="ThemeError.Subject"/>.</summary>
    IdTaken,

    /// <summary>The glyph <see cref="ThemeError.Detail"/> of the icon <see cref="ThemeError.Subject"/> is
    /// not the hex digits of a Unicode codepoint.</summary>
    NotACodepoint,

    /// <summary>The icon <see cref="ThemeError.Subject"/> is neither a font and a glyph nor an SVG file.</summary>
    InvalidIcon,

    /// <summary>The icon <see cref="ThemeError.Subject"/> is drawn in the font <see cref="ThemeError.Detail"/>,
    /// which neither the theme nor any it extends declares.</summary>
    UnknownFont,

    /// <summary>The file <see cref="ThemeError.Detail"/> of <see cref="ThemeError.Subject"/> is not inside
    /// the folder of the theme file: an absolute path, or one that leaves the folder.</summary>
    AssetOutsideFolder,

    /// <summary>The file of <see cref="ThemeError.Subject"/> cannot be read; <see cref="ThemeError.Detail"/>
    /// names it and says why.</summary>
    AssetUnreadable,

    /// <summary>The file <see cref="ThemeError.Detail"/> of the font <see cref="ThemeError.Subject"/> is no
    /// font that can be loaded, such as one cut short.</summary>
    NotAFont,

    /// <summary>The file of the icon <see cref="ThemeError.Subject"/> is no SVG picture;
    /// <see cref="ThemeError.Detail"/> names it and says why.</summary>
    InvalidSvg,

    /// <summary>The font of the icon <see cref="ThemeError.Subject"/> has no glyph for the codepoint
    /// <see cref="ThemeError.Detail"/>.</summary>
    MissingGlyph,
}

/// <summary>Something wrong in the theme file <see cref="File"/> at <see cref="Line"/>, counted from 1,
/// or anywhere in it when 0: the key or value it is about, and what more there is to tell.</summary>
public sealed record ThemeError(string File, int Line, ThemeProblem Problem, string Subject = "", string Detail = "");

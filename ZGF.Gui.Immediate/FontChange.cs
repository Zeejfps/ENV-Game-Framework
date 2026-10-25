namespace ZGF.Gui.Immediate;

/// <summary>A font asked to be registered as <see cref="Family"/>, or the family asked to be
/// unregistered when <see cref="Face"/> is null.</summary>
internal readonly record struct FontChange(string Family, FontFace? Face);

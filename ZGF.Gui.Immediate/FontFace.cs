namespace ZGF.Gui.Immediate;

/// <summary>A font to register with <see cref="ImmediateGuiAppHost{TUiState}.RegisterFont"/>: its TTF/OTF
/// <paramref name="Data"/>, the <paramref name="Family"/> it is drawn as, and whether it is that
/// family's <paramref name="Bold"/> face.</summary>
public sealed record FontFace(string Family, byte[] Data, bool Bold = false);

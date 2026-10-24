namespace ZGF.Gui.Immediate;

/// <summary>A font to register with <see cref="ImmediateGuiAppHost{TUiState}.RegisterFont"/> or
/// <see cref="Ui.RegisterFont"/>: its TTF/OTF <paramref name="Data"/>, the <paramref name="Family"/> it
/// is drawn as, whether it is that family's <paramref name="Bold"/> face, and which face of a
/// collection (.ttc) <paramref name="Data"/> holds it is.</summary>
public sealed record FontFace(string Family, byte[] Data, bool Bold = false, int FaceIndex = 0);

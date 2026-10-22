namespace ZGF.Gui.Immediate;

/// <summary>A font an app registers: its TTF/OTF bytes, the family it is drawn as, and whether it is
/// that family's bold face.</summary>
internal sealed record FontFace(string Family, byte[] Data, bool Bold);

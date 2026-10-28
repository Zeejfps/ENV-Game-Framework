namespace ZGF.ILSpy;

/// <summary>Where a code view stood: the line at its top, its sideways scroll and its selection —
/// what going back to a place restores.</summary>
public readonly record struct CodeViewPosition(int TopLine, float ScrollX, TextPosition Anchor, TextPosition Caret);

using ZGF.Fonts.SystemFonts;

namespace ZGF.Fonts;

public static class NativeSystemFonts
{
    /// <summary>The installed fonts as DirectWrite lists them on Windows and CoreText on macOS, else
    /// a <see cref="NoSystemFonts"/>.</summary>
    public static ISystemFonts Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsSystemFonts();
        if (OperatingSystem.IsMacOS())
            return new MacSystemFonts();
        return new NoSystemFonts();
    }
}

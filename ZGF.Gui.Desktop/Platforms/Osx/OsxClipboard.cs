using System.Diagnostics;
using System.Text;

namespace ZGF.Gui.Desktop.Platforms.Osx;

public class OsxClipboard : IClipboard
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public void SetText(string text)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pbcopy",
            RedirectStandardInput = true,
            UseShellExecute = false,
            StandardInputEncoding = Utf8NoBom
        };
        psi.Environment["LANG"] = "en_US.UTF-8";
        psi.Environment["LC_CTYPE"] = "UTF-8";

        var process = Process.Start(psi);
        if (process == null)
            throw new Exception("Failed to start pbcopy");

        process.StandardInput.Write(text);
        process.StandardInput.Close();
        process.WaitForExit();
    }

    public string? GetText()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pbpaste",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            StandardOutputEncoding = Utf8NoBom
        };
        psi.Environment["LANG"] = "en_US.UTF-8";
        psi.Environment["LC_CTYPE"] = "UTF-8";

        var process = Process.Start(psi);
        if (process == null)
            throw new Exception("Failed to start pbpaste");

        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result;
    }
}
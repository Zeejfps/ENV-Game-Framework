using System.Diagnostics;

namespace ZGF.Gui.Tests;

public class OsxClipboard : IClipboard
{
    public void SetText(string text)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pbcopy",
            RedirectStandardInput = true,
            UseShellExecute = false
        };
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
            UseShellExecute = false
        };
        var process = Process.Start(psi);
        if (process == null)
            throw new Exception("Failed to start pbpaste");

        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return result;
    }
}
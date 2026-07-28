using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace ZGF.Gui.Desktop.Platforms.Linux;

/// <summary>
/// Linux <see cref="ISecretStore"/> over the Secret Service (GNOME Keyring, KWallet's D-Bus
/// bridge), driven through <c>secret-tool</c> the way <see cref="LinuxFilePicker"/> drives
/// zenity/kdialog. Secrets are looked up by a <c>service</c>/<c>account</c> attribute pair.
/// Distributions that ship no <c>secret-tool</c> and sessions with no Secret Service provider
/// report "no secret" rather than failing, leaving the caller on its own fallback.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxSecretStore : ISecretStore
{
    // A locked keyring puts an unlock prompt in front of a request. Waiting forever would wedge
    // the calling thread, so a request that outlives this is abandoned as "no secret".
    private const int TimeoutMs = 30_000;

    private readonly string _service;
    private readonly string? _secretTool;

    public LinuxSecretStore(string serviceName)
    {
        _service = serviceName;
        _secretTool = FindOnPath("secret-tool");

        if (_secretTool == null)
            Console.WriteLine("[SecretStore] No secret-tool found on PATH; stored secrets are unavailable.");
    }

    public string? Get(string name)
    {
        var (exitCode, stdout) = Run(["lookup", "service", _service, "account", name], null, name);
        if (exitCode != 0) return null;

        var secret = stdout.TrimEnd('\r', '\n');
        return secret.Length == 0 ? null : secret;
    }

    public bool Set(string name, string secret)
    {
        if (string.IsNullOrEmpty(secret)) return false;

        var (exitCode, _) = Run(
            ["store", $"--label={_service}: {name}", "service", _service, "account", name],
            secret,
            name);
        return exitCode == 0;
    }

    // clear exits 0 whether or not anything matched.
    public bool Delete(string name)
    {
        var (exitCode, _) = Run(["clear", "service", _service, "account", name], null, name);
        return exitCode == 0;
    }

    private (int ExitCode, string Stdout) Run(string[] args, string? stdin, string name)
    {
        if (_secretTool == null) return (-1, "");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _secretTool,
                RedirectStandardInput = stdin != null,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                // A preamble would be written into the secret itself.
                StandardInputEncoding = stdin != null ? new UTF8Encoding(false) : null,
            };
            foreach (var arg in args) psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process == null) return (-1, "");

            if (stdin != null)
            {
                process.StandardInput.Write(stdin);
                process.StandardInput.Close();
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(TimeoutMs))
            {
                process.Kill(entireProcessTree: true);
                Console.WriteLine($"[SecretStore] secret-tool {args[0]} for '{name}' timed out.");
                return (-1, "");
            }

            // lookup exits non-zero for "no such secret", which is not worth reporting.
            if (process.ExitCode != 0 && args[0] != "lookup" && !string.IsNullOrWhiteSpace(stderr))
                Console.WriteLine($"[SecretStore] secret-tool {args[0]} for '{name}' exited {process.ExitCode}: {stderr.Trim()}");

            return (process.ExitCode, stdout);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[SecretStore] secret-tool {args[0]} for '{name}' failed: {e.Message}");
            return (-1, "");
        }
    }

    private static string? FindOnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path)) return null;
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (dir.Length == 0) continue;
            var full = Path.Combine(dir, exe);
            if (File.Exists(full)) return full;
        }
        return null;
    }
}

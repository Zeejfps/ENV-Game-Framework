using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace ZGF.Gui.Desktop.Platforms.Osx;

/// <summary>
/// macOS <see cref="ISecretStore"/> over the login Keychain, driven through <c>/usr/bin/security</c>
/// the way <see cref="MacOsFilePicker"/> drives <c>osascript</c>. Secrets are generic passwords
/// whose service is the app and whose account is the secret name.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacOsSecretStore : ISecretStore
{
    // The keychain can put an "allow access" or unlock prompt in front of a read. Waiting forever
    // would wedge the calling thread, so a request that outlives this is abandoned as "no secret".
    private const int TimeoutMs = 30_000;

    // security(1) reports a missing keychain item with this status.
    private const int ItemNotFound = 44;

    private readonly string _service;

    public MacOsSecretStore(string serviceName)
    {
        _service = serviceName;
    }

    public string? Get(string name)
    {
        var (exitCode, stdout) = RunSecurity(["find-generic-password", "-s", _service, "-a", name, "-w"], name);
        if (exitCode != 0) return null;

        var secret = stdout.TrimEnd('\r', '\n');
        return secret.Length == 0 ? null : secret;
    }

    // The secret rides on the command line, so it is briefly visible to `ps` for other processes
    // of the same user. security(1) only reads it from stdin when attached to a terminal, and a
    // same-user process could read the keychain item itself anyway.
    public bool Set(string name, string secret)
    {
        if (string.IsNullOrEmpty(secret)) return false;

        var (exitCode, _) = RunSecurity(["add-generic-password", "-s", _service, "-a", name, "-w", secret, "-U"], name);
        return exitCode == 0;
    }

    public bool Delete(string name)
    {
        var (exitCode, _) = RunSecurity(["delete-generic-password", "-s", _service, "-a", name], name);
        return exitCode is 0 or ItemNotFound;
    }

    private static (int ExitCode, string Stdout) RunSecurity(string[] args, string name)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/security",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            foreach (var arg in args) psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process == null) return (-1, "");

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(TimeoutMs))
            {
                process.Kill(entireProcessTree: true);
                Console.WriteLine($"[SecretStore] security {args[0]} for '{name}' timed out.");
                return (-1, "");
            }

            if (process.ExitCode is not (0 or ItemNotFound) && !string.IsNullOrWhiteSpace(stderr))
                Console.WriteLine($"[SecretStore] security {args[0]} for '{name}' exited {process.ExitCode}: {stderr.Trim()}");

            return (process.ExitCode, stdout);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[SecretStore] security {args[0]} for '{name}' failed: {e.Message}");
            return (-1, "");
        }
    }
}

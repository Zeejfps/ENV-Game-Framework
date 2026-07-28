using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace ZGF.Gui.Desktop.Platforms.Windows;

/// <summary>
/// Windows <see cref="ISecretStore"/>. Windows has no per-app secret vault reachable without the
/// Credential Manager UI, so each secret is encrypted with DPAPI under the current user account
/// and kept as a file in the app's local data folder — only that user, on that machine, can read
/// it back.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSecretStore : ISecretStore
{
    private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    private readonly string _directory;
    private readonly byte[] _entropy;

    public WindowsSecretStore(string serviceName)
    {
        _directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            serviceName,
            "secrets");
        _entropy = Encoding.UTF8.GetBytes(serviceName);
    }

    public string? Get(string name)
    {
        try
        {
            var path = PathFor(name);
            if (!File.Exists(path)) return null;

            var plaintext = Unprotect(File.ReadAllBytes(path));
            return plaintext == null ? null : Encoding.UTF8.GetString(plaintext);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[SecretStore] Reading '{name}' failed: {e.Message}");
            return null;
        }
    }

    public bool Set(string name, string secret)
    {
        if (string.IsNullOrEmpty(secret)) return false;

        try
        {
            var ciphertext = Protect(Encoding.UTF8.GetBytes(secret));
            if (ciphertext == null)
            {
                Console.WriteLine($"[SecretStore] DPAPI refused to encrypt '{name}' ({Marshal.GetLastPInvokeErrorMessage()}).");
                return false;
            }

            Directory.CreateDirectory(_directory);
            File.WriteAllBytes(PathFor(name), ciphertext);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[SecretStore] Saving '{name}' failed: {e.Message}");
            return false;
        }
    }

    public bool Delete(string name)
    {
        try
        {
            // File.Delete tolerates a missing file but not a missing folder, which is the state
            // before anything has ever been stored.
            var path = PathFor(name);
            if (File.Exists(path)) File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[SecretStore] Deleting '{name}' failed: {e.Message}");
            return false;
        }
    }

    private byte[]? Protect(byte[] plaintext) => Crypt(plaintext, protect: true);

    private byte[]? Unprotect(byte[] ciphertext) => Crypt(ciphertext, protect: false);

    // The service-name entropy binds the blob to this app: another program running as the same
    // user cannot decrypt it by handing the bytes to DPAPI on its own.
    private unsafe byte[]? Crypt(byte[] input, bool protect)
    {
        fixed (byte* pInput = input)
        fixed (byte* pEntropy = _entropy)
        {
            var inBlob = new DataBlob { cbData = input.Length, pbData = (IntPtr)pInput };
            var entropyBlob = new DataBlob { cbData = _entropy.Length, pbData = (IntPtr)pEntropy };

            var ok = protect
                ? CryptProtectData(ref inBlob, null, ref entropyBlob, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, out var outBlob)
                : CryptUnprotectData(ref inBlob, IntPtr.Zero, ref entropyBlob, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, out outBlob);
            if (!ok) return null;

            try
            {
                var result = new byte[outBlob.cbData];
                Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                return result;
            }
            finally
            {
                LocalFree(outBlob.pbData);
            }
        }
    }

    private string PathFor(string name) => Path.Combine(_directory, Encode(name) + ".dpapi");

    /// <summary>
    /// Escapes everything outside a conservative set, so two secret names can never land on the
    /// same file and no name can escape the secrets folder.
    /// </summary>
    private static string Encode(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')
                sb.Append(c);
            else
                sb.Append('%').Append(((int)c).ToString("X4"));
        }
        return sb.ToString();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob pDataIn,
        string? szDataDescr,
        ref DataBlob pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DataBlob pDataOut);

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob pDataIn,
        IntPtr ppszDataDescr,
        ref DataBlob pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        int dwFlags,
        out DataBlob pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}

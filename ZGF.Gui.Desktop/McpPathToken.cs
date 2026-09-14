using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace ZGF.Gui.Desktop;

/// <summary>
/// The secret path segment that gates a <see cref="GuiMcpServer"/> endpoint. Only URL-unreserved
/// characters, so it drops into a path verbatim and round-trips through a settings file unchanged.
/// </summary>
public sealed record McpPathToken
{
    private const int MaxLength = 128;

    public string Value { get; }

    private McpPathToken(string value) => Value = value;

    /// <summary>A fresh 128-bit token as 32 lowercase hex characters.</summary>
    public static McpPathToken Generate() => new(Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16)));

    /// <summary>
    /// Accepts a stored token back: 1–128 characters, each a letter, digit, <c>-</c>, <c>.</c>,
    /// <c>_</c> or <c>~</c>.
    /// </summary>
    public static bool TryParse(string? text, [NotNullWhen(true)] out McpPathToken? token)
    {
        if (text is { Length: >= 1 and <= MaxLength } && text.All(IsUnreserved))
        {
            token = new McpPathToken(text);
            return true;
        }
        token = null;
        return false;
    }

    private static bool IsUnreserved(char c) =>
        char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~';

    public override string ToString() => Value;
}

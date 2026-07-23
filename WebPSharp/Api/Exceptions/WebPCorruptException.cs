namespace WebPSharp.Api.Exceptions;

/// <summary>
/// Thrown when a WebP stream is structurally valid at the container level but its compressed
/// payload is corrupt: an undecodable prefix (Huffman) code, an invalid back-reference, a
/// coefficient run that overruns a block, or a truncated entropy-coded segment. Distinguishes
/// payload corruption from the header/container violations reported by the base
/// <see cref="WebPFormatException"/>.
/// </summary>
public class WebPCorruptException : WebPFormatException
{
    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">A description of the corruption.</param>
    public WebPCorruptException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">A description of the corruption.</param>
    /// <param name="innerException">The underlying cause.</param>
    public WebPCorruptException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

namespace WebPSharp.Api.Exceptions;

/// <summary>
/// Thrown when WebP data violates the container or bitstream format: a bad RIFF signature,
/// a malformed chunk header, an out-of-range dimension, an unexpected chunk ordering, or an
/// otherwise non-decodable header.
/// </summary>
public class WebPFormatException : WebPException
{
    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">A description of the format violation.</param>
    public WebPFormatException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">A description of the format violation.</param>
    /// <param name="innerException">The underlying cause of the error.</param>
    public WebPFormatException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

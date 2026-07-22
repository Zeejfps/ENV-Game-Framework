namespace JpegSharp.Api.Exceptions;

/// <summary>
/// Thrown when JPEG data violates the format: an invalid marker, a malformed table,
/// an oversubscribed Huffman code, a truncated segment, or otherwise non-decodable input.
/// </summary>
public class JpegFormatException : JpegException
{
    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">A description of the format violation.</param>
    public JpegFormatException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">A description of the format violation.</param>
    /// <param name="innerException">The underlying cause of the error.</param>
    public JpegFormatException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

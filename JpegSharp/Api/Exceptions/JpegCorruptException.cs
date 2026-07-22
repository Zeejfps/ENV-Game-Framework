namespace JpegSharp.Api.Exceptions;

/// <summary>
/// Thrown when the JPEG headers are structurally valid but the entropy-coded data is corrupt:
/// an undecodable Huffman code, a coefficient magnitude out of range, a coefficient run that
/// overruns a block, or a missing restart marker. Distinguishes data corruption from the
/// header/format violations reported by the base <see cref="JpegFormatException"/>.
/// </summary>
public class JpegCorruptException : JpegFormatException
{
    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">A description of the corruption.</param>
    public JpegCorruptException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">A description of the corruption.</param>
    /// <param name="innerException">The underlying cause.</param>
    public JpegCorruptException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

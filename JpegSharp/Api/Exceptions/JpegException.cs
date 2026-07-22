namespace JpegSharp.Api.Exceptions;

/// <summary>
/// The base type for all exceptions raised by JpegSharp.
/// </summary>
public class JpegException : Exception
{
    /// <summary>Initializes a new instance with the specified message.</summary>
    /// <param name="message">A description of the error.</param>
    public JpegException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">A description of the error.</param>
    /// <param name="innerException">The underlying cause of the error.</param>
    public JpegException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

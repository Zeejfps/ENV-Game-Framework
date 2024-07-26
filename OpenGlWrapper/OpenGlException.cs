using System.Diagnostics;

namespace OpenGlWrapper;

public sealed class OpenGlException : Exception
{
    private readonly StackTrace m_StackTrace;

    public OpenGlException(string error, StackTrace stackTrace) : base(error)
    {
        m_StackTrace = stackTrace;
    }

    public override string? StackTrace => m_StackTrace.ToString();
}
using System;

namespace DerasaX.Domain.Exceptions
{
    /// <summary>
    /// Raised when an internal AI-service (school-ai-rag) call fails. The
    /// <see cref="Message"/> is always a safe, non-sensitive summary suitable to
    /// echo to the client; the underlying cause/provider body is logged
    /// server-side only. <see cref="Category"/> classifies the failure
    /// (timeout, unavailable, provider_error, bad_response) for telemetry.
    /// Mapped by the API exception middleware to a stable RFC 9457 502 problem.
    /// </summary>
    public class AiServiceException : Exception
    {
        public string Category { get; }

        public AiServiceException(string category, string safeMessage, Exception? inner = null)
            : base(safeMessage, inner)
        {
            Category = category;
        }
    }
}

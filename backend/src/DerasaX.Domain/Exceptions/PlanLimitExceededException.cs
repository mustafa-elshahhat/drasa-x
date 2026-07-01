using System;

namespace DerasaX.Domain.Exceptions
{
    /// <summary>
    /// Raised when an operation would exceed a limit defined by the tenant's current
    /// subscription plan (e.g. max students, storage, AI requests/month). Maps to
    /// HTTP 409 with the canonical <c>PLAN_LIMIT_EXCEEDED</c> error code.
    /// </summary>
    public class PlanLimitExceededException(string message) : Exception(message);
}

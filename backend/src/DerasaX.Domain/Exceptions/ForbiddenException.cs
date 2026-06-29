using System;

namespace DerasaX.Domain.Exceptions
{
    /// <summary>
    /// Raised when an authenticated caller is correctly identified and the target
    /// resource exists within scope, but the caller's relationship to it does not grant
    /// the requested action (e.g. a teacher accessing a student they are not assigned to,
    /// or a non-participant posting to a conversation). Maps to HTTP 403 with the
    /// canonical <c>FORBIDDEN</c> error code (Phase 2 ERROR_CONTRACT §3).
    ///
    /// Use this ONLY when revealing that the resource exists is acceptable. When the
    /// resource is in another tenant (or its existence must not leak), throw
    /// <see cref="NotFoundException"/> instead so the response is a safe 404.
    /// </summary>
    public class ForbiddenException(string message) : Exception(message);
}

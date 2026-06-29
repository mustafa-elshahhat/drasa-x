using System;

namespace DerasaX.Domain.Exceptions
{
    /// <summary>
    /// Raised when a request cannot be completed because it conflicts with the
    /// current state of a resource — a duplicate unique key, an invalid-state
    /// transition, or an optimistic-concurrency clash. Maps to HTTP 409 with the
    /// canonical <c>CONFLICT</c> error code (Phase 2 ERROR_CONTRACT §3).
    /// </summary>
    public class ConflictException(string message) : Exception(message);
}

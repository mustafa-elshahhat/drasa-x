using System.Threading;
using System.Threading.Tasks;

namespace DerasaX.Application.Services.Abstractions.Provisioning
{
    /// <summary>
    /// Single, centralized source of generated account credentials: a strong CSPRNG temporary
    /// password and a unique login identifier derived from an (already English-validated) full
    /// name. Every account creation/reset flow (SystemAdmin onboarding a SchoolAdmin, SchoolAdmin
    /// provisioning Student/Teacher/Parent, and any reset-credential action) must go through this
    /// service so generation rules and collision handling live in exactly one place.
    /// </summary>
    public interface ICredentialProvisioningService
    {
        /// <summary>A one-time, cryptographically random temporary password. Never logged or persisted in clear text.</summary>
        string GenerateTemporaryPassword();

        /// <summary>
        /// A stable, unique login identifier derived from <paramref name="fullName"/> and
        /// <paramref name="role"/>, retried against a global uniqueness check until a free
        /// candidate is found.
        /// </summary>
        Task<string> GenerateLoginCodeAsync(string fullName, string role, CancellationToken ct = default);
    }
}

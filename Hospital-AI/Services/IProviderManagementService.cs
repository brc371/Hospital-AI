using Hospital_AI.Models;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Manages the provider/admin roster: adding new accounts and activating/deactivating
    /// existing ones. Used by the Admin dashboard. Deactivating a provider does not delete
    /// their historical encounters/note versions (preserving the audit trail); it only blocks
    /// them from signing in (see <see cref="IRoleResolutionService"/>).
    /// </summary>
    public interface IProviderManagementService
    {
        /// <summary>Retrieves all provider/admin accounts, ordered by name.</summary>
        Task<List<Provider>> GetAllAsync();

        /// <summary>
        /// Adds a new provider or admin account. The email must be unique; callers should
        /// validate this beforehand (a duplicate will throw a database constraint violation).
        /// </summary>
        /// <param name="name">The display name of the account.</param>
        /// <param name="email">The sign-in email address (must match Entra External ID login).</param>
        /// <param name="role">The role to assign (Provider or Admin).</param>
        Task<Provider> AddAsync(string name, string email, ProviderRole role);

        /// <summary>
        /// Sets a provider account's active flag. Returns <c>null</c> if no account with the
        /// given ID exists.
        /// </summary>
        /// <param name="providerId">The provider account to update.</param>
        /// <param name="isActive">Whether the account should be active.</param>
        Task<Provider?> SetActiveAsync(Guid providerId, bool isActive);
    }
}

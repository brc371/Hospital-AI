using Hospital_AI.Models;

namespace Hospital_AI.Services
{
    /// <summary>
    /// Resolves the currently signed-in Entra External ID user to a <see cref="Provider"/>
    /// record by matching their email claim, so the app knows the user's role (Provider vs
    /// Admin) and whether they are an active, recognized account.
    /// </summary>
    public interface IRoleResolutionService
    {
        /// <summary>
        /// Looks up the <see cref="Provider"/> record matching the given email address.
        /// Returns <c>null</c> if no matching account exists (unknown/unauthorized user) or the
        /// account has been deactivated.
        /// </summary>
        /// <param name="email">The email address from the signed-in user's claims.</param>
        Task<Provider?> ResolveByEmailAsync(string email);
    }
}

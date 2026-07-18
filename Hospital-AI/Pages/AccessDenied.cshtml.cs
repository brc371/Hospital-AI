using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hospital_AI.Pages
{
    /// <summary>
    /// Shown when a user successfully signs in via Entra External ID but their email does not
    /// match any active <see cref="Models.Provider"/> record (unknown or deactivated account).
    /// </summary>
    [AllowAnonymous]
    public class AccessDeniedModel : PageModel
    {
        /// <summary>The email address of the signed-in but unrecognized user, if available.</summary>
        public string? SignedInEmail { get; private set; }

        /// <summary>Handles GET requests to the access-denied page.</summary>
        public void OnGet()
        {
            SignedInEmail = User.Identity?.Name;
        }
    }
}

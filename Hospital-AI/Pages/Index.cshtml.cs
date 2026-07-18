using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hospital_AI.Pages
{
    /// <summary>
    /// The public welcome page. Unauthenticated users see a description of the application
    /// and a sign-in link; authenticated users see a greeting and a link into their workspace.
    /// </summary>
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        /// <summary>Handles GET requests to the welcome page.</summary>
        public void OnGet()
        {
        }
    }
}

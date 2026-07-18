namespace Hospital_AI.Models
{
    /// <summary>
    /// The role assigned to an authenticated user of the Clinical Scribe platform.
    /// </summary>
    public enum ProviderRole
    {
        /// <summary>A clinician who documents encounters and can only see their own data.</summary>
        Provider = 0,

        /// <summary>An administrator who manages providers, templates, and can view all encounters.</summary>
        Admin = 1
    }
}

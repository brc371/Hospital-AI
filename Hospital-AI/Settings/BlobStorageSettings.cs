namespace Hospital_AI.Settings
{
    /// <summary>
    /// Configuration settings for Azure Blob Storage connectivity.
    /// Mirrors the pattern used in the lecture demo's StorageAccountSettings class.
    /// </summary>
    public class BlobStorageSettings
    {
        /// <summary>
        /// The service-level URI for the Azure Blob Storage account.
        /// Example: https://&lt;accountname&gt;.blob.core.windows.net
        /// </summary>
        public required string Uri { get; set; }

        /// <summary>
        /// The Azure AD tenant ID where the storage account and managed identity reside.
        /// Used to direct <see cref="Azure.Identity.DefaultAzureCredential"/> to the correct
        /// Azure AD tenant when running in a local development environment, preventing it from
        /// probing the wrong tenant (e.g. 'Microsoft Services') via VisualStudioCredential.
        /// </summary>
        public required string TenantId { get; set; }
    }
}

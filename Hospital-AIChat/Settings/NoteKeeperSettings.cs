namespace Hospital_AIChat.Settings
{
    /// <summary>
    /// Configuration settings for connecting to the NoteKeeper REST API.
    /// Bound from the "NoteKeeperSettings" section of appsettings.json.
    /// </summary>
    public class NoteKeeperSettings
    {
        /// <summary>
        /// Gets or sets the base URL of the locally running NoteKeeper API instance.
        /// Example: https://localhost:7197
        /// </summary>
        public string LocalhostBaseUrl { get; set; } = "https://localhost:7197";

        /// <summary>
        /// Gets or sets the base URL of the Azure-deployed NoteKeeper API instance.
        /// Example: https://hw4-notekeeper.azurewebsites.net
        /// </summary>
        public string AzureBaseUrl { get; set; } = null!;

        /// <summary>
        /// Gets or sets which endpoint is currently active.
        /// Valid values are "Localhost" or "Azure".
        /// </summary>
        public string ActiveEndpoint { get; set; } = "Localhost";

        /// <summary>
        /// Returns the base URL for the currently active endpoint.
        /// </summary>
        /// <returns>
        /// <see cref="AzureBaseUrl"/> when <see cref="ActiveEndpoint"/> equals "Azure" (case-insensitive);
        /// otherwise <see cref="LocalhostBaseUrl"/>.
        /// </returns>
        public string GetActiveBaseUrl() =>
            ActiveEndpoint.Equals("Azure", StringComparison.OrdinalIgnoreCase)
                ? AzureBaseUrl
                : LocalhostBaseUrl;
    }
}

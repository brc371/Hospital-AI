namespace Hospital_AIChat.Settings
{
    /// <summary>
    /// Configuration settings for the Azure AI deployment used by the NoteKeeper Chat application.
    /// Bound from the "AISettings" section of appsettings.json.
    /// </summary>
    public class AISettings
    {
        /// <summary>
        /// Gets or sets the base URI of the Azure AI / Azure OpenAI endpoint.
        /// Example: https://&lt;resource&gt;.cognitiveservices.azure.com/
        /// </summary>
        public string DeploymentUri { get; set; } = null!;

        /// <summary>
        /// Gets or sets the API key for the Azure AI service.
        /// Should be loaded from User Secrets or environment variables, not from appsettings.json.
        /// </summary>
        public string ApiKey { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the deployed model (e.g. "gpt-5.3-chat").
        /// </summary>
        public string DeploymentModelName { get; set; } = "gpt-5.3-chat";

        /// <summary>
        /// Gets or sets the model identifier used in <see cref="Microsoft.Extensions.AI.ChatOptions"/>.
        /// Defaults to "gpt-5.3-chat".
        /// </summary>
        public string ModelId { get; set; } = "gpt-5.3-chat";

        /// <summary>
        /// Gets or sets the sampling temperature.
        /// Higher values (e.g. 1.0) produce more varied responses; lower values (e.g. 0.0) are more deterministic.
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Gets or sets the nucleus-sampling top-p value.
        /// 1.0 means all tokens are considered; lower values narrow the token distribution.
        /// </summary>
        public float TopP { get; set; } = 1.0f;
    }
}

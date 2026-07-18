namespace Hospital_AI.Settings
{
    /// <summary>
    /// Configuration settings for the Azure OpenAI service used by the NoteKeeper API.
    /// Bound from the "AISettings" section of appsettings.json.
    /// </summary>
    public class AISettings
    {
        /// <summary>
        /// Gets or sets the Azure OpenAI deployment endpoint URI
        /// </summary>
        /// <example>https://your-resource.openai.azure.com/</example>
        public string DeploymentUri { get; set; }

        /// <summary>
        /// Gets or sets the API key for authenticating with Azure OpenAI service
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the name of the deployed model to use for chat completions
        /// </summary>
        /// <example>gpt-4</example>
        public string DeploymentModelName { get; set; } = "gpt-5-mini";

        /// <summary>
        /// Gets or sets the sampling temperature (0.0 to 2.0). Higher values make output more random, lower values make it more deterministic
        /// </summary>
        /// <example>1.0</example>
        public float Temperature { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the nucleus sampling parameter (0.0 to 1.0). Controls diversity via nucleus sampling
        /// </summary>
        /// <example>1.0</example>
        public float TopP { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate in the completion
        /// </summary>
        /// <example>2000</example>
        public int MaxOutputTokens { get; set; } = 2000;
    }
}

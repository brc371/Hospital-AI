namespace Hospital_AI.Models
{
    /// <summary>
    /// Represents a chat request sent to the AI model.
    /// </summary>
    public class ChatRequest
    {
        /// <summary>Optional system message to set the assistant's behavior.</summary>
        public string? SystemMessage { get; set; }

        /// <summary>The user's message to send to the AI model.</summary>
        public string? UserMessage { get; set; }
    }
}

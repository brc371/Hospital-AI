using Hospital_AI.Data;
using Hospital_AI.Models;
using Hospital_AI.Settings;
using Hospital_AI.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace Hospital_AI.Controllers
{
    /// <summary>
    /// Provides a chat endpoint that forwards messages to the configured Azure OpenAI model
    /// and returns the assistant's reply. The model can call hospital database tools to answer
    /// questions about patients and notes.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ChatController : ControllerBase
    {
        private readonly IChatClient _chatClient;
        private readonly AISettings _aiSettings;
        private readonly ILogger<ChatController> _logger;
        private readonly MyDataBaseContext _dbContext;

        private const string SystemMessage =
            "You are a helpful assistant for the Hospital DB application. " +
            "You have access to tools that can query the hospital database. " +
            "Always use the available tools to look up real data when the user asks about patients. " +
            "Be concise and friendly in your responses.";

        /// <summary>
        /// Initializes a new instance of <see cref="ChatController"/>.
        /// </summary>
        public ChatController(IChatClient chatClient, AISettings aiSettings,
                              ILogger<ChatController> logger, MyDataBaseContext dbContext)
        {
            _chatClient = chatClient;
            _aiSettings = aiSettings;
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Sends a chat message to the AI model and returns the response.
        /// The model may invoke hospital database tools to answer the question.
        /// </summary>
        /// <param name="request">The chat request containing the user message.</param>
        /// <returns>An object containing the AI's response text.</returns>
        /// <response code="200">Returns the AI response.</response>
        /// <response code="400">If the user message is empty.</response>
        /// <response code="500">If the AI call fails.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PostAsync([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.UserMessage))
            {
                return BadRequest(new { error = "UserMessage is required." });
            }

            try
            {
                // Build the tool set for this request (uses the scoped DbContext)
                var tools = new HospitalDbTools(_dbContext);
                var toolList = tools.GetTools().OfType<AIFunction>().ToList();

                var messages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, SystemMessage),
                    new ChatMessage(ChatRole.User, request.UserMessage)
                };

                var options = new ChatOptions
                {
                    Temperature = _aiSettings.Temperature,
                    TopP = 1.0f,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0,
                    Tools = [.. toolList]
                };

                // Agentic loop: keep calling the model until it produces a final text response
                // (i.e. it stops requesting tool calls and gives us an answer).
                while (true)
                {
                    var completion = await _chatClient.GetResponseAsync(messages, options);

                    // Add all messages the model produced this turn to the history
                    foreach (var msg in completion.Messages)
                        messages.Add(msg);

                    // If there are no tool calls the model is done — return the text
                    var toolCalls = completion.Messages
                        .SelectMany(m => m.Contents)
                        .OfType<FunctionCallContent>()
                        .ToList();

                    if (toolCalls.Count == 0)
                    {
                        var responseText = completion.Text ?? "No response from AI.";
                        return Ok(new ChatResponse { AIResponse = responseText });
                    }

                    // Execute each tool call and add the results back into the message history
                    foreach (var call in toolCalls)
                    {
                        _logger.LogInformation("AI invoked tool: {Tool}", call.Name);

                        var fn = toolList.FirstOrDefault(f => f.Name == call.Name);
                        object? toolResult = fn is not null
                            ? await fn.InvokeAsync(
                                new AIFunctionArguments(call.Arguments ?? new Dictionary<string, object?>()),
                                CancellationToken.None)
                            : $"Tool '{call.Name}' not found.";

                        messages.Add(new ChatMessage(ChatRole.Tool,
                        [
                            new FunctionResultContent(call.CallId, toolResult)
                        ]));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI chat completion.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred while contacting the AI service." });
            }
        }
    }

    /// <summary>Request body for the chat endpoint.</summary>
    public class ChatRequest
    {
        /// <summary>Optional system-level instruction for the AI.</summary>
        public string? SystemMessage { get; set; }

        /// <summary>The user's message to send to the AI.</summary>
        public string UserMessage { get; set; } = string.Empty;
    }

    /// <summary>Response body returned by the chat endpoint.</summary>
    public class ChatResponse
    {
        /// <summary>The AI's reply.</summary>
        public string AIResponse { get; set; } = string.Empty;
    }
}

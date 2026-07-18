using Azure;
using Azure.AI.OpenAI;
using Hospital_AIChat;
using Hospital_AIChat.Settings;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

/// <summary>
/// Entry point for the HW4 NoteKeeper Chat console application.
/// Provides a multi-turn, AI-powered text interface for interacting with the
/// NoteKeeper REST API using Azure OpenAI tool-calling (function calling).
/// </summary>
internal class Program
{
    /// <summary>AI settings loaded from configuration.</summary>
    private static AISettings _aiSettings = null!;

    /// <summary>NoteKeeper API connection settings loaded from configuration.</summary>
    private static NoteKeeperSettings _noteKeeperSettings = null!;

    /// <summary>
    /// Application entry point. Builds the DI host, registers services, wires up AI tools,
    /// and runs the interactive chat loop.
    /// </summary>
    /// <param name="args">Command-line arguments (not currently used).</param>
    private static async Task Main(string[] args)
    {
        // ── Configuration ─────────────────────────────────────────────────────
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        // User Secrets override appsettings.json values locally (e.g. the API key).
        builder.Configuration.AddUserSecrets<Program>();

        _aiSettings = new AISettings();
        builder.Configuration.Bind("AISettings", _aiSettings);

        _noteKeeperSettings = new NoteKeeperSettings();
        builder.Configuration.Bind("NoteKeeperSettings", _noteKeeperSettings);

        // ── HTTP Client for NoteKeeper API ────────────────────────────────────
        // A single shared HttpClient is used. DangerousAcceptAnyServerCertificateValidator
        // is enabled so the app works against the local dev certificate on localhost.
        builder.Services.AddSingleton(_noteKeeperSettings);
        builder.Services.AddHttpClient<NoteKeeperApiClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

        // ── AI Tools ──────────────────────────────────────────────────────────
        builder.Services.AddSingleton<NoteKeeperTools>();

        // ── Azure OpenAI Chat Client ──────────────────────────────────────────
        AzureKeyCredential apiKeyCredential = new AzureKeyCredential(_aiSettings.ApiKey);
        builder.Services.AddChatClient(services =>
            new AzureOpenAIClient(new Uri(_aiSettings.DeploymentUri), apiKeyCredential)
                .GetChatClient(_aiSettings.DeploymentModelName)
                .AsIChatClient()
        ).UseFunctionInvocation();

        // ── Build & Resolve ────────────────────────────────────────────────────
        var app = builder.Build();
        var chatClient = app.Services.GetRequiredService<IChatClient>();
        var noteKeeperTools = app.Services.GetRequiredService<NoteKeeperTools>();

        // Register the AI tools so the model can call them automatically.
        var chatOptions = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(noteKeeperTools.GetAllNotesAsync),
                AIFunctionFactory.Create(noteKeeperTools.GetNoteByIdAsync),
                AIFunctionFactory.Create(noteKeeperTools.DownloadAttachmentAsync),
                AIFunctionFactory.Create(noteKeeperTools.WriteDataToFileAsync)
            ],
            ModelId = _aiSettings.ModelId
        };

        DisplayStartupInfo();

        // ── Conversation History ───────────────────────────────────────────────
        // Messages are kept across turns to support multi-turn conversation context.
        // The user can type /clear to reset the history.
        List<ChatMessage> chatMessages = new List<ChatMessage>();
        chatMessages.Add(BuildSystemPrompt());

        // ── Chat Loop ─────────────────────────────────────────────────────────
        bool keepChatting = true;
        while (keepChatting)
        {
            string prompt = PromptUser();

            // ── Built-in commands ─────────────────────────────────────────────
            if (prompt.Equals("/bye", StringComparison.OrdinalIgnoreCase))
            {
                keepChatting = false;
                ConsoleHelpers.ConsoleWriteLine(ConsoleColor.Cyan, "\nGoodbye! Happy note-taking!");
                continue;
            }

            if (prompt.Equals("/clear", StringComparison.OrdinalIgnoreCase))
            {
                Console.Clear();
                chatMessages.Clear();
                chatMessages.Add(BuildSystemPrompt());
                DisplayStartupInfo();
                continue;
            }

            if (prompt.Equals("/switch", StringComparison.OrdinalIgnoreCase))
            {
                // Toggle between Localhost and Azure endpoints.
                // Because NoteKeeperSettings is a singleton and NoteKeeperApiClient reads
                // GetActiveBaseUrl() per request, all subsequent calls use the new endpoint.
                _noteKeeperSettings.ActiveEndpoint =
                    _noteKeeperSettings.ActiveEndpoint.Equals("Azure", StringComparison.OrdinalIgnoreCase)
                        ? "Localhost"
                        : "Azure";

                ConsoleHelpers.ConsoleWriteLine(ConsoleColor.Cyan,
                    $"\nSwitched to {_noteKeeperSettings.ActiveEndpoint} endpoint: {_noteKeeperSettings.GetActiveBaseUrl()}");
                continue;
            }

            // ── Send to AI ────────────────────────────────────────────────────
            chatMessages.Add(new ChatMessage(ChatRole.User, prompt));

            IAsyncEnumerable<ChatResponseUpdate> responseStream =
                chatClient.GetStreamingResponseAsync(chatMessages, options: chatOptions);

            string fullResponse = await OutputResponseAsync(responseStream);

            // Append the assistant's reply to the conversation history for next-turn context.
            chatMessages.Add(new ChatMessage(ChatRole.Assistant, fullResponse));
        }
    }

    /// <summary>
    /// Builds the system prompt that instructs the AI on its role and output format.
    /// </summary>
    /// <returns>A <see cref="ChatMessage"/> with role <see cref="ChatRole.System"/>.</returns>
    private static ChatMessage BuildSystemPrompt()
    {
        return new ChatMessage(ChatRole.System,
            "You are a helpful assistant for the NoteKeeper application. " +
            "You help users retrieve and manage their notes and attachments using the NoteKeeper REST API. " +
            "You MUST output plain text only - do NOT use markdown formatting: no bold, no italics, no headers, " +
            "no bullet points with asterisks or hyphens, no code blocks, no backticks. " +
            "Use plain numbered lists (1. 2. 3.) and plain sentences. " +
            "When listing notes or attachments, always number them starting from 1 so the user can reference " +
            "them by position number in follow-up questions (e.g. 'give me the details for note 2'). " +
            "Maintain conversation context: remember what was listed in previous turns so the user can say " +
            "'note 2' or 'the third note' and you know which GUID that refers to.");
    }

    /// <summary>
    /// Prints the startup banner with the active model and endpoint information.
    /// </summary>
    private static void DisplayStartupInfo()
    {
        Console.WriteLine(new string('=', 80));
        ConsoleHelpers.ConsoleWriteLine(ConsoleColor.Cyan, "  HW4 NoteKeeper Chat Interface");
        Console.WriteLine($"  AI Model   : {_aiSettings.DeploymentModelName}");
        Console.WriteLine($"  Endpoint   : {_noteKeeperSettings.ActiveEndpoint} -> {_noteKeeperSettings.GetActiveBaseUrl()}");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("Commands:  /bye = exit  |  /clear = reset conversation  |  /switch = toggle endpoint");
        Console.WriteLine();
    }

    /// <summary>
    /// Reads a prompt from the user. Returns a default question if the user presses Enter
    /// without typing anything.
    /// </summary>
    /// <returns>The trimmed user input string.</returns>
    private static string PromptUser()
    {
        Console.WriteLine(new string('-', 80));
        ConsoleHelpers.ConsoleWrite(ConsoleColor.Green, "\nYou: ");
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            input = "What can you help me with?";
            ConsoleHelpers.ConsoleWriteLine(ConsoleColor.DarkGray, $"(Using default: {input})");
        }

        return input.Trim();
    }

    /// <summary>
    /// Streams the AI response to the console and collects the full text for appending to
    /// the conversation history.
    /// </summary>
    /// <param name="responseStream">The streaming response from the chat client.</param>
    /// <returns>The complete response text assembled from all stream chunks.</returns>
    private static async Task<string> OutputResponseAsync(IAsyncEnumerable<ChatResponseUpdate> responseStream)
    {
        Console.WriteLine();
        ConsoleHelpers.ConsoleWrite(ConsoleColor.Cyan, "Assistant: ");

        var sb = new StringBuilder();
        await foreach (var chunk in responseStream)
        {
            string text = chunk?.Text ?? string.Empty;
            ConsoleHelpers.ConsoleWrite(ConsoleColor.DarkYellow, text);
            sb.Append(text);
        }

        Console.WriteLine();
        return sb.ToString();
    }
}

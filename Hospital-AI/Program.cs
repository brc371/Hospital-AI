using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Hospital_AI.Data;
using Hospital_AI.Services;
using Hospital_AI.Settings;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OpenApi;
using OpenAI.Chat;
using System.Reflection;

namespace Hospital_AI
{

    /// <summary>
    /// Configures and starts the AI Clinical Scribe ASP.NET Core Web API application,
    /// registering all required services (AI, database, Swagger) and running the
    /// HTTP request pipeline.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            // Add services to the container.
            builder.Services.AddControllers();

            // Register Entra External ID (CIAM) authentication using the Microsoft identity
            // platform. This configures cookie-based OpenID Connect sign-in: unauthenticated
            // users are redirected to the tenant's hosted sign-up/sign-in user flow, and upon
            // successful sign-in an encrypted auth cookie is issued containing the user's claims
            // (name, email, etc.), which are then available via the Razor Pages 'User' property.
            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

            // Require authentication by default on every Razor Page/controller unless the
            // page explicitly opts out with [AllowAnonymous] (e.g. the public welcome page).
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = options.DefaultPolicy;
            });

            // Register Razor Pages (the provider/admin UI) alongside the existing API
            // controllers, and wire up the prebuilt Microsoft.Identity.Web sign-in/out UI.
            builder.Services.AddRazorPages()
                .AddMicrosoftIdentityUI();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Bind AISettings from appsettings.json
            AISettings? _aiSettings = builder.Configuration.GetSection("AISettings").Get<AISettings>()!;

            // Validate AISettings to ensure they are not null or empty
            // This is a consolre logger as we defined it above. In a real application, you might use dependency injection to get a logger instance.
            ILogger logger = loggerFactory.CreateLogger("Program");

            if (_aiSettings is null
                || string.IsNullOrWhiteSpace(_aiSettings.DeploymentUri)
                || string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
            {

                if (_aiSettings == null)
                {
                    logger.LogCritical("AISettings is null. Please ensure the configuration is present.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_aiSettings.DeploymentUri))
                    {
                        logger.LogCritical("AISettings.DeploymentUri is null or empty.");
                    }
                    if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
                    {
                        logger.LogCritical("AISettings.ApiKey is null or empty.");
                    }
                }
                throw new InvalidOperationException("AISettings validation failed. Check the logs for details.");
            }

            // If validation passes, log success
            logger.LogInformation("AISettings loaded successfully.");

            // There should be only one instance of AISettings in the application (i.e. a singelton)
            builder.Services.AddSingleton(_aiSettings);

            // Initialize OpenAI service endpoint and API key credential
            Uri openAIServiceEndpointUri;
            AzureKeyCredential apiKeyCredential;
            openAIServiceEndpointUri = new Uri(_aiSettings.DeploymentUri);
            apiKeyCredential = new AzureKeyCredential(_aiSettings.ApiKey);
            RegisterOpenAIClient(builder, openAIServiceEndpointUri, apiKeyCredential, _aiSettings.DeploymentModelName);

            // Below we configure Swagger/OpenAPI doing a number of things:
            // 1. Define the title of the API
            // 2. Include XML comments if the XML documentation file exists
            builder.Services.AddSwaggerGen(c =>
            {
                // Title of the note app:
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hospital DB", Version = "v1" });

                // Add documentation via C# XML Comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                // Only include XML comments if the file exists
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
                              
            });

            // DEMO: Entity Framework: Register the context with dependency injection
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

            // In Azure, augment the connection string to authenticate using the user-assigned
            // managed identity (id_dbadmin) identified by DB_IDENTITY_CLIENT_ID.
            if (!builder.Environment.IsDevelopment() &&
                builder.Environment.EnvironmentName != "managedidentities")
            {
                var dbClientId = builder.Configuration["DB_IDENTITY_CLIENT_ID"];
                if (!string.IsNullOrWhiteSpace(dbClientId))
                {
                    var sqlConnBuilder = new SqlConnectionStringBuilder(connectionString)
                    {
                        Authentication = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity,
                        UserID = dbClientId
                    };
                    connectionString = sqlConnBuilder.ConnectionString;
                }
            }

            builder.Services.AddDbContext<ClinicalScribeDbContext>(options => options.UseSqlServer(connectionString));

            // Register the role-resolution service used to match a signed-in Entra External ID
            // user's email to their Provider record (and thus their role). Scoped lifetime
            // matches the DbContext it depends on.
            builder.Services.AddScoped<IRoleResolutionService, RoleResolutionService>();

            // Register the patient-matching and encounter services used by the provider
            // encounter workspace.
            builder.Services.AddScoped<IPatientMatchingService, PatientMatchingService>();
            builder.Services.AddScoped<IEncounterService, EncounterService>();

            // Register the note-generation service (streams AI-generated SOAP notes, with
            // tool-calling for patient history context).
            builder.Services.AddScoped<INoteGenerationService, NoteGenerationService>();

            // Register the ICD-10 search service used by the code search widget.
            builder.Services.AddScoped<IIcd10SearchService, Icd10SearchService>();

            // Register the Admin dashboard services: template CRUD and provider roster
            // management.
            builder.Services.AddScoped<ITemplateService, TemplateService>();
            builder.Services.AddScoped<IProviderManagementService, ProviderManagementService>();

            // This is the command to build the app
            // builder.<> methods. From this point on you work with "app" to configure stuff.
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {

            }

            // Apply pending EF Core migrations and seed demo Provider/Admin accounts at
            // startup. Seeding is idempotent (no-op if Providers table already has rows), so
            // this is safe to run every time the app starts, in both dev and Azure.
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ClinicalScribeDbContext>();
                await dbContext.Database.MigrateAsync();
                await DbSeeder.SeedAsync(dbContext);
                await DbSeeder.SeedIcd10CodesAsync(dbContext);
            }


            // Tells the app to use swashbuckle (middleware) to generate the OpenAPI JSON
            // automatically from your controllers and serves it at /swagger/v1/swagger.json
            // endpoint defined below. The JSON tells Swagger UI (or any other OpenAPI tool):
            // - endpoint paths(e.g., /weather/{id})
            // - allowed HTTP methods (GET/POST/etc.)
            // - parameter names, types, and where they come from (path, query, header, body)
            // - response shapes and status codes
            // - security requirements (API keys, OAuth)
            // Serve index.html (chat UI) at the root by default
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();

            // Fetche sthe JSON generated by the UseSwagger() middleware to render 
            // UI (HTML, JS, CSS) to the browser. 
            app.UseSwaggerUI(c =>
            {
                // 1. Display a friendly title
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospital DB V1");

                // Swagger UI is available at /swagger so the root serves the chat UI
                c.RoutePrefix = "swagger";
            });

            // Adds one or more routable endpoints to the app that return OpenAPI/Swagger JSON
            app.MapOpenApi();

            // Redirects HTTP requests to HTTPS.  adds middleware that automatically redirects
            // HTTP requests to HTTPS so clients use a secure connection.
            app.UseHttpsRedirection();

            // Adds authentication middleware to the request pipeline. Must run before
            // UseAuthorization() so the ClaimsPrincipal is populated (from the auth cookie)
            // before authorization checks evaluate [Authorize]/fallback policies.
            app.UseAuthentication();

            // After authentication, resolve the signed-in user's email to a Provider record and
            // stash it in HttpContext.Items so pages (e.g. _Layout) can display the provider's
            // display name without an extra DB lookup. If the user is authenticated but has no
            // matching active Provider (unknown or deactivated account), redirect them to
            // /AccessDenied instead of letting them reach the app. Public/anonymous pages
            // (Index, sign-in/out, AccessDenied itself) are exempt from the redirect so users
            // always have a way to see this message and sign out.
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path;
                var isPublicPath = path.StartsWithSegments("/AccessDenied")
                    || path.StartsWithSegments("/MicrosoftIdentity")
                    || path.StartsWithSegments("/signin-oidc")
                    || path.StartsWithSegments("/signout-callback-oidc")
                    || path == "/" || path == "";

                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var email = context.User.Identity.Name;
                    var roleResolutionService = context.RequestServices.GetRequiredService<IRoleResolutionService>();
                    var provider = email is null ? null : await roleResolutionService.ResolveByEmailAsync(email);

                    if (provider is null && !isPublicPath)
                    {
                        context.Response.Redirect("/AccessDenied");
                        return;
                    }

                    context.Items["CurrentProvider"] = provider;
                }

                await next();
            });

            // Adds authorization middleware to the request pipeline.
            // Keep app.UseAuthorization() before app.MapControllers() so authorization runs before
            // controller actions execute. What it does (clearly, step?by?step):
            // - Reads endpoint metadata (added by attributes like [Authorize] or by endpoint configuration).
            // - Evaluates the authorization policies/requirements (roles, claims, policy names).
            // - If the request is not allowed, it short?circuits and returns a challenge/forbidden response (typically 401 or 403) instead of executing the controller action.
            // - If the request is allowed, the pipeline continues to the action.
            // - Important: authentication vs authorization
            // - Authentication = proving who the caller is (sign-in, tokens). Done by AddAuthentication(...) and app.UseAuthentication().
            // - Authorization = deciding whether that authenticated identity can do something. Done by AddAuthorization(...), attributes/policies, and app.UseAuthorization().
            app.UseAuthorization();

            // Activate endpoint mapping for Controllers registered with builder.Services.AddControllers();
            // This maps incoming HTTP requests to the appropriate controller actions within the NotesController.cs class
            // If the request is a GET() with no kwargs, the it will search for a GET() with no args within NotesController.cs
            // and execute the code there. If the request is a GET() with an id, it will search for a GET() with an id arg within
            // NotesController.cs and execute the code there.
            app.MapControllers();

            // Maps Razor Pages endpoints (the provider/admin workspace UI, including the
            // Microsoft.Identity.Web sign-in/sign-out pages under /MicrosoftIdentity/Account/*).
            app.MapRazorPages();

            app.Run();
        }

        /// <summary>
        /// Registers the OpenAI client with the specified parameters.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <param name="openAIServiceEndpointUri">The OpenAI service endpoint URI.</param>
        /// <param name="apiKeyCredential">The API key credential.</param>
        /// <param name="deploymentName">The deployment name.</param>
        private static void RegisterOpenAIClient(WebApplicationBuilder builder,
                                             Uri openAIServiceEndpointUri,
                                             AzureKeyCredential apiKeyCredential,
                                             string deploymentName)
        {
            // Register the OpenAI client as a singleton service
            // A singleton service is created once and shared throughout the application's lifetime
            builder.Services.AddSingleton<IChatClient>(services =>
            {
                var azureOpenAiClient = new AzureOpenAIClient(openAIServiceEndpointUri, apiKeyCredential);
                var chatClient = azureOpenAiClient.GetChatClient(deploymentName).AsIChatClient();

                // Wrap with function-invocation middleware so tool calls (e.g. the patient
                // history lookup used by NoteGenerationService) are automatically executed and
                // fed back to the model, rather than just returned as unexecuted tool-call requests.
                return new ChatClientBuilder(chatClient)
                    .UseFunctionInvocation()
                    .Build();
            });
        }
    }

    /// <summary>
    /// Provides global settings for the application.
    /// </summary>
    public static class GlobalSettings
    {
        /// <summary>
        /// The date and time when the server was started.
        /// </summary>
        public static DateTime ServerStartDateTime = DateTime.UtcNow;
    }
}

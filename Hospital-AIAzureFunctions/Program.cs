using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Register BlobServiceClient using the AzureWebJobsStorage connection string.
// This is the same storage account that holds the queue, so no additional
// credential configuration is required for the core requirements.
builder.Services.AddSingleton(sp =>
{
    string? connectionString = builder.Configuration["AzureWebJobsStorage"];
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "AzureWebJobsStorage is not configured. "
            + "Set the connection string in local.settings.json or App Service Application Settings.");
    }
    return new BlobServiceClient(connectionString);
});

builder.Build().Run();

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Hospital_AIAzureFunctions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace Hospital_AIAzureFunctions;

/// <summary>
/// Azure Function that listens to the <c>attachment-zip-requests</c> queue and compresses
/// all attachments for a note into a single zip archive stored in Azure Blob Storage.
/// The resulting blob is placed in a container named <c>{noteId}-zip</c> with a blob
/// name equal to the <c>zipFileId</c> provided in the queue message.
/// </summary>
public class AttachmentZipFunction
{
    /// <summary>The logger instance.</summary>
    private readonly ILogger<AttachmentZipFunction> _logger;

    /// <summary>The Azure Blob Storage service client.</summary>
    private readonly BlobServiceClient _blobServiceClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttachmentZipFunction"/> class.
    /// </summary>
    /// <param name="logger">The logger instance injected by the Functions runtime.</param>
    /// <param name="blobServiceClient">The blob service client registered in <c>Program.cs</c>.</param>
    public AttachmentZipFunction(ILogger<AttachmentZipFunction> logger, BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }

    /// <summary>
    /// Queue-triggered entry point. Dequeues a <see cref="ZipRequestMessage"/>, reads all
    /// attachment blobs for the referenced note, compresses them into a single zip archive,
    /// and uploads the archive to the <c>{noteId}-zip</c> blob container.
    /// </summary>
    /// <param name="message">
    /// The deserialized message from the <c>attachment-zip-requests</c> queue containing
    /// the <c>NoteId</c> and the target <c>ZipFileId</c>.
    /// </param>
    [Function("AttachmentZipFunction")]
    public async Task Run(
        [QueueTrigger("attachment-zip-requests", Connection = "AzureWebJobsStorage")] ZipRequestMessage message)
    {
        string noteId           = message.NoteId;
        string zipFileId        = message.ZipFileId;
        string attachmentContainerName = noteId.ToLower();
        string zipContainerName        = $"{noteId.ToLower()}-zip";

        _logger.LogInformation(
            "Processing zip request: zipFileId='{ZipFileId}' for note='{NoteId}'.",
            zipFileId, noteId);

        // Get the attachment container (container name == noteId)
        var attachmentContainerClient = _blobServiceClient.GetBlobContainerClient(attachmentContainerName);

        // 1.1.4 – Race condition: note was deleted after the message was enqueued.
        // Log the error and return without retrying (the note is gone so retrying is pointless).
        if (!await attachmentContainerClient.ExistsAsync())
        {
            _logger.LogError(
                "The note {NoteId} can't be found for the requested compression operation.",
                noteId);
            return;
        }

        // 3.1 – Create the zip container ({noteId}-zip) if it does not already exist.
        var zipContainerClient = _blobServiceClient.GetBlobContainerClient(zipContainerName);
        await zipContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        try
        {
            // 3.2 – Build the zip archive entirely in memory before uploading.
            // leaveOpen: true keeps the MemoryStream open after the ZipArchive is disposed
            // so the stream can be rewound and uploaded.
            using var memoryStream = new MemoryStream();

            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Iterate every attachment blob in the note's container.
                await foreach (var blobItem in attachmentContainerClient.GetBlobsAsync())
                {
                    var blobClient = attachmentContainerClient.GetBlobClient(blobItem.Name);

                    // Stream the blob content directly into a new zip entry.
                    using var blobStream = await blobClient.OpenReadAsync();
                    var entry = zipArchive.CreateEntry(blobItem.Name, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await blobStream.CopyToAsync(entryStream);
                }
            } // ZipArchive is fully flushed and finalised when disposed here.

            // Rewind before uploading.
            memoryStream.Position = 0;

            // Upload the completed zip as a single blob.
            var zipBlobClient = zipContainerClient.GetBlobClient(zipFileId);
            await zipBlobClient.UploadAsync(memoryStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/zip" }
            });

            // 3.2.1 – Log success.
            _logger.LogInformation(
                "Successfully created zip archive: zipFileId='{ZipFileId}' in container='{ContainerName}'.",
                zipFileId, zipContainerName);
        }
        catch (Exception ex)
        {
            // 3.2.2 – Log the failure with enough detail to diagnose.
            _logger.LogError(ex,
                "Failed to create zip archive: zipFileId='{ZipFileId}' intended container='{ContainerName}'.",
                zipFileId, zipContainerName);

            // 3.2.3 – Re-throw so the Functions runtime retries the message.
            // After maxDequeueCount attempts (default 5) the runtime automatically
            // moves the message to attachment-zip-requests-poison.
            throw;
        }
    }
}

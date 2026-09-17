using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzureStorageProject.Functions
{
    /// <summary>
    /// Function 2 of 4 — Blob Storage.
    /// HTTP-triggered function that accepts a multipart/form-data file
    /// (e.g. an order receipt or invoice) and writes it to the
    /// "transactionreceipts" blob container.
    /// Route: POST /api/UploadTransactionBlob
    /// Form fields expected: "file" (the document) and "orderId" (text).
    /// </summary>
    public class UploadTransactionBlobFunction
    {
        private const string ContainerName = "transactionreceipts";
        private readonly ILogger _logger;

        public UploadTransactionBlobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadTransactionBlobFunction>();
        }

        [Function("UploadTransactionBlob")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "UploadTransactionBlob")] HttpRequest req)
        {
            _logger.LogInformation("UploadTransactionBlob function triggered.");

            if (!req.HasFormContentType || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult(new { success = false, message = "No file was received. Send as multipart/form-data with a 'file' field." });
            }

            var file = req.Form.Files[0];
            string orderId = req.Form["orderId"].FirstOrDefault() ?? "UNKNOWN";

            string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? throw new InvalidOperationException("AzureStorageConnection app setting is not configured.");

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            string blobName = $"{orderId.Trim()}_{Guid.NewGuid()}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return new OkObjectResult(new
            {
                success = true,
                message = $"Receipt uploaded to Blob Storage container '{ContainerName}'.",
                blobUrl = blobClient.Uri.ToString(),
                blobName
            });
        }
    }
}

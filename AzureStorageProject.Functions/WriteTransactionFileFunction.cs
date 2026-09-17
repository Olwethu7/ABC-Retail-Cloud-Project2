using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Storage.Files.Shares;
using AzureStorageProject.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzureStorageProject.Functions
{
    /// <summary>
    /// Function 4 of 4 — Azure Files.
    /// HTTP-triggered function that writes a transaction log entry to a
    /// managed file share, used as a durable, human-readable audit trail
    /// that is independent of the Table/Blob/Queue services.
    /// Route: POST /api/WriteTransactionFile
    /// </summary>
    public class WriteTransactionFileFunction
    {
        private const string ShareName = "transactionlogs";
        private const string DirectoryName = "receipts";
        private readonly ILogger _logger;

        public WriteTransactionFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WriteTransactionFileFunction>();
        }

        [Function("WriteTransactionFile")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "WriteTransactionFile")] HttpRequestData req)
        {
            _logger.LogInformation("WriteTransactionFile function triggered.");

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<TransactionFileRequest>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null || string.IsNullOrWhiteSpace(request.OrderId))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(new { success = false, message = "OrderId is required." });
                return bad;
            }

            string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? throw new InvalidOperationException("AzureStorageConnection app setting is not configured.");

            var shareClient = new ShareClient(connectionString, ShareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetDirectoryClient(DirectoryName);
            await directoryClient.CreateIfNotExistsAsync();

            string fileName = $"{request.OrderId}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
            var fileClient = directoryClient.GetFileClient(fileName);

            string logText = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - Order {request.OrderId}{Environment.NewLine}{request.Content}{Environment.NewLine}";
            byte[] bytes = Encoding.UTF8.GetBytes(logText);

            using (var stream = new MemoryStream(bytes))
            {
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = $"Transaction log written to Azure Files share '{ShareName}/{DirectoryName}/{fileName}'.",
                fileName
            });
            return response;
        }
    }
}

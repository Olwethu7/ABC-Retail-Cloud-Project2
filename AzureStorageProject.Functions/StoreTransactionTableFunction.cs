using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using AzureStorageProject.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzureStorageProject.Functions
{
    /// <summary>
    /// Function 1 of 4 — Table Storage.
    /// HTTP-triggered function that stores a transaction record in the
    /// "Transactions" Azure Table. Called by the ABC Retail web app whenever
    /// an order is confirmed, keeping a durable, queryable record of every
    /// transaction separate from the live Orders/Products tables.
    /// Route: POST /api/StoreTransactionTable
    /// </summary>
    public class StoreTransactionTableFunction
    {
        private const string TableName = "Transactions";
        private readonly ILogger _logger;

        public StoreTransactionTableFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StoreTransactionTableFunction>();
        }

        [Function("StoreTransactionTable")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "StoreTransactionTable")] HttpRequestData req)
        {
            _logger.LogInformation("StoreTransactionTable function triggered.");

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<TransactionRequest>(body, new JsonSerializerOptions
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

            var tableClient = new TableClient(connectionString, TableName);
            await tableClient.CreateIfNotExistsAsync();

            var entity = new TransactionRecord
            {
                PartitionKey = "Transaction",
                RowKey = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                OrderId = request.OrderId,
                CustomerName = request.CustomerName,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                Status = "Recorded"
            };

            await tableClient.AddEntityAsync(entity);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = $"Transaction {entity.RowKey} stored in Azure Table Storage.",
                partitionKey = entity.PartitionKey,
                rowKey = entity.RowKey
            });
            return response;
        }
    }
}

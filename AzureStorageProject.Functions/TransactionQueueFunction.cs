using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using AzureStorageProject.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzureStorageProject.Functions
{
    /// <summary>
    /// Function 3 of 4 — Queue Storage.
    /// A single HTTP-triggered function that both writes transaction events
    /// to, and reads/processes them from, the "transactionqueue" Azure Queue.
    /// Route: POST /api/TransactionQueue?action=write   (enqueue a transaction event)
    ///        POST /api/TransactionQueue?action=read    (dequeue + delete pending events)
    /// </summary>
    public class TransactionQueueFunction
    {
        private const string QueueName = "transactionqueue";
        private readonly ILogger _logger;

        public TransactionQueueFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TransactionQueueFunction>();
        }

        [Function("TransactionQueue")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "TransactionQueue")] HttpRequestData req)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string action = query["action"] ?? "write";

            string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? throw new InvalidOperationException("AzureStorageConnection app setting is not configured.");

            var queueClient = new QueueClient(connectionString, QueueName);
            await queueClient.CreateIfNotExistsAsync();

            if (action.Equals("read", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("TransactionQueue function triggered — reading messages.");

                var received = await queueClient.ReceiveMessagesAsync(maxMessages: 10);
                var results = new List<object>();

                foreach (var msg in received.Value)
                {
                    results.Add(new { msg.MessageId, msg.Body });
                    await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
                }

                var readResponse = req.CreateResponse(HttpStatusCode.OK);
                await readResponse.WriteAsJsonAsync(new
                {
                    success = true,
                    message = $"Read and removed {results.Count} message(s) from '{QueueName}'.",
                    messages = results
                });
                return readResponse;
            }
            else
            {
                _logger.LogInformation("TransactionQueue function triggered — writing a message.");

                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonSerializer.Deserialize<TransactionQueueMessage>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new TransactionQueueMessage();

                string message = JsonSerializer.Serialize(request);
                await queueClient.SendMessageAsync(message);

                var writeResponse = req.CreateResponse(HttpStatusCode.OK);
                await writeResponse.WriteAsJsonAsync(new
                {
                    success = true,
                    message = $"Transaction event for order {request.OrderId} written to queue '{QueueName}'."
                });
                return writeResponse;
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using AzureStorageProject.Models;
using AzureStorageProject.Services;
using System.Text.Json;

namespace AzureStorageProject.Controllers
{
    public class OrderController : Controller
    {
        private readonly IStorageService _storageService;
        private const string QUEUE_NAME = "orderqueue";
        private readonly string _connectionString;

        public OrderController(IStorageService storageService, IConfiguration configuration)
        {
            _storageService = storageService;
            _connectionString = configuration.GetConnectionString("AzureStorageConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        // GET: Order/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderMessage order)
        {
            if (ModelState.IsValid)
            {
                // Generate a unique Order ID
                order.OrderId = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                order.Action = "ProcessOrder";

                // Serialize the order to JSON and send it to the queue
                string message = JsonSerializer.Serialize(order);
                await _storageService.SendMessageAsync(QUEUE_NAME, message);

                ViewBag.Message = $"✅ Order {order.OrderId} has been queued for processing!";
                ViewBag.OrderId = order.OrderId;
                return View("OrderPlaced");
            }
            return View(order);
        }

        // GET: Order/ProcessQueue
        public async Task<IActionResult> ProcessQueue()
        {
            try
            {
                // Get actual message count from queue
                var queueClient = new Azure.Storage.Queues.QueueClient(
                    _connectionString,
                    QUEUE_NAME
                );
                await queueClient.CreateIfNotExistsAsync();
                var properties = await queueClient.GetPropertiesAsync();
                var messageCount = properties.Value.ApproximateMessagesCount;

                var messages = await _storageService.ReceiveMessagesAsync(QUEUE_NAME, maxMessages: 10);
                var processedMessages = new List<string>();

                foreach (var message in messages)
                {
                    try
                    {
                        var order = JsonSerializer.Deserialize<OrderMessage>(message);
                        if (order != null)
                        {
                            processedMessages.Add($"✅ Processed: {order.OrderId} - {order.CustomerName} (Qty: {order.Quantity})");

                            // In a real app, you would process the order here
                            await LogOrderProcessingAsync(order);
                        }
                    }
                    catch (Exception ex)
                    {
                        processedMessages.Add($"❌ Error processing message: {ex.Message}");
                    }
                }

                ViewBag.ProcessedMessages = processedMessages;
                ViewBag.MessageCount = messageCount;

                if (!processedMessages.Any())
                {
                    ViewBag.Message = "No messages in the queue to process.";
                }
                else
                {
                    ViewBag.Message = $"✅ Processed {processedMessages.Count} message(s) from the queue.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.ProcessedMessages = new List<string>();
                ViewBag.MessageCount = 0;
                ViewBag.Message = $"❌ Error accessing queue: {ex.Message}";
            }

            return View();
        }

        // Helper method to log order processing
        private async Task LogOrderProcessingAsync(OrderMessage order)
        {
            string logFileName = $"order-log-{DateTime.Now:yyyy-MM-dd}.txt";
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Order {order.OrderId} processed for {order.CustomerName} (Product: {order.ProductId}, Qty: {order.Quantity}){Environment.NewLine}";

            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(logEntry)))
            {
                // Upload to Azure Files
                await _storageService.UploadFileAsync("logs", "order-logs", logFileName, stream);
            }
        }
    }
}
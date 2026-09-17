using Microsoft.AspNetCore.Mvc;
using AzureStorageProject.Models;
using AzureStorageProject.Services;

namespace AzureStorageProject.Controllers
{
    /// <summary>
    /// Project 2 — demonstrates integrating four Azure Functions into the
    /// application's architecture. Each action on this controller calls one
    /// (or more) of the four HTTP-triggered Azure Functions rather than the
    /// storage SDK directly, showing how the workload can be offloaded to a
    /// serverless, independently-scalable layer.
    /// </summary>
    public class TransactionController : Controller
    {
        private readonly IFunctionsClientService _functionsClient;

        public TransactionController(IFunctionsClientService functionsClient)
        {
            _functionsClient = functionsClient;
        }

        // GET: Transaction/Create
        public IActionResult Create()
        {
            return View(new TransactionViewModel());
        }

        // POST: Transaction/Create
        // Calls Function 1 (Table), Function 2 (Blob, if a receipt was attached),
        // Function 3 (Queue) and Function 4 (Files) in sequence.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var results = new List<TransactionResultViewModel>();

            // Function 1 — Azure Table Storage
            var tableResult = await _functionsClient.StoreTransactionInTableAsync(
                model.OrderId, model.CustomerName, model.ProductId, model.Quantity);
            results.Add(new TransactionResultViewModel
            {
                StepName = "Store transaction record",
                AzureService = "Azure Table Storage",
                Success = tableResult.Success,
                Message = tableResult.Message
            });

            // Function 2 — Azure Blob Storage (only if a receipt file was supplied)
            if (model.Receipt != null && model.Receipt.Length > 0)
            {
                var blobResult = await _functionsClient.UploadTransactionReceiptAsync(model.OrderId, model.Receipt);
                results.Add(new TransactionResultViewModel
                {
                    StepName = "Upload transaction receipt",
                    AzureService = "Azure Blob Storage",
                    Success = blobResult.Success,
                    Message = blobResult.Message
                });
            }

            // Function 3 — Azure Queue Storage (write)
            var queueResult = await _functionsClient.WriteTransactionToQueueAsync(
                model.OrderId, model.CustomerName, "TransactionRecorded");
            results.Add(new TransactionResultViewModel
            {
                StepName = "Queue transaction event",
                AzureService = "Azure Queue Storage",
                Success = queueResult.Success,
                Message = queueResult.Message
            });

            // Function 4 — Azure Files
            string logContent = $"Product: {model.ProductId}, Quantity: {model.Quantity}, Customer: {model.CustomerName}";
            var fileResult = await _functionsClient.WriteTransactionFileAsync(model.OrderId, logContent);
            results.Add(new TransactionResultViewModel
            {
                StepName = "Write transaction log file",
                AzureService = "Azure Files",
                Success = fileResult.Success,
                Message = fileResult.Message
            });

            ViewBag.OrderId = model.OrderId;
            return View("Results", results);
        }

        // GET: Transaction/ProcessQueue
        // Calls Function 3 (Queue) in "read" mode to drain pending transaction events.
        public async Task<IActionResult> ProcessQueue()
        {
            var result = await _functionsClient.ReadTransactionQueueAsync();
            ViewBag.Success = result.Success;
            ViewBag.Message = result.Message;
            ViewBag.RawResponse = result.RawResponse;
            return View();
        }
    }
}

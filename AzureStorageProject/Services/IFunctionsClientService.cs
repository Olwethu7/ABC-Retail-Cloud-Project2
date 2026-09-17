using Microsoft.AspNetCore.Http;

namespace AzureStorageProject.Services
{
    /// <summary>
    /// Client used by the web application to call the four Azure Functions
    /// created for Project 2 (Table, Blob, Queue, Files). Keeping this behind
    /// an interface lets the app call either a locally-running Functions host
    /// (http://localhost:7071) or the deployed Function App, based purely on
    /// the "FunctionsBaseUrl" setting in appsettings.json.
    /// </summary>
    public interface IFunctionsClientService
    {
        Task<FunctionCallResult> StoreTransactionInTableAsync(string orderId, string customerName, string productId, int quantity);
        Task<FunctionCallResult> UploadTransactionReceiptAsync(string orderId, IFormFile file);
        Task<FunctionCallResult> WriteTransactionToQueueAsync(string orderId, string customerName, string eventType);
        Task<FunctionCallResult> ReadTransactionQueueAsync();
        Task<FunctionCallResult> WriteTransactionFileAsync(string orderId, string content);
    }

    public class FunctionCallResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RawResponse { get; set; } = string.Empty;
    }
}

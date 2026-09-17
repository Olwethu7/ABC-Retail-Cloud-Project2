using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzureStorageProject.Services
{
    public class FunctionsClientService : IFunctionsClientService
    {
        private readonly HttpClient _httpClient;
        private readonly string _functionsBaseUrl;
        private readonly string _functionsKey;
        private readonly ILogger<FunctionsClientService> _logger;

        public FunctionsClientService(HttpClient httpClient, IConfiguration configuration, ILogger<FunctionsClientService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _functionsBaseUrl = configuration["FunctionsSettings:BaseUrl"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("FunctionsSettings:BaseUrl is not configured in appsettings.json.");
            _functionsKey = configuration["FunctionsSettings:FunctionKey"] ?? string.Empty;
        }

        private string BuildUrl(string functionName, string? extraQuery = null)
        {
            var url = $"{_functionsBaseUrl}/api/{functionName}";
            var query = new List<string>();

            if (!string.IsNullOrEmpty(_functionsKey))
                query.Add($"code={_functionsKey}");
            if (!string.IsNullOrEmpty(extraQuery))
                query.Add(extraQuery);

            if (query.Count > 0)
                url += "?" + string.Join("&", query);

            return url;
        }

        private async Task<FunctionCallResult> PostJsonAsync(string functionName, object payload, string? extraQuery = null)
        {
            try
            {
                var url = BuildUrl(functionName, extraQuery);
                var response = await _httpClient.PostAsJsonAsync(url, payload);
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Function {FunctionName} returned {StatusCode}: {Body}", functionName, response.StatusCode, raw);
                    return new FunctionCallResult { Success = false, Message = $"Function call failed ({(int)response.StatusCode}).", RawResponse = raw };
                }

                using var doc = JsonDocument.Parse(raw);
                string message = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : "Success.";

                return new FunctionCallResult { Success = true, Message = message, RawResponse = raw };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling function {FunctionName}", functionName);
                return new FunctionCallResult { Success = false, Message = $"Could not reach the {functionName} function: {ex.Message}" };
            }
        }

        public Task<FunctionCallResult> StoreTransactionInTableAsync(string orderId, string customerName, string productId, int quantity)
        {
            var payload = new { OrderId = orderId, CustomerName = customerName, ProductId = productId, Quantity = quantity };
            return PostJsonAsync("StoreTransactionTable", payload);
        }

        public async Task<FunctionCallResult> UploadTransactionReceiptAsync(string orderId, IFormFile file)
        {
            try
            {
                var url = BuildUrl("UploadTransactionBlob");

                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);

                content.Add(streamContent, "file", file.FileName);
                content.Add(new StringContent(orderId), "orderId");

                var response = await _httpClient.PostAsync(url, content);
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new FunctionCallResult { Success = false, Message = $"Blob function call failed ({(int)response.StatusCode}).", RawResponse = raw };

                using var doc = JsonDocument.Parse(raw);
                string message = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : "Uploaded.";

                return new FunctionCallResult { Success = true, Message = message, RawResponse = raw };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling UploadTransactionBlob function");
                return new FunctionCallResult { Success = false, Message = $"Could not reach the UploadTransactionBlob function: {ex.Message}" };
            }
        }

        public Task<FunctionCallResult> WriteTransactionToQueueAsync(string orderId, string customerName, string eventType)
        {
            var payload = new { OrderId = orderId, CustomerName = customerName, EventType = eventType, Timestamp = DateTimeOffset.UtcNow };
            return PostJsonAsync("TransactionQueue", payload, "action=write");
        }

        public async Task<FunctionCallResult> ReadTransactionQueueAsync()
        {
            try
            {
                var url = BuildUrl("TransactionQueue", "action=read");
                var response = await _httpClient.PostAsync(url, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new FunctionCallResult { Success = false, Message = $"Queue read failed ({(int)response.StatusCode}).", RawResponse = raw };

                using var doc = JsonDocument.Parse(raw);
                string message = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : "Read complete.";

                return new FunctionCallResult { Success = true, Message = message, RawResponse = raw };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling TransactionQueue (read) function");
                return new FunctionCallResult { Success = false, Message = $"Could not reach the TransactionQueue function: {ex.Message}" };
            }
        }

        public Task<FunctionCallResult> WriteTransactionFileAsync(string orderId, string content)
        {
            var payload = new { OrderId = orderId, Content = content };
            return PostJsonAsync("WriteTransactionFile", payload);
        }
    }
}

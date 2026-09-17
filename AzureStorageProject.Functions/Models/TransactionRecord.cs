using Azure;
using Azure.Data.Tables;

namespace AzureStorageProject.Functions.Models
{
    /// <summary>
    /// Azure Table entity representing a single retail transaction
    /// (an order that has moved from "queued" to "processed").
    /// </summary>
    public class TransactionRecord : ITableEntity
    {
        public string PartitionKey { get; set; } = "Transaction";
        public string RowKey { get; set; } = string.Empty;

        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = "Recorded";
        public string ReceiptBlobUrl { get; set; } = string.Empty;

        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }

    /// <summary>Request body used by StoreTransactionTableFunction.</summary>
    public class TransactionRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    /// <summary>Request body used by TransactionQueueFunction when writing to the queue.</summary>
    public class TransactionQueueMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string EventType { get; set; } = "TransactionRecorded";
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>Request body used by WriteTransactionFileFunction.</summary>
    public class TransactionFileRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}

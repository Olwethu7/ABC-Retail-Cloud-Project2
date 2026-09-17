using Azure;
using Azure.Data.Tables;

namespace AzureStorageProject.Models
{
    public class ProductInfo : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Price { get; set; } = "0.00";
        public string ImageUrl { get; set; } = string.Empty;

        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}
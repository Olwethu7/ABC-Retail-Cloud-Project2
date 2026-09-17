using Azure.Data.Tables;

namespace AzureStorageProject.Services
{
    public interface IStorageService
    {
        // Tables
        Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity;
        Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity;
        Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new();
        Task UpdateEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity;
        Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey);

        // Blobs
        Task<string> UploadBlobAsync(string containerName, string fileName, Stream fileStream);
        Task<Stream?> DownloadBlobAsync(string containerName, string fileName);
        Task DeleteBlobAsync(string containerName, string fileName);
        Task<IEnumerable<string>> ListBlobsAsync(string containerName);

        // Queues
        Task SendMessageAsync(string queueName, string message);
        Task<IEnumerable<string>> ReceiveMessagesAsync(string queueName, int maxMessages = 10);
        Task DeleteMessageAsync(string queueName, string messageId, string popReceipt);

        // Files
        Task UploadFileAsync(string shareName, string directoryName, string fileName, Stream fileStream);
        Task<Stream?> DownloadFileAsync(string shareName, string directoryName, string fileName);
        Task<IEnumerable<string>> ListFilesAsync(string shareName, string directoryName);
    }
}
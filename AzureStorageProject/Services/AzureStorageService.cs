using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;

namespace AzureStorageProject.Services
{
    public class AzureStorageService : IStorageService
    {
        private readonly string _connectionString;

        public AzureStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorageConnection")
                ?? throw new InvalidOperationException("Connection string 'AzureStorageConnection' not found.");
        }

        // ============================================================
        //  TABLES
        // ============================================================

        public async Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity
        {
            var tableClient = new TableClient(_connectionString, tableName);
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(entity);
        }

        public async Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity
        {
            var tableClient = new TableClient(_connectionString, tableName);
            try
            {
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new()
        {
            var tableClient = new TableClient(_connectionString, tableName);
            await tableClient.CreateIfNotExistsAsync();

            var entities = new List<T>();
            await foreach (var entity in tableClient.QueryAsync<T>())
            {
                entities.Add(entity);
            }
            return entities;
        }

        public async Task UpdateEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity
        {
            var tableClient = new TableClient(_connectionString, tableName);
            await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
        }

        public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
        {
            var tableClient = new TableClient(_connectionString, tableName);
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // ============================================================
        //  BLOBS
        // ============================================================

        public async Task<string> UploadBlobAsync(string containerName, string fileName, Stream fileStream)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task<Stream?> DownloadBlobAsync(string containerName, string fileName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            try
            {
                var response = await blobClient.DownloadStreamingAsync();
                return response.Value.Content;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task DeleteBlobAsync(string containerName, string fileName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<IEnumerable<string>> ListBlobsAsync(string containerName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobs = new List<string>();
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
            }
            return blobs;
        }

        // ============================================================
        //  QUEUES
        // ============================================================

        public async Task SendMessageAsync(string queueName, string message)
        {
            var queueClient = new QueueClient(_connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(message);
        }

        public async Task<IEnumerable<string>> ReceiveMessagesAsync(string queueName, int maxMessages = 10)
        {
            var queueClient = new QueueClient(_connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();

            var messages = new List<string>();
            var response = await queueClient.ReceiveMessagesAsync(maxMessages: maxMessages, visibilityTimeout: TimeSpan.FromSeconds(30));

            foreach (var message in response.Value)
            {
                messages.Add(message.Body.ToString());
            }

            return messages;
        }

        public async Task DeleteMessageAsync(string queueName, string messageId, string popReceipt)
        {
            var queueClient = new QueueClient(_connectionString, queueName);
            await queueClient.DeleteMessageAsync(messageId, popReceipt);
        }

        // ============================================================
        //  FILES
        // ============================================================

        public async Task UploadFileAsync(string shareName, string directoryName, string fileName, Stream fileStream)
        {
            var shareClient = new ShareClient(_connectionString, shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            var fileClient = directoryClient.GetFileClient(fileName);

            fileStream.Position = 0;
            await fileClient.CreateAsync(fileStream.Length);
            await fileClient.UploadRangeAsync(new HttpRange(0, fileStream.Length), fileStream);
        }

        public async Task<Stream?> DownloadFileAsync(string shareName, string directoryName, string fileName)
        {
            var shareClient = new ShareClient(_connectionString, shareName);
            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            var fileClient = directoryClient.GetFileClient(fileName);

            try
            {
                var response = await fileClient.DownloadAsync();
                return response.Value.Content;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<IEnumerable<string>> ListFilesAsync(string shareName, string directoryName)
        {
            var shareClient = new ShareClient(_connectionString, shareName);
            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            var files = new List<string>();
            await foreach (var fileItem in directoryClient.GetFilesAndDirectoriesAsync())
            {
                files.Add(fileItem.Name);
            }
            return files;
        }
    }
}
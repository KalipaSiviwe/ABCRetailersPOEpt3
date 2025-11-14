using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Functions.Services
{
    public class AzureStorageService : IAzureStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueServiceClient _queueServiceClient;

        public AzureStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorageConnectionString"] ?? "UseDevelopmentStorage=true";
            _tableServiceClient = new TableServiceClient(connectionString);
            _queueServiceClient = new QueueServiceClient(connectionString);
        }

        public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string? filter = null) where T : class, ITableEntity, new()
        {
            var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
            var entities = new List<T>();

            await foreach (var entity in tableClient.QueryAsync<T>(filter))
            {
                entities.Add(entity);
            }

            return entities;
        }

        public async Task AddEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
            await tableClient.AddEntityAsync(entity);
        }

        public async Task UpdateEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
            await tableClient.UpdateEntityAsync(entity, entity.ETag);
        }

        public async Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity
        {
            var tableClient = _tableServiceClient.GetTableClient(GetTableName<T>());
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task SendMessageAsync(string queueName, string message)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(message);
        }

        public async Task<string?> ReceiveMessageAsync(string queueName)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            var response = await queueClient.ReceiveMessageAsync();
            return response.Value?.MessageText;
        }

        private static string GetTableName<T>()
        {
            return typeof(T).Name switch
            {
                "Product" => "Products",
                "Customer" => "Customers",
                "Order" => "Orders",
                _ => typeof(T).Name + "s"
            };
        }
    }
}
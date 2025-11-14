using Azure.Data.Tables;
using Azure.Storage.Queues;

namespace ABCRetailers.Functions.Services
{
    public interface IAzureStorageService
    {
        Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();
        Task<IEnumerable<T>> GetAllEntitiesAsync<T>(string? filter = null) where T : class, ITableEntity, new();
        Task AddEntityAsync<T>(T entity) where T : class, ITableEntity;
        Task UpdateEntityAsync<T>(T entity) where T : class, ITableEntity;
        Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity;
        Task SendMessageAsync(string queueName, string message);
        Task<string?> ReceiveMessageAsync(string queueName);
    }
}
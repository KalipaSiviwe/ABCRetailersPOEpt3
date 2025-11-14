using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public interface IAzureFunctionsService
    {
        Task<bool> ProcessOrderAsync(string orderId, string action);
        Task<OrderStatusResponse?> GetOrderStatusAsync(string orderId);
        Task<bool> UpdateStockAsync(string productId, int newStock, string updatedBy, string reason);
        Task<List<LowStockProduct>> GetLowStockProductsAsync(int threshold = 10);
        Task<StockHistoryResponse?> GetStockHistoryAsync(string productId);
        Task<string?> UploadProductImageAsync(Stream fileStream, string fileName, string contentType);
        Task<string?> UploadContractAsync(Stream fileStream, string fileName);
        Task<List<ProductImage>> GetProductImagesAsync();
        Task<List<ContractFile>> GetContractsAsync();
        Task<bool> DeleteFileAsync(string fileType, string fileName);
    }

    public class OrderStatusResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class LowStockProduct
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public double Price { get; set; }
    }

    public class StockHistoryResponse
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public DateTimeOffset? LastUpdated { get; set; }
    }

    public class ProductImage
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }

    public class ContractFile
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
}

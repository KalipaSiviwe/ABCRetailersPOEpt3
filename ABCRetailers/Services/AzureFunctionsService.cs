using System.Text;
using System.Text.Json;
using ABCRetailers.Models;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Services
{
    public class AzureFunctionsService : IAzureFunctionsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureFunctionsService> _logger;

        public AzureFunctionsService(IHttpClientFactory httpClientFactory, ILogger<AzureFunctionsService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("AzureFunctions");
            _logger = logger;
        }

        public async Task<bool> ProcessOrderAsync(string orderId, string action)
        {
            try
            {
                var request = new { OrderId = orderId, Action = action };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/orders/process", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order {OrderId} with action {Action}", orderId, action);
                return false;
            }
        }

        public async Task<OrderStatusResponse?> GetOrderStatusAsync(string orderId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/orders/{orderId}/status");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<OrderStatusResponse>(json);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status for {OrderId}", orderId);
                return null;
            }
        }

        public async Task<bool> UpdateStockAsync(string productId, int newStock, string updatedBy, string reason)
        {
            try
            {
                var request = new { ProductId = productId, NewStock = newStock, UpdatedBy = updatedBy, Reason = reason };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/stock/update", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
                return false;
            }
        }

        public async Task<List<LowStockProduct>> GetLowStockProductsAsync(int threshold = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/stock/low?threshold={threshold}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LowStockResponse>(json);
                    return result?.Products ?? new List<LowStockProduct>();
                }
                return new List<LowStockProduct>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock products");
                return new List<LowStockProduct>();
            }
        }

        public async Task<StockHistoryResponse?> GetStockHistoryAsync(string productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/stock/history/{productId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<StockHistoryResponse>(json);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock history for product {ProductId}", productId);
                return null;
            }
        }

        public async Task<string?> UploadProductImageAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                using var formData = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                formData.Add(streamContent, "file", fileName);

                var response = await _httpClient.PostAsync("/api/files/upload/image", formData);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<FileUploadResponse>(json);
                    return result?.FileUrl;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image {FileName}", fileName);
                return null;
            }
        }

        public async Task<string?> UploadContractAsync(Stream fileStream, string fileName)
        {
            try
            {
                using var formData = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);
                formData.Add(streamContent, "file", fileName);

                var response = await _httpClient.PostAsync("/api/files/upload/contract", formData);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<FileUploadResponse>(json);
                    return result?.FileUrl;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading contract {FileName}", fileName);
                return null;
            }
        }

        public async Task<List<ProductImage>> GetProductImagesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/files/images");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ImageListResponse>(json);
                    return result?.Images ?? new List<ProductImage>();
                }
                return new List<ProductImage>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product images");
                return new List<ProductImage>();
            }
        }

        public async Task<List<ContractFile>> GetContractsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/files/contracts");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ContractListResponse>(json);
                    return result?.Contracts ?? new List<ContractFile>();
                }
                return new List<ContractFile>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contracts");
                return new List<ContractFile>();
            }
        }

        public async Task<bool> DeleteFileAsync(string fileType, string fileName)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/files/{fileType}/{fileName}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName} of type {FileType}", fileName, fileType);
                return false;
            }
        }
    }

    // Response models
    public class LowStockResponse
    {
        public bool Success { get; set; }
        public int Threshold { get; set; }
        public int Count { get; set; }
        public List<LowStockProduct> Products { get; set; } = new();
    }

    public class FileUploadResponse
    {
        public bool Success { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ImageListResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<ProductImage> Images { get; set; } = new();
    }

    public class ContractListResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public List<ContractFile> Contracts { get; set; } = new();
    }
}

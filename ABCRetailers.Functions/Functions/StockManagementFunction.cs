using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using ABCRetailers.Functions.Services;
using Microsoft.Azure.Functions.Worker.Extensions.Http;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Functions
{
    public class StockManagementFunction
    {
        private readonly ILogger<StockManagementFunction> _logger;
        private readonly IAzureStorageService _storageService;

        public StockManagementFunction(ILogger<StockManagementFunction> logger, IAzureStorageService storageService)
        {
            _logger = logger;
            _storageService = storageService;
        }

        [Function("UpdateStock")]
        public async Task<HttpResponseData> UpdateStock(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "stock/update")] HttpRequestData req)
        {
            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var stockUpdate = JsonSerializer.Deserialize<StockUpdateRequest>(requestBody);

                if (stockUpdate == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid stock update data");
                    return badRequestResponse;
                }

                // Get the product
                var product = await _storageService.GetEntityAsync<Product>("Product", stockUpdate.ProductId);
                if (product == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("Product not found");
                    return notFoundResponse;
                }

                // Update stock
                var previousStock = product.StockAvailable;
                product.StockAvailable = stockUpdate.NewStock;
                await _storageService.UpdateEntityAsync(product);

                // Send stock update notification
                var stockMessage = new
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    PreviousStock = previousStock,
                    NewStock = product.StockAvailable,
                    UpdatedBy = stockUpdate.UpdatedBy,
                    UpdateDate = DateTime.UtcNow,
                    Reason = stockUpdate.Reason
                };
                await _storageService.SendMessageAsync("stock-updates", JsonSerializer.Serialize(stockMessage));

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Stock updated successfully",
                    productId = product.ProductId,
                    previousStock = previousStock,
                    newStock = product.StockAvailable
                }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal server error");
                return errorResponse;
            }
        }

        [Function("GetLowStockProducts")]
        public async Task<HttpResponseData> GetLowStockProducts(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stock/low")] HttpRequestData req)
        {
            try
            {
                var threshold = int.Parse(req.Query["threshold"] ?? "10");
                var products = await _storageService.GetAllEntitiesAsync<Product>();
                var lowStockProducts = products.Where(p => p.StockAvailable <= threshold).ToList();

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = true,
                    threshold = threshold,
                    count = lowStockProducts.Count,
                    products = lowStockProducts.Select(p => new {
                        productId = p.ProductId,
                        productName = p.ProductName,
                        currentStock = p.StockAvailable,
                        price = p.Price
                    })
                }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock products");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal server error");
                return errorResponse;
            }
        }

        [Function("GetStockHistory")]
        public async Task<HttpResponseData> GetStockHistory(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stock/history/{productId}")] HttpRequestData req,
            string productId)
        {
            try
            {
                // In a real implementation, you would have a separate StockHistory table
                // For now, we'll return the current product stock info
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("Product not found");
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = true,
                    productId = product.ProductId,
                    productName = product.ProductName,
                    currentStock = product.StockAvailable,
                    lastUpdated = product.Timestamp
                }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock history");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal server error");
                return errorResponse;
            }
        }
    }

    public class StockUpdateRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int NewStock { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
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
    public class OrderProcessingFunction
    {
        private readonly ILogger<OrderProcessingFunction> _logger;
        private readonly IAzureStorageService _storageService;

        public OrderProcessingFunction(ILogger<OrderProcessingFunction> logger, IAzureStorageService storageService)
        {
            _logger = logger;
            _storageService = storageService;
        }

        [Function("ProcessOrder")]
        public async Task<HttpResponseData> ProcessOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders/process")] HttpRequestData req)
        {
            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var orderData = JsonSerializer.Deserialize<OrderProcessRequest>(requestBody);

                if (orderData == null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid order data");
                    return badRequestResponse;
                }

                // Get the order from storage
                var order = await _storageService.GetEntityAsync<Order>("Order", orderData.OrderId);
                if (order == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("Order not found");
                    return notFoundResponse;
                }

                // Process the order based on status
                switch (orderData.Action.ToLower())
                {
                    case "approve":
                        order.Status = "Processing";
                        await _storageService.UpdateEntityAsync(order);

                        // Send notification
                        var notificationMessage = new
                        {
                            OrderId = order.OrderId,
                            CustomerId = order.CustomerId,
                            Status = "Processing",
                            Message = "Your order has been approved and is being processed",
                            Timestamp = DateTime.UtcNow
                        };
                        await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(notificationMessage));
                        break;

                    case "complete":
                        order.Status = "Completed";
                        await _storageService.UpdateEntityAsync(order);

                        // Send completion notification
                        var completionMessage = new
                        {
                            OrderId = order.OrderId,
                            CustomerId = order.CustomerId,
                            Status = "Completed",
                            Message = "Your order has been completed and delivered",
                            Timestamp = DateTime.UtcNow
                        };
                        await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(completionMessage));
                        break;

                    case "cancel":
                        order.Status = "Cancelled";
                        await _storageService.UpdateEntityAsync(order);

                        // Restore stock
                        var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                        if (product != null)
                        {
                            product.StockAvailable += order.Quantity;
                            await _storageService.UpdateEntityAsync(product);
                        }
                        break;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { success = true, message = "Order processed successfully" }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal server error");
                return errorResponse;
            }
        }

        [Function("GetOrderStatus")]
        public async Task<HttpResponseData> GetOrderStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{orderId}/status")] HttpRequestData req,
            string orderId)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", orderId);
                if (order == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("Order not found");
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    orderId = order.OrderId,
                    status = order.Status,
                    orderDate = order.OrderDate,
                    totalPrice = order.TotalPrice
                }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal server error");
                return errorResponse;
            }
        }
    }

    public class OrderProcessRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // approve, complete, cancel
    }

    // Models for the Functions project
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string OrderId => RowKey;
        public string CustomerId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Today;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Submitted";
    }

    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string ProductId => RowKey;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockAvailable { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
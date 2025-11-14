// Services/ISqlDatabaseService.cs
using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public interface ISqlDatabaseService
    {
        // Customer operations
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(string customerId);
        Task<Customer> AddCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(string customerId);

        // Product operations
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(string productId);
        Task<Product> AddProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(string productId);

        // Order operations
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order?> GetOrderByIdAsync(string orderId);
        Task<Order> AddOrderAsync(Order order);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(string orderId);

        // User operations
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User> AddUserAsync(User user);

        // Cart operations
        Task<List<Cart>> GetUserCartAsync(int userId);
        Task<Cart?> GetCartItemAsync(int userId, string productId);
        Task<Cart> AddCartItemAsync(Cart cart);
        Task<Cart> UpdateCartItemAsync(Cart cart);
        Task DeleteCartItemAsync(int cartId);
        Task ClearUserCartAsync(int userId);
    }
}


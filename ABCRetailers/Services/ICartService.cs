// Services/ICartService.cs
using ABCRetailers.Models;

namespace ABCRetailers.Services
{
    public interface ICartService
    {
        List<CartItem> GetCartItems(ISession session);
        void SaveCartItems(ISession session, List<CartItem> items);
        void AddToCart(ISession session, Product product, int quantity);
        void RemoveFromCart(ISession session, string productId);
        void UpdateCartItemQuantity(ISession session, string productId, int quantity);
        void ClearCart(ISession session);
        int GetCartItemCount(ISession session);
    }
}


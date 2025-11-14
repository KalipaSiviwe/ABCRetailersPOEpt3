// Services/CartService.cs
using ABCRetailers.Models;
using System.Text.Json;

namespace ABCRetailers.Services
{
    public class CartService : ICartService
    {
        private const string CartSessionKey = "ShoppingCart";

        public List<CartItem> GetCartItems(ISession session)
        {
            var cartJson = session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        public void SaveCartItems(ISession session, List<CartItem> items)
        {
            var cartJson = JsonSerializer.Serialize(items);
            session.SetString(CartSessionKey, cartJson);
        }

        public void AddToCart(ISession session, Product product, int quantity)
        {
            var cartItems = GetCartItems(session);
            var existingItem = cartItems.FirstOrDefault(item => item.ProductId == product.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                if (existingItem.Quantity > product.StockAvailable)
                {
                    existingItem.Quantity = product.StockAvailable;
                }
            }
            else
            {
                cartItems.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = quantity > product.StockAvailable ? product.StockAvailable : quantity,
                    StockAvailable = product.StockAvailable
                });
            }

            SaveCartItems(session, cartItems);
        }

        public void RemoveFromCart(ISession session, string productId)
        {
            var cartItems = GetCartItems(session);
            cartItems.RemoveAll(item => item.ProductId == productId);
            SaveCartItems(session, cartItems);
        }

        public void UpdateCartItemQuantity(ISession session, string productId, int quantity)
        {
            var cartItems = GetCartItems(session);
            var item = cartItems.FirstOrDefault(i => i.ProductId == productId);
            
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cartItems.Remove(item);
                }
                else if (quantity <= item.StockAvailable)
                {
                    item.Quantity = quantity;
                }
            }

            SaveCartItems(session, cartItems);
        }

        public void ClearCart(ISession session)
        {
            session.Remove(CartSessionKey);
        }

        public int GetCartItemCount(ISession session)
        {
            var cartItems = GetCartItems(session);
            return cartItems.Sum(item => item.Quantity);
        }
    }
}


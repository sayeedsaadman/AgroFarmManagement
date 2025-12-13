using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using AgroManagement.Models;

namespace AgroManagement.Helper
{
    public static class CartSessionHelper
    {
        private const string CartKey = "CART";

        public static List<CartItem> GetCart(ISession session)
        {
            var json = session.GetString(CartKey);
            if (string.IsNullOrWhiteSpace(json))
                return new List<CartItem>();

            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        public static void SaveCart(ISession session, List<CartItem> cart)
        {
            var json = JsonSerializer.Serialize(cart);
            session.SetString(CartKey, json);
        }

        public static void ClearCart(ISession session)
        {
            session.Remove(CartKey);
        }
    }
}

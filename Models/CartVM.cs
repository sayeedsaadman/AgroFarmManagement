using System.Collections.Generic;
using System.Linq;

namespace AgroManagement.Models
{
    public class CartVM
    {
        public List<CartItem> Items { get; set; } = new();

        public decimal Subtotal => Items.Sum(i => i.Price * i.Quantity);
    }
}

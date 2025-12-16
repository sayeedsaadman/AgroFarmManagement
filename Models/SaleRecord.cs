using System;
using System.Collections.Generic;

namespace AgroManagement.Models
{
    public class SaleRecord
    {
        public string OrderId { get; set; } = Guid.NewGuid().ToString("N");
        public string Username { get; set; } = "";
        public DateTime OrderDateUtc { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public List<CartItem> Items { get; set; } = new();

        // ✅ ALIASES (DO NOT REMOVE)
        public DateTime Date
        {
            get => OrderDateUtc;
            set => OrderDateUtc = value;
        }

        public decimal Total
        {
            get => TotalAmount;
            set => TotalAmount = value;
        }
    }
}

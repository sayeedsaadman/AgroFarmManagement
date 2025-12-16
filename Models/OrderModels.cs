namespace AgroManagement.Models
{
    public class OrderItem
    {
        public string ProductKey { get; set; } = "";
        public string Name { get; set; } = "";
        public string UnitLabel { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => Price * Quantity;
    }

    public class OrderRecord
    {
        public string InvoiceNo { get; set; } = "";
        public string Username { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public decimal Total { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }
}

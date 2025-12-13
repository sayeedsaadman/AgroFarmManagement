namespace AgroManagement.Models
{
    public class CartItem
    {
        public string ProductKey { get; set; } = "";
        public string Name { get; set; } = "";
        public string UnitLabel { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}

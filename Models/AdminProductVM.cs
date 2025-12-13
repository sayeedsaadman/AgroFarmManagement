namespace AgroManagement.Models
{
    public class AdminProductVM
    {
        public string Key { get; set; } = "";
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public string UnitLabel { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}

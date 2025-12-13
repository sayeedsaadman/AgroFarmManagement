using System.Collections.Generic;

namespace AgroManagement.Helper
{
    public record CatalogProduct(
        string Key,
        string Category,
        string Name,
        string UnitLabel,
        decimal Price
    );

    public static class ProductCatalog
    {
        public static IReadOnlyList<CatalogProduct> All => new List<CatalogProduct>
        {
            // Milk
            new("milk_raw_milk", "Milk", "Raw Milk", "KG", 360m),
            new("milk_pasteurized", "Milk", "Pasteurized Milk", "Litre", 215m),
            new("milk_uht", "Milk", "UHT Milk", "200 gm", 419m),
            new("milk_almond", "Milk", "Almond Milk", "250 gm", 479m),
            new("milk_zero_fat", "Milk", "Zero Fat Milk", "KG", 1559m),
            new("milk_mango", "Milk", "Mango Milk", "KG", 1919m),

            // Yogurt
            new("yogurt_organic", "Yogurt", "Organic Yogurt", "KG", 360m),
            new("yogurt_sweet_curd", "Yogurt", "Sweet Curd", "Litre", 215m),
            new("yogurt_greek", "Yogurt", "Greek Yogurt", "200 gm", 419m),
            new("yogurt_mango", "Yogurt", "Mango Yogurt", "250 gm", 479m),
            new("yogurt_baked", "Yogurt", "Baked Yogurt", "KG", 1559m),
            new("yogurt_no_sugar_lassi", "Yogurt", "No Sugar Lassi", "KG", 1919m),

            // Cheese
            new("cheese_paneer", "Cheese", "Paneer", "KG", 360m),
            new("cheese_mozarella", "Cheese", "Mozarella", "Litre", 215m),
            new("cheese_cheddar", "Cheese", "Cheddar Cheese", "200 gm", 419m),
            new("cheese_cottage", "Cheese", "Cottage Cheese", "250 gm", 479m),
            new("cheese_parmigiano", "Cheese", "Parmigiano", "KG", 1559m),
            new("cheese_roquefort", "Cheese", "Roquefort Cheese", "KG", 1919m),

            // Meat
            new("meat_raw_boneless", "Meat", "Raw Meat (Boneless)", "KG", 360m),
            new("meat_beef_steak", "Meat", "Beef Steak", "Litre", 215m),
            new("meat_beef_sausage", "Meat", "Beef Sausage", "200 gm", 419m),

            // Creamy Products
            new("cream_heavy", "Creamy Products", "Heavy Cream", "KG", 360m),
            new("cream_whipped", "Creamy Products", "Whipped Cream", "Litre", 215m),
            new("cream_cheese", "Creamy Products", "Cream Cheese", "200 gm", 419m),

            // Others
            new("other_milk_powder", "Others", "Milk Powder", "KG", 360m),
            new("other_casein", "Others", "Casein", "Litre", 215m),
            new("other_string_cheese", "Others", "String Cheese", "200 gm", 419m),
        };
    }
}

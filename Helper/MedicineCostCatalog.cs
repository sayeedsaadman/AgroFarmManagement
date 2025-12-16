namespace AgroManagement.Helper
{
    public static class MedicineCostCatalog
    {
        public static readonly Dictionary<string, decimal> Cost = new()
        {
            { "Antibiotic", 300 },
            { "Dewormer", 150 },
            { "Vitamin", 100 },
            { "Pain Reliever", 200 },
            { "Anti-inflammatory", 250 }
        };
    }
}

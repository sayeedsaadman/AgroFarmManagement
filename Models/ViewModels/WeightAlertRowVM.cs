namespace AgroManagement.Models.ViewModels
{
    public class WeightAlertRowVM
    {
        public int AnimalId { get; set; }
        public string TagNumber { get; set; } = "";

        public decimal Weight7DaysAgo { get; set; }
        public decimal LatestWeight { get; set; }

        public decimal PercentChange { get; set; }  // negative means drop
        public bool IsAlert { get; set; }
    }
}

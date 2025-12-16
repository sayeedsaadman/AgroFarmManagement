namespace AgroManagement.Models.ViewModels
{
    public class HealthTrackerPageVM
    {
        public string Date { get; set; } = ""; // yyyy-MM-dd
        public List<string> Medicines { get; set; } = new();
        public List<HealthTrackerRowVM> Rows { get; set; } = new();
    }
}

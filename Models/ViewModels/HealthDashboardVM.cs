using AgroManagement.Models;

namespace AgroManagement.Models.ViewModels
{
    public class HealthDashboardVM
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal TotalMilkMonth { get; set; }
        public int AnimalsWithMilkMonth { get; set; }
        public int MedicineLogsMonth { get; set; }

        public List<(string TagNumber, decimal TotalMilk)> MilkByAnimal { get; set; } = new();
        public List<HealthLogEntry> MedicineEntries { get; set; } = new();
    }
}

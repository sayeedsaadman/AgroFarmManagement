namespace AgroManagement.Models.ViewModels
{
    public class HealthTrackerRowVM
    {
        public int AnimalId { get; set; }
        public string TagNumber { get; set; } = "";
        public string? Breed { get; set; }
        public decimal WeightKg { get; set; }

        public decimal MilkLiters { get; set; }

        public bool HealthChecked { get; set; }

        public bool MedicineGiven { get; set; }
        public string? MedicineName { get; set; }
        public string? Dose { get; set; }

        public string? Notes { get; set; }
    }
}

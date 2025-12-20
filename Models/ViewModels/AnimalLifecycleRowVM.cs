namespace AgroManagement.Models.ViewModels
{
    public class AnimalLifecycleRowVM
    {
        public int AnimalId { get; set; }
        public string TagNumber { get; set; } = "";
        public string Breed { get; set; } = "";
        public double Weight { get; set; }

        public string Status { get; set; } = "Active";
        public string Notes { get; set; } = "";
        public string UpdatedOn { get; set; } = "";
    }
}

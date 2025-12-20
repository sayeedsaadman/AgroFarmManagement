namespace AgroManagement.Models
{
    public class AnimalLifecycleStatusEntry
    {
        public int AnimalId { get; set; }
        public string TagNumber { get; set; } = "";
        public string Status { get; set; } = "Active";
        public string Notes { get; set; } = "";
        public string UpdatedOn { get; set; } = ""; // yyyy-MM-dd
    }
}

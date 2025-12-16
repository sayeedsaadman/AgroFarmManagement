namespace AgroManagement.Models.ViewModels
{
    public class AnimalExpenseVM
    {
        public int AnimalId { get; set; }
        public string TagNumber { get; set; } = "";

        public decimal FoodExpense { get; set; }
        public decimal MaintenanceExpense { get; set; }
        public decimal MedicalExpense { get; set; }

        public decimal TotalExpense =>
            FoodExpense + MaintenanceExpense + MedicalExpense;
    }
}
